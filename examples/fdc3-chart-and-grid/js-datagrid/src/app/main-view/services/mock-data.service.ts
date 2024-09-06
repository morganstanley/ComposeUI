/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { Injectable } from '@angular/core';
import { interval, Subject } from 'rxjs';
import { Market } from './mock-data';
import { AppIdentifier, Channel, Context, ContextTypes, IntentResolution, Intents } from '@finos/fdc3';
import { Symbol } from './../models/Symbol';

@Injectable({
  providedIn: 'root'
})
export class MockDataService{
  public subject: Subject<any> = new Subject<any>();
  public marketData: any;
  private market: Market;
  private intentResolution: IntentResolution;
  private currentChannel: Channel | null;
  private connected: Boolean = false;
  private connecting: Promise<void>;

  constructor(){
      this.market = new Market();

      window.addEventListener('fdc3Ready', () => {
        this.connecting = new Promise(async(resolve, reject) => {
          try{
            resolve(await this.checkFdc3Connection());
          } catch(err) {
            reject(err);
          };
        });
      });

      interval(1000).subscribe(() => {
        this.marketData = this.market.generateNewMarketNumbers();
        this.subject.next(this.marketData);
      });
  }

  private async checkFdc3Connection(): Promise<void> {
    if(!this.connected) {
      this.currentChannel = await window.fdc3.getCurrentChannel();
      if (!this.currentChannel) {
        await window.fdc3.joinUserChannel("fdc3.channel.1");
      }
      this.connected = true;
    }
  }

  public async publishSymbolData(symbol: Symbol | undefined): Promise<void> {
    await this.connecting;

    if(symbol){      
      const context: Context = {
        type: ContextTypes.Instrument,
        id: {
          ticker: symbol?.symbol
        }
      }

      await window.fdc3.broadcast(context);
    }
  }

  public async openChart(symbol: Symbol | undefined) : Promise<AppIdentifier> {
    let context: Context = {
      type: ContextTypes.Instrument
    };

    if (symbol) {
      context = {
        type: ContextTypes.Instrument,
        id: {
          ticker: symbol?.symbol
        }
      };
    }

    if (this.intentResolution?.source) {
      await window.fdc3.raiseIntent(Intents.ViewChart, context, this.intentResolution.source).catch(async(rejected) => {
        console.log("Error while raising intent, eg.: the window could be closed: ", rejected);
        this.intentResolution = await window.fdc3.raiseIntent(Intents.ViewChart, context);
      }); 
    } else {
      this.intentResolution = await window.fdc3.raiseIntent(Intents.ViewChart, context);
    }

    return this.intentResolution.source;
  }
}
