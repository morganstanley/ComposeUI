/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule} from '@angular/material/table';
import { MatIconModule} from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatagridComponent } from './datagrid/datagrid.component';
import { MainViewRoutingModule } from './main-view-routing.module';
import { MainViewComponent } from './main-view.component';
import { MatButtonModule } from '@angular/material/button';


@NgModule({
    declarations: [
        MainViewComponent,
        DatagridComponent,
    ],
    imports: [
        MainViewRoutingModule,
        CommonModule,
        MatCheckboxModule,
        MatTableModule,
        MatToolbarModule,
        MatIconModule,
        MatInputModule,
        MatButtonModule,
        MatPaginatorModule,
        MatSortModule,
        MatProgressSpinnerModule
    ]
})
export class MainViewModule{

}