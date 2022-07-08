/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ComposeUI.Example.WPFDataGrid.Models;
using ComposeUI.Messaging.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;

namespace ComposeUI.Example.WPFDataGrid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        private readonly ILogger<ShellView>? _logger;
        private readonly IMessageRouter? _messageRouter;
        public ShellView(ILogger<ShellView> logger, IMessageRouter messageRouter)
        {
            _logger = logger;
            _messageRouter = messageRouter;
            InitializeComponent();
        }

        private void FillingDataGridWithMarketData()
        {
            DataTable table = new();
            CreateColumns(table);
            FillTable(table);
            MyDataGridMarketData.ItemsSource = table.DefaultView;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FillingDataGridWithMarketData();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var rows = GetSelectedRows();
                if(rows is not null)
                {
                    foreach (DataGridRow row in rows)
                    {
                        if (row is not null && row.IsSelected == true)
                        {
                            var selectedRow = row.DataContext as DataRowView;
                            var selectedObject = new SymbolModel()
                            {
                                Name = selectedRow?.Row.Field<string>("Name"),
                                Symbol = selectedRow?.Row.Field<string>("Symbol"),
                                Amount = (double) selectedRow?.Row.Field<double>("Amount"),
                                Price = (decimal) selectedRow?.Row.Field<decimal>("Price"),
                                SymbolRating = (SymbolRating) selectedRow?.Row.Field<SymbolRating>("SymbolRating")
                            };

                            var message = string.Format("You have selected: {0}", selectedObject?.Name);
                            Debug.WriteLine(message);
                            _logger?.LogInformation(message);

                            var payload = ObjetToByte(selectedObject);

                            if (payload != null)
                            {
                                _messageRouter?.PublishAsync("proto_marketData", payload);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
                _logger?.LogError(exception.Message);
            }
        }

        private IEnumerable<DataGridRow>? GetSelectedRows()
        {
            var source = MyDataGridMarketData.ItemsSource;
            if(source != null)
            {
                foreach (var item in source)
                {
                    var row = MyDataGridMarketData.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if(row != null)
                    {
                        yield return row;
                    }
                }
            }
        }

        private void FillTable(DataTable table)
        {
            var list = MarketDataAccess.InitMarketData();
            for (int i = 0; i < list.Count; i++)
            {
                DataRow row = table.NewRow();
                row["Name"] = list[i].Name;
                row["Symbol"] = list[i].Symbol;
                row["Amount"] = list[i].Amount;
                row["Price"] = list[i].Price;
                row["SymbolRating"] = list[i].SymbolRating;

                table.Rows.Add(row);
            }
        }
        private void CreateColumns(DataTable table)
        {
            DataColumn symbol = new DataColumn("Symbol", typeof(string));
            DataColumn name = new DataColumn("Name", typeof(string));
            DataColumn amount = new DataColumn("Amount", typeof(double));
            DataColumn price = new DataColumn("Price", typeof(decimal));
            DataColumn symbolRating = new DataColumn("SymbolRating", typeof(SymbolRating));

            table.Columns.Add(symbol);
            table.Columns.Add(name);
            table.Columns.Add(price);
            table.Columns.Add(amount);
            table.Columns.Add(symbolRating);
        }

        private byte[]? ObjetToByte(object? obj)
        {
            if(obj != null)
            {
                BinaryFormatter bf = new();
                using(MemoryStream ms = new())
                {
                    try
                    {
                        bf.Serialize(ms, (object)obj);
                        return ms.ToArray();
                    }
                    catch(Exception ex)
                    {
                        _logger?.LogError(ex.ToString(), ex);
                    }
                }
            }
            return null;
        }
    }
}
