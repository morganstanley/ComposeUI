/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { MarketSymbol } from '../models/MarketSymbol';
import { Symbol } from '../models/Symbol';

export class Market{
    public MarketData: Symbol[];
    private symbols: Map<string, MarketSymbol> = new Map<string, MarketSymbol>([
        ['AAPL', {symbol: 'AAPL', sell: [ 62, 93, 114, 140, 150, 161, 191, 206, 224, 255, 286, 295], buy: [18, 49, 62, 110, 134, 162, 166, 210, 215, 277, 290, 297] }],
        ['IBM', {symbol: 'IBM', sell: [ 83, 78, 98, 93, 106, 82, 45, 305, 263, 33, 112, 87 ], buy: [49, 71, 106, 129, 144, 63, 89, 15, 203, 58, 115, 32] }],
        ['TSLA', {symbol: 'TSLA', sell: [ 33, 62, 64, 94, 124, 166, 186, 191, 225, 247, 267, 294], buy: [33, 93, 124, 177, 187, 210, 225, 234, 236, 247, 250, 282] }],
        ['SMSN', {symbol:'SMSN', sell: [30, 37, 67, 86, 182, 199, 219, 225, 238, 245, 250, 262], buy: [14, 25, 73, 84, 138, 155, 181, 195, 200, 209, 231, 254] }],
        ['GOOG', {symbol: 'GOOG', sell: [28, 29, 57, 109, 129, 184, 196, 224, 230, 259, 277, 278], buy: [25, 42, 43, 175, 189, 190, 201, 218, 223, 231, 263, 284] }]
    ]);

    constructor(){
        this.MarketData = this.generateNewMarketNumbers();
    }

    public generateNewMarketNumbers(): Symbol[] {
        let temp = [{
            "symbol": "GOOG",
            "fullname": "Google",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "long",
        },{
            "symbol": "GOOG",
            "fullname": "Google",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "short",
        },{
            "symbol": "AAPL",
            "fullname": "Apple",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "long",
        },{
            "symbol": "AAPL",
            "fullname": "Apple",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "short",
        },{
            "symbol": "IBM",
            "fullname": "IBM",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "long",
        },{
            "symbol": "IBM",
            "fullname": "IBM",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "short",
        },{
            "symbol": "SMSN",
            "fullname": "Samsung Electronics Co Ltd",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "long",
        },{
            "symbol": "SMSN",
            "fullname": "Samsung Electronics Co Ltd",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "short",
        },{
            "symbol": "TSLA",
            "fullname": "Tesla",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "long",
        },{
            "symbol": "TSLA",
            "fullname": "Tesla",
            "amount": Math.floor(Math.random() * 2000),
            "avarageProfit": Math.floor(Math.random() * 50000),
            "symbolRating": "short",
        }];

        return temp;
    }

    public createMarketSymbol(symbol: Symbol) : MarketSymbol | undefined{
        return this.symbols.get(symbol.symbol!);
    }
}