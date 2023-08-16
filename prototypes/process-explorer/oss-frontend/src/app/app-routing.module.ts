import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProcessesComponent } from './components/processes/processes.component';

const routes: Routes = [
  {path: '', component: ProcessesComponent},
  {path: 'processes', component: ProcessesComponent}

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
