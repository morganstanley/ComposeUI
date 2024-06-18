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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Finos.Fdc3;


namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI.Pages;

/// <summary>
/// Interaction logic for AdvancedResolverUIPage.xaml
/// </summary>
public partial class AdvancedResolverUIPage : Page, IPageService
{
    private readonly ResolverUIPageViewModel _viewModel;

    public AdvancedResolverUIPage(IEnumerable<ResolverUIAppData> apps)
    {
        InitializeComponent();

        _viewModel = new ResolverUIPageViewModel(this, apps);
        DataContext = _viewModel;
    }

    public void ClosePage(IAppMetadata appMetadata)
    {
        var window = Window.GetWindow(this);
        if (window is Fdc3ResolverUI resolverUIWindow)
        {
            resolverUIWindow.AppMetadata = appMetadata;
            resolverUIWindow.Close();
        }
    }

    private void ResolverUIDataSource_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            if (dataGrid.SelectionMode != DataGridSelectionMode.Single)
            {
                ClosePage(null);
            }

            if (dataGrid.SelectedItem is ResolverUIAppData resolverUIAppData)
            {
                _viewModel.DoubleClickListBox(resolverUIAppData);
            }
        }
    }
}
