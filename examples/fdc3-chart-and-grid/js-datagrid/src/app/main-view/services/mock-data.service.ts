/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { Injectable } from '@angular/core';
import { interval, Subject } from 'rxjs';
import { Market } from './mock-data';
import { AppIdentifier, Context, ContextTypes, IntentResolution } from '@finos/fdc3';
import { Symbol } from './../models/Symbol';

@Injectable({
  providedIn: 'root'
})
export class MockDataService{
  public subject: Subject<any> = new Subject<any>();
  public marketData: any;
  private market: Market;
  private intentResolution: IntentResolution;
  private readonly viewChartIntent: string = "ViewChart"; 

  constructor(){
    this.market = new Market();

    interval(1000).subscribe(() => {
      this.marketData = this.market.generateNewMarketNumbers();
      this.subject.next(this.marketData);
    });
  }

  public async publishSymbolData(symbol: Symbol | undefined): Promise<void> {
    if(symbol){
      let marketSymbol;
      //TODO: involve DataService to generate the data for the chart
      if (symbol) {
        marketSymbol = this.market.createMarketSymbol(symbol);
      }
      
      const context: Context = {
        type: ContextTypes.Chart,
        id: {
          ticker: marketSymbol?.symbol,
          buyData: marketSymbol?.buy,
          sellData: marketSymbol?.sell
        }
      }

      if (this.intentResolution?.source) {
        await window.fdc3.raiseIntent(this.viewChartIntent, context, this.intentResolution.source).catch(async(rejected) => {
          console.log("Error while raising intent, eg.: the window could be closed: ", rejected);
          this.intentResolution = await window.fdc3.raiseIntent(this.viewChartIntent, context);
        });
        
      } else {
        this.intentResolution = await window.fdc3.raiseIntent(this.viewChartIntent, context);
      }
      console.log("Result: ", await this.intentResolution.getResult());
    }
  }

  public async openChart(symbol: Symbol | undefined) : Promise<AppIdentifier> {
    let context: Context = {
      type: ContextTypes.Chart
    };

    if (symbol) {
      const marketSymbol = this.market.createMarketSymbol(symbol);
      context = {
        type: ContextTypes.Chart,
        id: {
          ticker: marketSymbol?.symbol,
          buyData: marketSymbol?.buy,
          sellData: marketSymbol?.sell
        }
      };
    }

    this.intentResolution = await window.fdc3.raiseIntent(this.viewChartIntent, context);

    return this.intentResolution.source;
  }
}
