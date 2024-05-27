// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

/// <summary>
/// Interaction logic for Fdc3ResolverUi.xaml
/// </summary>
public partial class Fdc3ResolverUi : Window, IDisposable
{
    private ILogger<Fdc3ResolverUi> _logger;
    private readonly List<ResolverUiAppData> _appData = new();
    private readonly CancellationTokenSource _userCancellationTokenSource;

    public IAppMetadata? AppMetadata { get; private set; }
    internal CancellationToken UserCancellationToken => _userCancellationTokenSource.Token;
    
    public Fdc3ResolverUi(
        IEnumerable<IAppMetadata> apps,
        ILogger<Fdc3ResolverUi>? logger = null)
    {
        _userCancellationTokenSource = new();
        _logger = logger ?? NullLogger<Fdc3ResolverUi>.Instance;

        InitializeComponent();

        foreach (var app in apps)
        {
            _appData.Add(new()
            {
                AppMetadata = app,
                Icon = app.Icons.FirstOrDefault() ?? null
            });
        }

        ResolverUiDataSource.ItemsSource = _appData;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        // Check if the user previously set the AppMetadata property to a row, if not then the user clicked the X button
        if (AppMetadata == null)
        {
            _userCancellationTokenSource.Cancel();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _userCancellationTokenSource.Cancel();
        Close();
    }

    private void ResolverUiDataSource_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ResolverUiDataSource.SelectedItem != null)
        {
            AppMetadata = ((ResolverUiAppData) ResolverUiDataSource.SelectedItem).AppMetadata;
            Close();
        }
    }

    public void Dispose()
    {
        _userCancellationTokenSource.Cancel();
        _userCancellationTokenSource.Dispose();
    }
}
