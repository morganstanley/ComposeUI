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

using ComposeUI.Messaging.Client;
using MorganStanley.ComposeUI.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfDemoApp;

public class HelloWorldApp : IApplication
{
    HelloWorld? _app;
    IMessageRouter _messageRouter;

    public async Task<bool> ClosingRequested()
    {
        return await _app.Dispatcher.Invoke(DisplayExitMessageBox);
    }

    public Task Initialize(IMessageRouter messageRouter)
    {
        _app = new HelloWorld();
        _messageRouter = messageRouter;        
        return Task.CompletedTask;
    }

    public void Render(ContentPresenter target)
    {
        target.Content = _app;
        _app.ButtonClicked -= Button_Clicked;
        _app.ButtonClicked += Button_Clicked;
    }

    public Task Teardown() => Task.CompletedTask;

    private Task<bool> DisplayExitMessageBox()
    {
        return Task.FromResult(MessageBox.Show("Are you sure you want to exit?", "Are you sure?", MessageBoxButton.YesNo) == MessageBoxResult.Yes);
    }

    private async void Button_Clicked(object? sender, EventArgs e)
    {
        await _messageRouter.PublishAsync("urls", "https://morganstanley.com");
    }
}
