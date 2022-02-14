import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MasterViewRoutingModule } from './master-view-routing.module';
import { MasterViewComponent } from './master-view.component';
import { ConnectionsComponent } from './connections/connections.component';
import { IgxListModule, IgxAvatarModule, IgxIconModule, IgxGridModule, IgxActionStripModule, IgxButtonModule, IgxButtonGroupModule, IgxCheckboxModule, IgxSelectModule, IgxNavbarModule, IgxToggleModule, IgxNavigationDrawerModule } from 'igniteui-angular';
import { FormsModule } from '@angular/forms';
import { ProcessesComponent } from './processes/processes.component';
import { MemoryChartComponent } from './memory-chart/memory-chart.component';

import { HighchartsChartModule } from 'highcharts-angular';

import { 
	IgxDoughnutChartModule,
	IgxRingSeriesModule,
	IgxLegendModule,
	IgxItemLegendModule
 } from "igniteui-angular-charts";



@NgModule({
  declarations: [
    MasterViewComponent,
    ConnectionsComponent,
    ProcessesComponent,
    MemoryChartComponent
  ],
  imports: [
    HighchartsChartModule,
    CommonModule,
    MasterViewRoutingModule,
    IgxListModule,
    IgxAvatarModule,
    IgxIconModule,
    IgxGridModule,
    IgxActionStripModule,
    FormsModule,
    IgxButtonModule,
    IgxButtonGroupModule,
    IgxCheckboxModule,
    IgxSelectModule,
    IgxNavbarModule,
    IgxToggleModule,
    IgxNavigationDrawerModule,
    IgxDoughnutChartModule,
	  IgxRingSeriesModule,
	  IgxLegendModule,
	  IgxItemLegendModule
  ]
})
export class MasterViewModule {
}
