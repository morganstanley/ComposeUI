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

using System.Text.Json;
using System.Text.Json.Nodes;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Channels;

public abstract class ChannelTestBase
{
    internal Channel Channel { get; init; }
    internal ChannelTopics Topics { get; init; }

    [Theory]
    [InlineData([null])]
    [InlineData(["testType"])]
    public async Task CallingGetCurrentContextOnNewChannelReturnsNull(string? contextType)
    {
        var request = new GetCurrentContextRequest() { ContextType = contextType };
        var ctx = await Channel.GetCurrentContext(request);
        ctx.Should().BeNull();
    }

    [Fact]
    public async Task NewChannelCanHandleContext()
    {
        var context = GetContext();
        Task Act() => Channel.HandleBroadcast(SerializeJson(context)).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastedChannelCanReturnLatestBroadcast()
    {
        var context = await PreBroadcastContext();

        var ctx = await Channel.GetCurrentContext(null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelCanReturnLatestBroadcastForType()
    {
        var context = await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(RequestWithContextType);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelReturnsNullForDifferentType()
    {
        await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(RequestWithOtherContextType);
        ctx.Should().BeNull();
    }

    [Fact]
    public async Task BroadcastedChannelCanHandleBroadcast()
    {
        await PreBroadcastContext();
        var context = GetContext();
        Task Act() => Channel.HandleBroadcast(SerializeJson(context)).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastedChannelUpdatesLatestBroadcast()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelUpdatesLatestBroadcastForType()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(RequestWithContextType);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelCanHandleDifferentBroadcast()
    {
        await PreBroadcastContext();
        Task Act() => Channel.HandleBroadcast(SerializeJson(GetDifferentContext())).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ChannelWithDifferentBroadcastsUpdatesLatestBroadcast()
    {
        var (_, second) = await BroadcastDifferentContexts();
        var ctx = await Channel.GetCurrentContext(null);
        ctx.Should().BeEquivalentTo(second);
    }

    [Fact]
    public async Task ChannelWithDifferentBroadcastsReturnsAppropriateContext()
    {
        var (first, second) = await BroadcastDifferentContexts();

        var ctx1 = await Channel.GetCurrentContext(RequestWithContextType);
        var ctx2 = await Channel.GetCurrentContext(RequestWithDifferentContextType);

        ctx1.Should().BeEquivalentTo(first);
        ctx2.Should().BeEquivalentTo(second);
    }

    private int _counter;
    private GetCurrentContextRequest RequestWithContextType => new GetCurrentContextRequest { ContextType = new Contact().Type };
    private GetCurrentContextRequest RequestWithOtherContextType => new GetCurrentContextRequest { ContextType = new Email(null).Type };
    private Contact GetContext() => new Contact(new ContactID() { Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}" }, "Testy Tester");
    private GetCurrentContextRequest RequestWithDifferentContextType => new GetCurrentContextRequest { ContextType = new Currency().Type };
    private Currency GetDifferentContext() => new Currency(new CurrencyID() { CURRENCY_ISOCODE = "HUF" });

    private async ValueTask<string> PreBroadcastContext()
    {
        var context = SerializeJson(GetContext());
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<string> DoubleBroadcastContext()
    {
        var context = SerializeJson(GetContext());
        await Channel.HandleBroadcast(context);
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<(string first, string second)> BroadcastDifferentContexts()
    {
        var first = SerializeJson(GetContext());
        var second = SerializeJson(GetDifferentContext());
        await Channel.HandleBroadcast(first);
        await Channel.HandleBroadcast(second);
        return (first, second);
    }

    private string SerializeJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

}