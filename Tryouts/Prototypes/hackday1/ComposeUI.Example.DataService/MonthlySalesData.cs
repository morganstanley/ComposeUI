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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComposeUI.Example.DataService
{
    internal class MonthlySalesData
    {
        //todo internal readonly 
        //Todo  IReadOnlyCollection

        internal static Dictionary<string , MonthlySalesDataModel> MyList { get; } = new Dictionary<string, MonthlySalesDataModel>()
        {
             ["IBM"] = new()
             {
                Symbol = "IBM",
                Buy = new int[] { 49, 71, 106, 129, 144, 63, 89, 15, 203, 58, 115, 32 },
                Sell = new int[] { 83, 78, 98, 93, 106, 82, 45, 305, 263, 33, 112, 87 },
             },


            ["AAPL"] = new()
            {
                Symbol = "AAPL",
                Buy = new int[] { 18, 49, 62, 110, 134, 162, 166, 210, 215, 277, 290, 297 },
                Sell = new int[] { 62, 93, 114, 140, 150, 161, 191, 206, 224, 255, 286, 295 },
            },

            ["TSLA"] = new()
            {
                Symbol = "TSLA",
                Buy = new int[] { 33, 93, 124, 177, 187, 210, 225, 234, 236, 247, 250, 282 },
                Sell = new int[] { 33, 62, 64, 94, 124, 166, 186, 191, 225, 247, 267, 294 },
            },

            ["SMSN"] = new()
            {
                Symbol = "SMSN",
                Buy = new int[] { 14, 25, 73, 84, 138, 155, 181, 195, 200, 209, 231, 254 },
                Sell = new int[] { 30, 37, 67, 86, 182, 199, 219, 225, 238, 245, 250, 262 },
            },

            ["GOOG"] = new()
            {
                Symbol = "GOOG",
                Buy = new int[] { 25, 42, 43, 175, 189, 190, 201, 218, 223, 231, 263, 284 },
                Sell = new int[] { 28, 29, 57, 109, 129, 184, 196, 224, 230, 259, 277, 278 }
            }
        };
    }
}
