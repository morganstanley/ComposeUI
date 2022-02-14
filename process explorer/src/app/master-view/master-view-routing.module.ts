import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MasterViewComponent } from './master-view.component';
import { ConnectionsComponent } from './connections/connections.component';
import { ProcessesComponent } from './processes/processes.component';
import { MemoryChartComponent } from './memory-chart/memory-chart.component';

const routes: Routes = [
  { 
    path: '', 
    component: MasterViewComponent, 
    children: [
      { 
        path: '', 
        redirectTo: 'connections', 
        pathMatch: 'full'
      }, { 
        path: 'connections', 
        component: ConnectionsComponent, 
        data: { text: 'Connections'}
      }, { 
        path: 'processes', 
        component: ProcessesComponent, 
        data: { text: 'Processes'}
      }, { 
        path: 'memory-chart', 
        component: MemoryChartComponent, 
        data: { text: 'Memory chart'}
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MasterViewRoutingModule {
}
