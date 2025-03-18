/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { SelectionModel } from '@angular/cdk/collections';
import { AfterViewInit, ChangeDetectorRef, Component, NgZone, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, MatPaginatorIntl, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Symbol } from '../models/Symbol';
import { MockDataService } from '../services/mock-data.service';

@Component({
  selector: 'app-datagrid',
  templateUrl: './datagrid.component.html',
  styleUrls: ['./datagrid.component.scss']
})

export class DatagridComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator: MatPaginator = new MatPaginator(new MatPaginatorIntl(), ChangeDetectorRef.prototype);

  public marketData: MatTableDataSource<Symbol> = new MatTableDataSource<Symbol>();
  public displayedColumns: string[] = ['symbol', 'fullname', 'avarageProfit', 'amount', 'symbolRating'];
  public selection: SelectionModel<Symbol>;  
  
  public lowValue: number = 0;
  public highValue: number = 5;

  private chartWindow: Window|null;
  
  constructor(private ngZone: NgZone, private mockDataService: MockDataService){
    this.selection = new SelectionModel<Symbol>(false, []);
  }

  ngOnInit(){
    const subscribingToMarketData = this.mockDataService.subject
      .subscribe((data) => {
        this.marketData.data = data;
      });
      
    this.paginator._intl.itemsPerPageLabel = "5";
  }

  ngAfterViewInit() {
    this.marketData.paginator = this.paginator;
    this.paginator.page.subscribe(
       (event) => console.log(event));
  }

  public async onRowClicked(symbol: Symbol){
    console.log(symbol.symbol);
    await this.mockDataService.publishSymbolData(symbol);
  }

  public toggleRow() {
    this.selection.clear();
    this.marketData.data.forEach(row => this.selection.select(row));
  }

  public getPaginatorData(event: PageEvent): PageEvent {
    this.lowValue = event.pageIndex * event.pageSize;
    this.highValue = this.lowValue + event.pageSize;
    return event;
  }
}
