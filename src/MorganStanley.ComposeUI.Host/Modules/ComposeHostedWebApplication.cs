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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using ComposeUI.Messaging.Client;
using Microsoft.Web.WebView2.Wpf;
using MorganStanley.ComposeUI.Interfaces;

namespace MorganStanley.ComposeUI.Host.Modules;

internal class ComposeHostedWebApplication : IApplication
{
    private readonly Process _process = new Process();
    private readonly string _path;
    private readonly WebView2 _webView = new WebView2();

    public ComposeHostedWebApplication(string path)
    {
        _path = path;
    }

    public Task<bool> ClosingRequested()
    {
        return Task.FromResult(true);
    }

    public Task Initialize(IMessageRouter messageRouter)
    {
        _process.StartInfo.FileName = "cmd.exe";
        _process.StartInfo.WorkingDirectory = Path.GetFullPath(_path);
        _process.StartInfo.RedirectStandardInput = true;
        _process.Start();
        _process.StandardInput.WriteLine("npm run serve");
        return Task.Delay(10);
    }

    public void Render(ContentPresenter target)
    {
        _webView.Source = new Uri("http://localhost:8080");
        target.Content = _webView;
    }

    public async Task Teardown()
    {
        _process.CloseMainWindow();
        await _process.WaitForExitAsync();
    }
}
