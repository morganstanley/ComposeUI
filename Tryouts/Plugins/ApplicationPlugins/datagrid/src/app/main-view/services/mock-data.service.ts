/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { Injectable } from '@angular/core';
import { interval, Subject } from 'rxjs';
import { Market } from './mock-data';
import { MessageRouter, TopicMessage, createMessageRouter } from "@morgan-stanley/composeui-messaging-client";

@Injectable({
  providedIn: 'root'
})

export class MockDataService{
  public subject: Subject<any> = new Subject<any>();
  public marketData: any;
  private market: Market;
  private client: MessageRouter;
  private connected: Boolean;
  private connecting: Promise<void>;

  constructor(){
    this.market = new Market();
    // TODO: Inject the MessageRouter
    this.client = createMessageRouter();

    interval(1000).subscribe(() => {
      this.marketData = this.market.generateNewMarketNumbers();
      this.subject.next(this.marketData);
    });

    this.connecting = new Promise(async (resolve, reject) => {
      try{
        await this.checkMessageRouterConnection();
        resolve(undefined);
      }catch(exception){
        console.log(exception);
        reject(exception);
      }
    });
  }

  public async publishSymbolData(symbol: any|undefined) {
    await this.connecting;

    if(symbol != undefined){
      //Publishing to DataService
      // let serialized = JSON.stringify(symbol);
      // await this.client.publish('proto_select_marketData', serialized);

      //Here we are connecting to the chart example without the DataService Example
      let marketSymbol = this.market.createMarketSymbol(symbol);
      let serialized = JSON.stringify(marketSymbol);
      await this.client.publish('proto_select_monthlySales', serialized);
    }
  }

  public async requestSymbolData(topic: string){
    await this.connecting;

    await this.client.subscribe(topic, (message: TopicMessage) => {

      const payload = JSON.parse(message.payload!);

      //TODO: Get live data from DataService example.
      const symbol = payload.symbol;
      const buyData = payload.buy;
      const sellData = payload.sell;
    });
  }

  private async checkMessageRouterConnection(){
    if(!this.connected){
      await this.client.connect();
      this.connected = true;
    }
  }
}
