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

using ComposeUI.Example.WPFDataGrid.Models;
using ComposeUI.Messaging.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace ComposeUI.Example.WPFDataGrid;

/// <summary>
/// Interaction logic for ShellView.xaml
/// </summary>
public partial class ShellView : Window
{
    private readonly ILogger<ShellView>? _logger;
    private readonly IMessageRouter _messageRouterClient;
    private readonly List<SymbolModel> _symbols;

    /// <summary>
    /// Constructor for the View.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageRouterClient"></param>
    public ShellView(ILogger<ShellView> logger, IMessageRouter messageRouterClient)
    {
        _logger = logger;
        _messageRouterClient = messageRouterClient;
        _symbols = MarketDataAccess.InitMarketData();
        InitializeComponent();
    }

    private void FillingDataGridWithMarketData()
    {
        DataTable table = new();
        CreateColumns(table);
        FillTable(table);
        MyDataGridMarketData.ItemsSource = table.DefaultView;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FillingDataGridWithMarketData();
        try
        {
            foreach (var symbol in _symbols)
            {
                await _messageRouterClient.PublishAsync("proto_register_marketData", symbol.ToString());
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception.ToString());
        }
    }

    private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var rows = GetSelectedRows();
            if (rows is not null)
            {
                foreach (var row in rows)
                {
                    if (row is not null && row.IsSelected == true)
                    {
                        var selectedRow = row.DataContext as DataRowView;
                        if (selectedRow is not null)
                        {
                            var selectedObject = new SymbolModel()
                            {
                                Fullname = selectedRow.Row.Field<string>("Name") ?? string.Empty,
                                Symbol = selectedRow.Row.Field<string>("Symbol") ?? string.Empty,
                                Amount = (double) selectedRow.Row.Field<double>("Amount"),
                                AvarageProfit = selectedRow.Row.Field<decimal>("Price"),
                                SymbolRating = selectedRow.Row.Field<SymbolRating>("SymbolRating")
                            };

                            if (selectedObject is not null)
                            {
                                var message = string.Format("You have selected: {0}", selectedObject.Fullname);
                                _logger?.LogInformation(message);
                                await _messageRouterClient.PublishAsync("proto_select_marketData", selectedObject.ToString());
                            }
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception.Message);
        }
    }

    private IEnumerable<DataGridRow>? GetSelectedRows()
    {
        var source = MyDataGridMarketData.ItemsSource;
        if (source != null)
        {
            foreach (var item in source)
            {
                var row = MyDataGridMarketData.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    yield return row;
                }
            }
        }
    }

    private void FillTable(DataTable table)
    {
        for (var i = 0; i < _symbols.Count; i++)
        {
            var row = table.NewRow();
            row["Name"] = _symbols[i].Fullname;
            row["Symbol"] = _symbols[i].Symbol;
            row["Amount"] = _symbols[i].Amount;
            row["Price"] = _symbols[i].AvarageProfit;
            row["SymbolRating"] = _symbols[i].SymbolRating;

            table.Rows.Add(row);
        }
    }
    private void CreateColumns(DataTable table)
    {
        var symbol = new DataColumn("Symbol", typeof(string));
        var name = new DataColumn("Name", typeof(string));
        var amount = new DataColumn("Amount", typeof(double));
        var price = new DataColumn("Price", typeof(decimal));
        var symbolRating = new DataColumn("SymbolRating", typeof(SymbolRating));

        table.Columns.Add(symbol);
        table.Columns.Add(name);
        table.Columns.Add(price);
        table.Columns.Add(amount);
        table.Columns.Add(symbolRating);
    }
}
