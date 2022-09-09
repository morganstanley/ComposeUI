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
using System.ComponentModel;

namespace ComposeUI.Example.WPFDataGrid.Models
{
    /// <summary>
    /// Rating/type of the symbol.
    /// </summary>
    public enum SymbolRating
    {
        /// <summary>
        /// Description for financial short: we want to sell our product on the public market with actual market price. 
        /// </summary>
        [Description("Short")]
        Short,

        /// <summary>
        /// Description for financial long: we want to buy product on the public market with actual market price, and we are hoping for this price to increase so we can make profit from them. 
        /// </summary>
        [Description("Long")]
        Long
    }
}

