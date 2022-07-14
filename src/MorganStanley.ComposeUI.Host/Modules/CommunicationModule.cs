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

using MorganStanley.ComposeUI.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using ComposeUI.Messaging.Client;

namespace MorganStanley.ComposeUI.Host.Modules;

internal class CommunicationModule : IModule, IDisposable
{
    Process? _process;
    private bool _disposed;
    
    public async Task Initialize(IMessageRouter messageRouter)
    {
        _process = new Process();
        _process.StartInfo.UseShellExecute = false;
        //_process.StartInfo.CreateNoWindow = true;

        var location = Path.GetFullPath("..\\..\\..\\ComposeUI.Messaging.Server\\bin\\Debug\\ComposeUI.Messaging.Server.exe");
        _process.StartInfo.FileName = location;
        _process.StartInfo.WorkingDirectory = Path.GetDirectoryName(location);
        _process.Start();
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    public async Task Teardown()
    {
        if (_process == null)
        {
            return;
        }

        _process.CloseMainWindow();
        await _process.WaitForExitAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _process != null)
            {
                _process.Kill();
                _process.Dispose();
            }

            _process = null;
            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
