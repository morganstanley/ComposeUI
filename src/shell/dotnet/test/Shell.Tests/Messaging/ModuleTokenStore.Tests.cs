/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using FluentAssertions;

namespace MorganStanley.ComposeUI.Shell.Messaging;

public class ModuleTokenStoreTests
{
    private readonly ModuleTokenStore _store = new();

    [Fact]
    public void Issue_returns_a_64_character_hex_token()
    {
        var token = _store.Issue(Guid.NewGuid(), "module-a");

        token.Should().MatchRegex("^[0-9A-F]{64}$");
    }

    [Fact]
    public void Issue_returns_different_tokens_for_different_instances()
    {
        var tokenA = _store.Issue(Guid.NewGuid(), "module-a");
        var tokenB = _store.Issue(Guid.NewGuid(), "module-b");

        tokenA.Should().NotBe(tokenB);
    }

    [Fact]
    public void Issue_returns_different_tokens_for_two_instances_of_the_same_module()
    {
        var tokenA = _store.Issue(Guid.NewGuid(), "module-a");
        var tokenB = _store.Issue(Guid.NewGuid(), "module-a");

        tokenA.Should().NotBe(tokenB);
    }

    [Fact]
    public void Issue_returns_the_same_token_when_called_again_for_the_same_instance()
    {
        var instanceId = Guid.NewGuid();

        var first = _store.Issue(instanceId, "module-a");
        var second = _store.Issue(instanceId, "module-a");

        second.Should().Be(first);
    }

    [Fact]
    public void Issue_throws_if_moduleId_is_null()
    {
        var act = () => _store.Issue(Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryValidate_returns_the_identity_of_the_instance_the_token_was_issued_to()
    {
        var instanceId = Guid.NewGuid();
        var token = _store.Issue(instanceId, "module-a");

        var result = _store.TryValidate(token, out var identity);

        result.Should().BeTrue();
        identity.Should().Be(new ModuleIdentity(instanceId, "module-a"));
    }

    [Fact]
    public void TryValidate_does_not_confuse_the_tokens_of_different_modules()
    {
        var instanceA = Guid.NewGuid();
        var instanceB = Guid.NewGuid();
        var tokenA = _store.Issue(instanceA, "module-a");
        var tokenB = _store.Issue(instanceB, "module-b");

        _store.TryValidate(tokenA, out var identityA).Should().BeTrue();
        _store.TryValidate(tokenB, out var identityB).Should().BeTrue();

        identityA.Should().Be(new ModuleIdentity(instanceA, "module-a"));
        identityB.Should().Be(new ModuleIdentity(instanceB, "module-b"));
    }

    [Fact]
    public void TryValidate_can_be_called_repeatedly_with_the_same_token()
    {
        var token = _store.Issue(Guid.NewGuid(), "module-a");

        _store.TryValidate(token, out _).Should().BeTrue();
        _store.TryValidate(token, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-token")]
    public void TryValidate_returns_false_for_missing_or_malformed_tokens(string? token)
    {
        _store.Issue(Guid.NewGuid(), "module-a");

        var result = _store.TryValidate(token, out var identity);

        result.Should().BeFalse();
        identity.Should().BeNull();
    }

    [Fact]
    public void TryValidate_returns_false_for_an_unknown_token()
    {
        _store.Issue(Guid.NewGuid(), "module-a");
        var unknownToken = new string('0', 64);

        var result = _store.TryValidate(unknownToken, out var identity);

        result.Should().BeFalse();
        identity.Should().BeNull();
    }

    [Fact]
    public void TryValidate_returns_false_for_a_token_that_differs_in_one_character()
    {
        var token = _store.Issue(Guid.NewGuid(), "module-a");
        var tampered = (token[0] == '0' ? '1' : '0') + token[1..];

        _store.TryValidate(tampered, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_returns_false_after_the_token_is_revoked()
    {
        var instanceId = Guid.NewGuid();
        var token = _store.Issue(instanceId, "module-a");

        _store.Revoke(instanceId);

        _store.TryValidate(token, out var identity).Should().BeFalse();
        identity.Should().BeNull();
    }

    [Fact]
    public void Revoke_does_not_affect_the_tokens_of_other_instances()
    {
        var instanceA = Guid.NewGuid();
        var instanceB = Guid.NewGuid();
        _store.Issue(instanceA, "module-a");
        var tokenB = _store.Issue(instanceB, "module-b");

        _store.Revoke(instanceA);

        _store.TryValidate(tokenB, out _).Should().BeTrue();
    }

    [Fact]
    public void Revoke_returns_true_only_if_a_token_was_revoked()
    {
        var instanceId = Guid.NewGuid();
        _store.Issue(instanceId, "module-a");

        _store.Revoke(instanceId).Should().BeTrue();
        _store.Revoke(instanceId).Should().BeFalse();
        _store.Revoke(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void HostToken_is_a_64_character_hex_token_and_is_stable()
    {
        _store.HostToken.Should().MatchRegex("^[0-9A-F]{64}$");
        _store.HostToken.Should().Be(_store.HostToken);
    }

    [Fact]
    public void HostToken_is_different_for_each_store()
    {
        new ModuleTokenStore().HostToken.Should().NotBe(new ModuleTokenStore().HostToken);
    }

    [Fact]
    public void IsHostToken_returns_true_only_for_the_host_token()
    {
        var moduleToken = _store.Issue(Guid.NewGuid(), "module-a");

        _store.IsHostToken(_store.HostToken).Should().BeTrue();
        _store.IsHostToken(moduleToken).Should().BeFalse();
        _store.IsHostToken(null).Should().BeFalse();
        _store.IsHostToken("").Should().BeFalse();
        _store.IsHostToken(new string('0', 64)).Should().BeFalse();
    }

    [Fact]
    public void The_host_token_is_not_a_module_token()
    {
        _store.TryValidate(_store.HostToken, out var identity).Should().BeFalse();
        identity.Should().BeNull();
    }

    [Fact]
    public void Issue_after_revoke_returns_a_new_token()
    {
        var instanceId = Guid.NewGuid();
        var oldToken = _store.Issue(instanceId, "module-a");
        _store.Revoke(instanceId);

        var newToken = _store.Issue(instanceId, "module-a");

        newToken.Should().NotBe(oldToken);
        _store.TryValidate(oldToken, out _).Should().BeFalse();
        _store.TryValidate(newToken, out _).Should().BeTrue();
    }
}
