/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MainViewRoutingModule } from './main-view-routing.module';
import { MainViewComponent } from './main-view.component';
import { ConnectionsComponent } from './connections/connections.component';
import {
  IgxListModule, IgxAvatarModule, IgxIconModule, IgxGridModule, IgxActionStripModule, IgxButtonModule,
  IgxButtonGroupModule, IgxCheckboxModule, IgxSelectModule, IgxNavbarModule, IgxToggleModule,
  IgxNavigationDrawerModule,  IgxTreeGridModule } from 'igniteui-angular';
import { FormsModule } from '@angular/forms';
import { ProcessesComponent } from './processes/processes.component';

import { SubsystemComponent } from './subsystems/subsystem.component';

@NgModule({
  declarations: [
    MainViewComponent,
    ConnectionsComponent,
    ProcessesComponent,
    SubsystemComponent
  ],
  imports: [
    CommonModule,
    MainViewRoutingModule,
    IgxListModule,
    IgxAvatarModule,
    IgxIconModule,
    IgxGridModule,
    IgxActionStripModule,
    IgxCheckboxModule,
    FormsModule,
    IgxButtonModule,
    IgxButtonGroupModule,
    IgxCheckboxModule,
    IgxSelectModule,
    IgxNavbarModule,
    IgxToggleModule,
    IgxNavigationDrawerModule,
    IgxTreeGridModule
  ]
})
export class MainViewModule {
}
