/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { SubsystemInfo } from 'src/app/DTOs/SubsystemInfo';
import { SubsystemsService } from 'src/app/services/subsystems-service/subsystems.service';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';

@Component({
    selector: 'app-subsystems',
    templateUrl: './subsystems.component.html',
    styleUrls: ['./subsystems.component.scss'],
    standalone: false
})
export class SubsystemsComponent {
  subsysemsData: Array<SubsystemInfo>;
  displayedColumns: string[] = ['Name', 'Id', 'State', 'ModuleType'];
  dataSource = new MatTableDataSource<SubsystemInfo>();

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(private subsystemsService: SubsystemsService, private liveAnnouncer: LiveAnnouncer){
    this.subsystemsService.getSubsystems('Subsystems').subscribe(subsystem => this.subsysemsData = subsystem);
    this.dataSource = new MatTableDataSource<SubsystemInfo>(this.subsysemsData);
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  announceSortChange(sortState: Sort) {
    if (sortState.direction) {
      this.liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this.liveAnnouncer.announce('Sorting cleared');
    }
  }
}
