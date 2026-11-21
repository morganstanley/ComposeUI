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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class PrivateChannelEventListenersTests
{
    [Fact]
    public async Task PrivateChannelContextListenerEventListener_unsubscribe_prevents_future_execute_callbacks()
    {
        var handledContextTypes = new List<string?>();
        var unsubscribeCount = 0;
        var listener = new PrivateChannelContextListenerEventListener(
            onContext: contextType => handledContextTypes.Add(contextType),
            onUnsubscribe: _ => unsubscribeCount++);

        listener.Execute("fdc3.instrument");
        await listener.Unsubscribe();
        listener.Execute("fdc3.contact");

        handledContextTypes.Should().ContainSingle().Which.Should().Be("fdc3.instrument");
        unsubscribeCount.Should().Be(1);
    }

    [Fact]
    public async Task PrivateChannelContextListenerEventListener_unsubscribe_core_without_callback_skips_callback()
    {
        var unsubscribeCount = 0;
        var listener = new PrivateChannelContextListenerEventListener(
            onContext: _ => { },
            onUnsubscribe: _ => unsubscribeCount++);

        await listener.UnsubscribeCoreAsync(false);

        unsubscribeCount.Should().Be(0);
    }

    [Fact]
    public async Task PrivateChannelDisconnectEventListener_unsubscribe_prevents_future_execute_callbacks()
    {
        var disconnectCount = 0;
        var unsubscribeCount = 0;
        var listener = new PrivateChannelDisconnectEventListener(
            onDisconnectEventHandler: () => disconnectCount++,
            onUnsubscribeHandler: _ => unsubscribeCount++);

        listener.Execute();
        await listener.Unsubscribe();
        listener.Execute();

        disconnectCount.Should().Be(1);
        unsubscribeCount.Should().Be(1);
    }

    [Fact]
    public async Task PrivateChannelDisconnectEventListener_unsubscribe_core_without_callback_skips_callback()
    {
        var unsubscribeCount = 0;
        var listener = new PrivateChannelDisconnectEventListener(
            onDisconnectEventHandler: () => { },
            onUnsubscribeHandler: _ => unsubscribeCount++);

        await listener.UnsubscribeCoreAsync(false);

        unsubscribeCount.Should().Be(0);
    }
}
