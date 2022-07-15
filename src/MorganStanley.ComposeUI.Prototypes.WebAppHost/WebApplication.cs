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
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MorganStanley.ComposeUI.Prototypes.WebAppHost;

public class WebApplication : IApplication, INotifyPropertyChanged
{
    private Uri? _currentUri;
    private IMessageRouter _messageRouter;
    private WebControl? _webControl;
    private IObserver<RouterMessage> _urlObserver;

    public Uri? CurrentUri
    {
        get { return _currentUri; }
        set
        {
            _currentUri = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUri)));
        }
    }

    public WebApplication()
    {
        _currentUri = new Uri("about:blank");
        _urlObserver = Observer.Create<RouterMessage>(ChangeUrl);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task Initialize(IMessageRouter messageRouter)
    {
        _webControl = new WebControl();
        _webControl.DataContext = this;
        _messageRouter = messageRouter;
        await _messageRouter.SubscribeAsync("urls", _urlObserver).ConfigureAwait(false);
    }

    public void Render(ContentPresenter target)
    {
        target.Content = _webControl;
    }

    public Task Teardown() => Task.CompletedTask;

    public Task<bool> ClosingRequested()
    {
        return Task.FromResult(true);
    }

    private async void ChangeUrl(RouterMessage message)
    {
        CurrentUri = new Uri(message.Payload ?? "about:blank");
    }
}
