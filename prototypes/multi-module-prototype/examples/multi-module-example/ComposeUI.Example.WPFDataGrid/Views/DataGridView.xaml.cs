// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using WPFDataGrid.Models;

namespace WPFDataGrid.Views;

/// <summary>
/// Interaction logic for ShellView.xaml
/// </summary>
public partial class DataGridView : Window
{
    private readonly ILogger<DataGridView> _logger;
    private readonly IMessageRouter _messageRouter;
    private readonly ObservableCollection<SymbolModel> _symbols;

    /// <summary>
    /// Constructor for the View.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageRouter"></param>
    public DataGridView(
        ILogger<DataGridView>? logger,
        IMessageRouter messageRouter)
    {
        _logger = logger ?? NullLogger<DataGridView>.Instance;
        _messageRouter = messageRouter;
        _symbols = new(MarketDataAccess.MarketData);
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        MyDataGridMarketData.ItemsSource = _symbols;
    }

    private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var selectedObject = e.AddedItems.Cast<SymbolModel>().FirstOrDefault();

            if (selectedObject != null)
            {
                _logger.LogInformation(string.Format("You have selected: {0}", selectedObject.Fullname));
                await _messageRouter.PublishJsonAsync("proto_select_marketData", selectedObject, SymbolModel.JsonSerializerOptions);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.ToString());
        }
    }
}