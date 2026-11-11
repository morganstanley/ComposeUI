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

public class ModuleTokenClientAuthenticatorTests
{
    private readonly ModuleTokenStore _store = new();
    private readonly ModuleTokenClientAuthenticator _authenticator;

    public ModuleTokenClientAuthenticatorTests()
    {
        _authenticator = new ModuleTokenClientAuthenticator(_store);
    }

    [Fact]
    public async Task A_module_token_authenticates_as_that_module_instance()
    {
        var instanceId = Guid.NewGuid();
        var token = _store.Issue(instanceId, "module-a");

        var principal = await _authenticator.Authenticate("client-1", token);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.FindFirst(MessageRouterClaimTypes.IdentityType)!.Value
            .Should().Be(MessageRouterClaimTypes.IdentityTypes.Module);
        principal.FindFirst(MessageRouterClaimTypes.ModuleId)!.Value.Should().Be("module-a");
        principal.FindFirst(MessageRouterClaimTypes.ModuleInstanceId)!.Value.Should().Be(instanceId.ToString());
    }

    [Fact]
    public async Task Tokens_of_two_modules_authenticate_as_two_different_principals()
    {
        var instanceA = Guid.NewGuid();
        var instanceB = Guid.NewGuid();
        var tokenA = _store.Issue(instanceA, "module-a");
        var tokenB = _store.Issue(instanceB, "module-b");

        var principalA = await _authenticator.Authenticate("client-1", tokenA);
        var principalB = await _authenticator.Authenticate("client-2", tokenB);

        principalA.FindFirst(MessageRouterClaimTypes.ModuleInstanceId)!.Value.Should().Be(instanceA.ToString());
        principalB.FindFirst(MessageRouterClaimTypes.ModuleInstanceId)!.Value.Should().Be(instanceB.ToString());
    }

    [Fact]
    public async Task The_host_token_authenticates_as_the_host()
    {
        var principal = await _authenticator.Authenticate("client-1", _store.HostToken);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.FindFirst(MessageRouterClaimTypes.IdentityType)!.Value
            .Should().Be(MessageRouterClaimTypes.IdentityTypes.Host);
        principal.FindFirst(MessageRouterClaimTypes.ModuleId).Should().BeNull();
        principal.FindFirst(MessageRouterClaimTypes.ModuleInstanceId).Should().BeNull();
    }

    [Fact]
    public async Task A_module_cannot_authenticate_as_the_host()
    {
        var token = _store.Issue(Guid.NewGuid(), "module-a");

        var principal = await _authenticator.Authenticate("client-1", token);

        principal.FindFirst(MessageRouterClaimTypes.IdentityType)!.Value
            .Should().NotBe(MessageRouterClaimTypes.IdentityTypes.Host);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Missing_or_unknown_tokens_are_rejected(string? token)
    {
        var act = async () => await _authenticator.Authenticate("client-1", token);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task A_token_is_rejected_after_it_is_revoked()
    {
        var instanceId = Guid.NewGuid();
        var token = _store.Issue(instanceId, "module-a");
        _store.Revoke(instanceId);

        var act = async () => await _authenticator.Authenticate("client-1", token);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
