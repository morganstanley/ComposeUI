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
using System.Collections.Generic;
using ComposeUI.Example.WPFDataGrid.Models;

namespace ComposeUI.Example.WPFDataGrid;

/// <summary>
/// Initializes the data for the DataGrid.
/// </summary>
internal static class MarketDataAccess
{
    internal static IReadOnlyCollection<SymbolModel> MarketData { get; } = new List<SymbolModel>()
    {
        new() { Fullname = "Google", Symbol = "GOOG", Amount = 2500, AverageProfit = 18643, SymbolRating = SymbolRating.Long },
        new() { Fullname = "Apple", Symbol = "AAPL", Amount = 549, AverageProfit = 54345, SymbolRating = SymbolRating.Short },
        new() { Fullname = "Apple", Symbol = "AAPL", Amount = 1000, AverageProfit = 152968, SymbolRating = SymbolRating.Long },
        new() { Fullname = "Google", Symbol = "GOOG", Amount = 4897, AverageProfit = 1234, SymbolRating = SymbolRating.Short },
        new() { Fullname = "IBM", Symbol = "IBM", Amount = 6543, AverageProfit = 65496, SymbolRating = SymbolRating.Long },
        new() { Fullname = "IBM", Symbol = "IBM", Amount = 7894, AverageProfit = 464332, SymbolRating = SymbolRating.Short },
        new() { Fullname = "Samsung Electronics Co Ltd", Symbol = "SMSN", Amount = 9872, AverageProfit = 156313, SymbolRating = SymbolRating.Long },
        new() { Fullname = "Samsung Electronics Co Ltd", Symbol = "SMSN", Amount = 1234, AverageProfit = 789465, SymbolRating = SymbolRating.Short },
        new() { Fullname = "Tesla", Symbol = "TSLA", Amount = 45678, AverageProfit = 978546, SymbolRating = SymbolRating.Long },
        new() { Fullname = "Tesla", Symbol = "TSLA", Amount = 65478, AverageProfit = 987877, SymbolRating = SymbolRating.Short }
    };
}
