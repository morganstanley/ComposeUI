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
using System.Windows.Controls;
using System.Windows.Input;
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi.Pages;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi;

/// <summary>
/// Interaction logic for Fdc3ResolverUi.xaml
/// </summary>
public partial class Fdc3ResolverUi : Window, IDisposable
{
    private ILogger<Fdc3ResolverUi> _logger;
    private readonly Page _simpleResolverUiPage;
    private readonly Size _simpleResolverUiSize = new(500, 400);
    private readonly Page _advancedResolverUiPage;
    private readonly Size _advancedResolverUiSize = new(800, 600);
    private Page? _currentPage;
    private readonly List<ResolverUiAppData> _appData = new();
    private readonly CancellationTokenSource _userCancellationTokenSource;

    public IAppMetadata? AppMetadata { get; internal set; }
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
                AppId = app.AppId,
                AppMetadata = app,
                Icon = app.Icons.FirstOrDefault() //First Icon from the array will be shown on the ResolverUi
            });
        }

        _simpleResolverUiPage = new SimpleResolverUiPage(_appData);
        _advancedResolverUiPage = new AdvancedResolverUiPage(_appData);

        SetCurrentPageToSimpleView();
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

    public void Dispose()
    {
        _userCancellationTokenSource.Cancel();
        _userCancellationTokenSource.Dispose();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        { 
            DragMove(); 
        }
    }

    private void SetCurrentPageToSimpleView()
    {
        ResolverUiFrame.Content = _simpleResolverUiPage;
        Width = _simpleResolverUiSize.Width;
        Height = _simpleResolverUiSize.Height;
        _currentPage = _simpleResolverUiPage;
    }

    private void SetCurrentPageToAdvancedView()
    {
        ResolverUiFrame.Content = _advancedResolverUiPage;
        Width = _advancedResolverUiSize.Width;
        Height = _advancedResolverUiSize.Height;
        _currentPage = _advancedResolverUiPage;
    }

    private void OpenSimpleResolverUi(object sender, RoutedEventArgs e)
    {
        if (_currentPage != null
            && _currentPage != _simpleResolverUiPage)
        {
            SetCurrentPageToSimpleView();
        }
    }

    private void OpenAdvancedResolverUi(object sender, RoutedEventArgs e)
    {
        if (_currentPage != null
            && _currentPage != _advancedResolverUiPage)
        {
            SetCurrentPageToAdvancedView();
        }
    }
}
