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

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

/// <summary>
/// Interaction logic for ResolverUIIntent.xaml
/// </summary>
public partial class ResolverUIIntent : Window
{
    private readonly Fdc3ResolverUIIntentViewModel _viewModel;
    public ResolverUIIntent(IEnumerable<string> intents)
    {
        InitializeComponent();

        _viewModel = new(intents);
        DataContext = _viewModel;
    }

    public string? Intent { get; private set; }

    public CancellationToken UserCancellationToken => _viewModel.UserCancellationToken;

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelDialog();
        Close();
    }

    private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedIntent != null)
        {
            Intent = _viewModel.SelectedIntent.IntentName;
            Close();
        }
    }
}
