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
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

/// <summary>
/// Interaction logic for Fdc3ResolverUi.xaml
/// </summary>
public partial class Fdc3ResolverUI : Window, IDisposable
{
    private readonly Fdc3ResolverUIViewModel _viewModel;

    public IAppMetadata? AppMetadata { get; internal set; }
    public CancellationToken UserCancellationToken => _viewModel.UserCancellationToken;

    public Fdc3ResolverUI(IEnumerable<IAppMetadata> apps)
    {
        InitializeComponent();

        _viewModel = new Fdc3ResolverUIViewModel(apps);
        DataContext = _viewModel;
        _viewModel.SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, PageSizeChangedEventArgs e)
    {
        if (e.NewSize != Size.Empty)
        {
            Width = e.NewSize.Width;
            Height = e.NewSize.Height;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        // Check if the user previously set the AppMetadata property to a row, if not then the user clicked the X button
        if (AppMetadata == null)
        {
            _viewModel.CancelDialog();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelDialog();
        Close();
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        { 
            DragMove(); 
        }
    }
}
