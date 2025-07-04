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

using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

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
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, JsonFactory.CreateJson(request), null);
        ctx.Should().BeNull();
    }

    [Fact]
    public async Task NewChannelCanHandleContext()
    {
        var context = GetContext();
        Task Act() => Channel.HandleBroadcast(context).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastedChannelCanReturnLatestBroadcast()
    {
        var context = await PreBroadcastContext();

        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelCanReturnLatestBroadcastForType()
    {
        var context = await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelReturnsNullForDifferentType()
    {
        await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, OtherContextType, null);
        ctx.Should().BeNull();
    }

    [Fact]
    public async Task BroadcastedChannelCanHandleBroadcast()
    {
        await PreBroadcastContext();
        var context = GetContext();
        Task Act() => Channel.HandleBroadcast(context).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastedChannelUpdatesLatestBroadcast()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelUpdatesLatestBroadcastForType()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task BroadcastedChannelCanHandleDifferentBroadcast()
    {
        await PreBroadcastContext();
        Task Act() => Channel.HandleBroadcast(GetDifferentContext()).AsTask();
        await FluentActions.Awaiting(Act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ChannelWithDifferentBroadcastsUpdatesLatestBroadcast()
    {
        var (_, second) = await BroadcastDifferentContexts();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(second);
    }

    [Fact]
    public async Task ChannelWithDifferentBroadcastsReturnsAppropriateContext()
    {
        var (first, second) = await BroadcastDifferentContexts();

        var ctx1 = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        var ctx2 = await Channel.GetCurrentContext(Topics.GetCurrentContext, DifferentContextType, null);

        ctx1.Should().BeEquivalentTo(first);
        ctx2.Should().BeEquivalentTo(second);
    }

    private int _counter;
    private string ContextType => JsonFactory.CreateJson(new GetCurrentContextRequest { ContextType = new Contact().Type });
    private string OtherContextType => JsonFactory.CreateJson(new GetCurrentContextRequest { ContextType = new Email(null).Type });
    private string GetContext() => JsonFactory.CreateJson(new Contact(new ContactID() { Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}" }, "Testy Tester"));
    private string DifferentContextType => JsonFactory.CreateJson(new GetCurrentContextRequest { ContextType = new Currency().Type });
    private string GetDifferentContext() => JsonFactory.CreateJson(new Currency(new CurrencyID() { CURRENCY_ISOCODE = "HUF" }));

    private async ValueTask<string> PreBroadcastContext()
    {
        var context = GetContext();
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<string> DoubleBroadcastContext()
    {
        await Channel.HandleBroadcast(GetContext());
        var context = GetContext();
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<(string first, string second)> BroadcastDifferentContexts()
    {
        var first = GetContext();
        var second = GetDifferentContext();
        await Channel.HandleBroadcast(first);
        await Channel.HandleBroadcast(second);
        return (first, second);
    }
}