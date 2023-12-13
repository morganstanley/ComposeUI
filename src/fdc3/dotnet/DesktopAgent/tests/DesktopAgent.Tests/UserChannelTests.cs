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

using System.Text;
using System.Text.Json;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class UserChannelTests
{
    private const string TestChannel = "testChannel";
    UserChannel _channel = new UserChannel(TestChannel, new Mock<IMessagingService>().Object, null);
    UserChannelTopics _topics = new UserChannelTopics(TestChannel);

    [Theory]
    [InlineData(new object?[] { null })]
    [InlineData(new object?[] { "testType" })]
    public async void CallingGetCurrentContextOnNewUserChannelReturnsNull(string? contextType)
    {
        var request = new GetCurrentContextRequest() { ContextType = contextType };
        var ctx = await _channel.GetCurrentContext(request);
        ctx.Should().BeNull();
    }

    [Fact]
    public void NewUserChannelCanHandleContext()
    {
        var context = GetContext();
        new Action(() => _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context)))).Should().NotThrow();
    }

    [Fact]
    public async void BroadcastedChannelCanReturnLatestBroadcast()
    {
        var context = await PreBroadcastContext();
        var ctx = await _channel.GetCurrentContext(null);
        var result = JsonSerializer.Deserialize<Contact>(Encoding.UTF8.GetString(ctx));
        result.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedUserChannelCanReturnLatestBroadcastForType()
    {
        var context = await PreBroadcastContext();
        var ctx = await _channel.GetCurrentContext(ContextType);
        var result = JsonSerializer.Deserialize<Contact>(Encoding.UTF8.GetString(ctx));
        result.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedUserChannelReturnsNullForDifferentType()
    {
        await PreBroadcastContext();
        var ctx = await _channel.GetCurrentContext(OtherContextType);
        ctx.Should().BeNull();
    }

    [Fact]
    public async void BroadcastedUserChannelCanHandleBroadcast()
    {
        await PreBroadcastContext();
        var context = GetContext();
        new Action(() => _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context)))).Should().NotThrow();
    }

    [Fact]
    public async void BroadcastedUserChannelUpdatesLatestBroadcast()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await _channel.GetCurrentContext(null);
        var result = JsonSerializer.Deserialize<Contact>(Encoding.UTF8.GetString(ctx));
        result.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedUserChannelUpdatesLatestBroadcastForType()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await _channel.GetCurrentContext(ContextType);
        var result = JsonSerializer.Deserialize<Contact>(Encoding.UTF8.GetString(ctx));
        result.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedUserChannelCanHandleDifferentBroadcast()
    {
        await PreBroadcastContext();
        new Action(() => _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(GetDifferentContext())))).Should().NotThrow();
    }

    [Fact]
    public async void ChannelWithDifferentBroadcastsUpdatesLatestBroadcast()
    {
        var (_, second) = await BroadcastDifferentContexts();
        var ctx = await _channel.GetCurrentContext(null);
        var result = JsonSerializer.Deserialize<Currency>(Encoding.UTF8.GetString(ctx));

        result.Should().BeEquivalentTo(second);
    }

    [Fact]
    public async void ChannelWithDifferentBroadcastsReturnsAppropriateContext()
    {
        var (first, second) = await BroadcastDifferentContexts();

        var ctx1 = await _channel.GetCurrentContext(ContextType);
        var ctx2 = await _channel.GetCurrentContext(DifferentContextType);

        var context1 = JsonSerializer.Deserialize<Contact>(Encoding.UTF8.GetString(ctx1));
        var context2 = JsonSerializer.Deserialize<Currency>(Encoding.UTF8.GetString(ctx2));

        context1.Should().BeEquivalentTo(first);
        context2.Should().BeEquivalentTo(second);
    }

    private int _counter;

    private GetCurrentContextRequest ContextType => new GetCurrentContextRequest { ContextType = new Contact().Type };
    private GetCurrentContextRequest OtherContextType => new GetCurrentContextRequest { ContextType = new Email(null).Type };
    private Contact GetContext() => new Contact(new ContactID() { Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}" }, "Testy Tester");
    private GetCurrentContextRequest DifferentContextType => new GetCurrentContextRequest { ContextType = new Currency().Type };
    private Currency GetDifferentContext() => new Currency(new CurrencyID() { CURRENCY_ISOCODE = "HUF" });

    private async ValueTask<Contact> PreBroadcastContext()
    {
        var context = GetContext();
        await _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context)));
        return context;
    }

    private async ValueTask<Contact> DoubleBroadcastContext()
    {
        await _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(GetContext())));
        var context = GetContext();
        await _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context)));
        return context;
    }

    private async ValueTask<(Contact first, Currency second)> BroadcastDifferentContexts()
    {
        var first = GetContext();
        var second = GetDifferentContext();
        await _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(first)));
        await _channel.HandleBroadcast(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(second)));
        return (first, second);
    }
}