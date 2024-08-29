/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Component, Inject, Input, NgZone, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogActions, MatDialogClose, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { Channel, Context, Listener } from '@finos/fdc3';
import { Subject } from 'rxjs';

export interface SymbolElement {
  symbol: string;
  quantity: number;
  trader: string;
}

@Component({
  selector: 'app-trade-idea-generator',
  templateUrl: './trade-idea-generator.component.html',
  styleUrl: './trade-idea-generator.component.scss'
})
export class TradeIdeaGeneratorComponent implements OnDestroy {
  public feedback?: string;
  public trader?: string;
  public channel?: Channel;
  public myFormGroup = new FormGroup({
    formField: new FormControl()
  });

  public currentValue: number = 1;
  public currentStep: number = 1;
  public minimumValue: number = 0;
  public maximumValue: number = Infinity;
  public wrapValue: boolean = false;
  public currentColor: string = 'default';
  public symbols: FormControl = new FormControl('');
  public symbolList: string[] = ['AAPL', 'SSNLF', 'RDY', 'ACIU', 'NVDA', 'MSFT', 'TSLA', 'SERV', 'CRWD', 'BTDR', 'RBRK', 'DAL', 'DIS']
  private listeners: Map<string, Listener> = new Map<string, Listener>();
  private feedbackSubject: Subject<string> = new Subject<string>();

  constructor(public dialog: MatDialog, private ngZone: NgZone) {
    this.feedbackSubject.subscribe(data => {
      this.ngZone.run(() => {
        this.feedback = data;
      })
    })
  }

  async ngOnDestroy() {
    this.listeners.forEach((listener, _) => {
      listener.unsubscribe();
    });

    this.listeners.clear();
  }

  @Input('value')
  set inputValue(_value: number) {
    this.currentValue = this.parseNumber(_value);
  }

  @Input()
  set step(step: number) {
    this.step = this.parseNumber(step);
  }

  @Input()
  set min(mimimum: number) {
    this.minimumValue = this.parseNumber(mimimum);
  }

  @Input()
  set max(maximum: number) {
    this.maximumValue = this.parseNumber(maximum);
  }

  @Input()
  set wrap(wrap: boolean) {
    this.wrapValue = this.parseBoolean(wrap);
  }

  private parseNumber(num: any): number {
    return +num;
  }

  private parseBoolean(bool: any): boolean {
    return !!bool;
  }

  public setColor(color: string): void {
    this.currentColor = color;
  }

  public getColor(): string {
    return this.currentColor
  }

  public incrementValue(step: number = 1): void {

    let inputValue = this.currentValue + step;

    if (this.wrap) {
      inputValue = this.wrappedValue(inputValue);
    }

    this.currentValue = inputValue;
  }

  private wrappedValue(inputValue: number): number {
    if (inputValue > this.maximumValue) {
      return this.minimumValue + inputValue - this.maximumValue;
    }

    if (inputValue < this.minimumValue) {

      if (this.maximumValue === Infinity) {
        return 0;
      }

      return this.maximumValue + inputValue;
    }

    return inputValue;
  }

  public shouldDisableDecrement(inputValue: number): boolean {
    return !this.wrapValue && inputValue <= this.minimumValue;
  }

  public shouldDisableIncrement(inputValue: number): boolean {
    return !this.wrapValue && inputValue >= this.maximumValue;
  }

  private async broadcastTradeIdea(action: string) : Promise<void> {
    if (!this.currentValue || this.currentValue <= 0) {
      this.feedbackSubject.next('Please select at least 1 quantity.');
      return;
    }

    if (!this.symbols.value) {
      this.feedbackSubject.next('Please select one or more symbols.');
      return;
    }

    if (!this.trader) {
      this.feedbackSubject.next('Please enter the trader\'s name.');
      return;
    }

    const dialogRef = this.dialog.open(ConfirmationDialog, {
      data: {trader: this.trader!, quantity: this.currentValue, symbol: this.symbols.value as string}
    });

    dialogRef.afterClosed().subscribe(async (result: boolean) => {
      if (!result) {
        return;
      }

      const context: Context = {
        type: "fdc3.trade",
        data: {
          symbol: this.symbols.value as string,
          timestamp: new Date().toISOString(),
          trader: this.trader,
          quantity: this.currentValue,
          action: action
        }
      };

      try {
        if (!this.channel) {
          this.channel = await window.fdc3.getOrCreateChannel('tradeIdeasChannel');
        }

        const topic: string = "fdc3." + this.symbols.value as string + "." + this.trader!;

        if (!this.listeners.get(topic)) {
          const listener = await this.channel.addContextListener(topic, (context, metadata) => {
            if (context['result'].success === false){
              this.feedbackSubject.next(context['result'].error as string);
              return;
            }

            const result = context['result'];
            if (result.action as string == "BUY") {
              const price: number = result.tradePrice as number;
              this.feedbackSubject.next(this.trader + " has bought " + this.currentValue + " symbol of: " + this.symbols.value as string + " for $" + price + ".");
            } else {
              this.feedbackSubject.next(this.trader + " has indicated that " + this.currentValue + " symbol of: " + this.symbols.value as string + " is/are available for buying.");
            }
          });
          this.listeners.set(topic, listener);
        }

        await this.channel.broadcast(context);

      } catch (error) {
        this.feedbackSubject.next('Failed to broadcast trade idea.');
        console.error('Error is thrown while broadcasting trade idea:', error);
      }
    });
  }

  public async buySymbol() : Promise<void> {
    await this.broadcastTradeIdea("BUY");
  }

  public async sellSymbol() : Promise<void> {
    await this.broadcastTradeIdea("SELL");
  }
}

@Component({
  selector: 'confirmation-dialog',
  templateUrl: './dialog.html',
  styleUrl: './trade-idea-generator.component.scss',
  standalone: true,
  imports:[
    MatDialogActions,
    MatDialogClose,
    MatDialogModule,
    MatFormFieldModule,
    FormsModule,
    MatButtonModule
  ]
})
export class ConfirmationDialog {
  constructor(public dialogRef: MatDialogRef<ConfirmationDialog>,
    @Inject(MAT_DIALOG_DATA) public data: SymbolElement) { }
}