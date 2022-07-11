﻿// /*
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

namespace ComposeUI.Example.WPFDataGrid.Models;

/// <summary>
/// Model for symbol. We can show the short and long market of the product. (The amount of the symbolRating's trading, the avarage profit of the stock's position)
/// </summary>
[Serializable]
public class SymbolModel
{
    /// <summary>
    /// Name for the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Fullname of the symbol.
    /// </summary>
    public string Fullname { get; set; } = string.Empty;

    /// <summary>
    /// Price of the symbol for the rating.
    /// </summary>
    public decimal AvarageProfit { get; set; }

    /// <summary>
    /// Count of the symbol.
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    /// Rating/type of the symbol. (LONG/SHORT)
    /// </summary>
    public SymbolRating SymbolRating { get; set; }

    /// <summary>
    /// Overriding ToString method, because we are sending strings.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $@"{{""Symbol"": ""{Symbol}"", ""Fullname"": ""{Fullname}"", ""AvarageProfit"": ""{AvarageProfit}"", ""Amount"": ""{Amount}"", ""SymbolRating"": ""{SymbolRating}"" }}";
    }
}
