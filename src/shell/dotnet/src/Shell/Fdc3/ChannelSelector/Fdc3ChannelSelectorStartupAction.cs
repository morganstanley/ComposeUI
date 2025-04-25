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

using System;
using System.Threading.Tasks;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ChannelSelector
{
    internal class Fdc3ChannelSelectorStartupAction : IStartupAction
    {
        private Fdc3ChannelSelectorControl _fdc3ChannelSelectorControl;
        private IMessageRouter _messageRouter;
        private string _color;
        private string _instanceId;
        private readonly IChannelSelectorInstanceCommunicator _channelSelectorInstanceCommunicator;


        public Fdc3ChannelSelectorStartupAction( IMessageRouter messageRouter)
        {
           _messageRouter = messageRouter;
           _channelSelectorInstanceCommunicator = new ChannelSelectorInstanceCommunicator(_messageRouter);
        }

        public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
        {
            if (startupContext.ModuleInstance.Manifest.ModuleType == ModuleType.Web)
            {
                var webStartupProperties = startupContext.GetOrAddProperty<WebStartupProperties>();
                var color = webStartupProperties.ChannelColor;
                var instanceId = webStartupProperties.InstanceId;

                var fdc3StartupProperties = startupContext.GetOrAddProperty<Fdc3StartupProperties>();
                var userChannelCollection = fdc3StartupProperties.UserChannelCollection;

                _fdc3ChannelSelectorControl = new Fdc3ChannelSelectorControl(_channelSelectorInstanceCommunicator, color, instanceId, userChannelCollection);

                await Task.Run(async () =>
                {
                    await _channelSelectorInstanceCommunicator.RegisterMessageRouterForInstance(instanceId, _fdc3ChannelSelectorControl.ColorUpdate);
                });


                webStartupProperties.Fdc3ChannelSelectorControl = _fdc3ChannelSelectorControl;
            }

            await (next.Invoke());
        }
    }
}
