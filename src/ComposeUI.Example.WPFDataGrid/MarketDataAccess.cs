/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections.Generic;
using ComposeUI.Example.WPFDataGrid.Models;

namespace ComposeUI.Example.WPFDataGrid
{
    public static class MarketDataAccess
    {
        private static List<SymbolModel> MarketData { get;} = new();

        public static List<SymbolModel> InitMarketData()
        {
            MarketData.Add(new() { Name = "Apple", Symbol = "AAPL", Amount = 1000, Price = 152968, SymbolRating = SymbolRating.Long });
            MarketData.Add(new() { Name = "Apple", Symbol = "AAPL", Amount = 549, Price = 54345, SymbolRating = SymbolRating.Short });
            MarketData.Add(new() { Name = "Google", Symbol = "GOOG", Amount = 2500, Price = 18643, SymbolRating = SymbolRating.Long });
            MarketData.Add(new() { Name = "Google", Symbol = "GOOG", Amount = 4897, Price = 1234, SymbolRating = SymbolRating.Short });
            MarketData.Add(new() { Name = "IBM", Symbol = "IBM", Amount = 6543, Price = 65496, SymbolRating = SymbolRating.Long });
            MarketData.Add(new() { Name = "IBM", Symbol = "IBM", Amount = 7894, Price = 464332, SymbolRating = SymbolRating.Short });
            MarketData.Add(new() { Name = "Samsung Electronics Co Ltd", Symbol = "Samsung", Amount = 9872, Price = 156313, SymbolRating = SymbolRating.Long });
            MarketData.Add(new() { Name = "Samsung Electronics Co Ltd", Symbol = "Samsung", Amount = 1234, Price = 789465, SymbolRating = SymbolRating.Short });
            MarketData.Add(new() { Name = "Tesla", Symbol = "TSLA", Amount = 45678, Price = 978546, SymbolRating = SymbolRating.Long });
            MarketData.Add(new() { Name = "Tesla", Symbol = "TSLA", Amount = 65478, Price = 987877, SymbolRating = SymbolRating.Short });
            return MarketData;
        }
    }
}
