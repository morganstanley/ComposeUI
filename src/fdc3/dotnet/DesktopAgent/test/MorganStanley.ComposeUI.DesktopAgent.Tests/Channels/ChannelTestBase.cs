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
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Channels;

public abstract class ChannelTestBase
{
    internal Channel Channel { get; init; }
    internal ChannelTopics Topics { get; init; }

    [Theory]
    [InlineData(new object?[] { null })]
    [InlineData(new object?[] { "testType" })]
    public async void CallingGetCurrentContextOnNewChannelReturnsNull(string? contextType)
    {
        var request = new GetCurrentContextRequest() { ContextType = contextType };
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, MessageBuffer.Factory.CreateJson(request), null);
        ctx.Should().BeNull();
    }

    [Fact]
    public void NewChannelCanHandleContext()
    {
        var context = GetContext();
        new System.Action(() => Channel.HandleBroadcast(context)).Should().NotThrow();
    }

    [Fact]
    public async void BroadcastedChannelCanReturnLatestBroadcast()
    {
        var context = await PreBroadcastContext();

        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedChannelCanReturnLatestBroadcastForType()
    {
        var context = await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedChannelReturnsNullForDifferentType()
    {
        await PreBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, OtherContextType, null);
        ctx.Should().BeNull();
    }

    [Fact]
    public async void BroadcastedChannelCanHandleBroadcast()
    {
        await PreBroadcastContext();
        var context = GetContext();
        new System.Action(() => Channel.HandleBroadcast(context)).Should().NotThrow();
    }

    [Fact]
    public async void BroadcastedChannelUpdatesLatestBroadcast()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedChannelUpdatesLatestBroadcastForType()
    {
        var context = await DoubleBroadcastContext();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        ctx.Should().BeEquivalentTo(context);
    }

    [Fact]
    public async void BroadcastedChannelCanHandleDifferentBroadcast()
    {
        await PreBroadcastContext();
        new System.Action(() => Channel.HandleBroadcast(GetDifferentContext())).Should().NotThrow();
    }

    [Fact]
    public async void ChannelWithDifferentBroadcastsUpdatesLatestBroadcast()
    {
        var (_, second) = await BroadcastDifferentContexts();
        var ctx = await Channel.GetCurrentContext(Topics.GetCurrentContext, null, null);
        ctx.Should().BeEquivalentTo(second);
    }

    [Fact]
    public async void ChannelWithDifferentBroadcastsReturnsAppropriateContext()
    {
        var (first, second) = await BroadcastDifferentContexts();

        var ctx1 = await Channel.GetCurrentContext(Topics.GetCurrentContext, ContextType, null);
        var ctx2 = await Channel.GetCurrentContext(Topics.GetCurrentContext, DifferentContextType, null);

        ctx1.Should().BeEquivalentTo(first);
        ctx2.Should().BeEquivalentTo(second);
    }

    private int _counter;
    private MessageBuffer ContextType => MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest { ContextType = new Contact().Type });
    private MessageBuffer OtherContextType => MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest { ContextType = new Email(null).Type });
    private MessageBuffer GetContext() => MessageBuffer.Factory.CreateJson(new Contact(new ContactID() { Email = $"test{_counter}@test.org", FDS_ID = $"test{_counter++}" }, "Testy Tester"));
    private MessageBuffer DifferentContextType => MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest { ContextType = new Currency().Type });
    private MessageBuffer GetDifferentContext() => MessageBuffer.Factory.CreateJson(new Currency(new CurrencyID() { CURRENCY_ISOCODE = "HUF" }));

    private async ValueTask<MessageBuffer> PreBroadcastContext()
    {
        var context = GetContext();
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<MessageBuffer> DoubleBroadcastContext()
    {
        await Channel.HandleBroadcast(GetContext());
        var context = GetContext();
        await Channel.HandleBroadcast(context);
        return context;
    }

    private async ValueTask<(MessageBuffer first, MessageBuffer second)> BroadcastDifferentContexts()
    {
        var first = GetContext();
        var second = GetDifferentContext();
        await Channel.HandleBroadcast(first);
        await Channel.HandleBroadcast(second);
        return (first, second);
    }
}