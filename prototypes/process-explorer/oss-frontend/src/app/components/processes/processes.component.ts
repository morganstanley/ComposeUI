/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { AfterViewInit, ViewChild, Component } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ProcessInfo, ProcessTable } from 'src/app/DTOs/ProcessInfo';
import { ProcessesService } from 'src/app/services/processes-service/processes.service';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { MatSort, Sort } from '@angular/material/sort';
import {LiveAnnouncer} from '@angular/cdk/a11y';
import { of } from 'rxjs';
import { Process } from 'src/app/generated-protos-files/ProcessExplorerMessages_pb';


@Component({
    selector: 'app-processes',
    templateUrl: './processes.component.html',
    styleUrls: ['./processes.component.scss'],
    animations: [
        trigger('detailExpand', [
            state('collapsed', style({ height: '0px', minHeight: '0' })),
            state('expanded', style({ height: '*' })),
            transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),
    ],
    standalone: false
})
export class ProcessesComponent {
  expandedElement: any;
  processesData: Array<ProcessTable>; 
  dataSource = new MatTableDataSource<Process.AsObject>();
  allProcesses: Array<Process.AsObject> = [];

  displayedColumns = [{ key: 'processname', header: 'Process Name' }, { key: 'processid', header: 'PID' }, { key: 'processstatus', header: 'Process Status' }, { key: 'starttime', header: 'Start Time' }, { key: 'processorusage', header: 'Processor Usage' }, { key: 'physicalmemoryusagebit', header: 'Physical Mem Usage' }, { key: 'processpriorityclass', header: 'Priority Level' }, { key: 'virtualmemorysize', header: 'Virtual Memory Size' }];
  displayedColumnsKeys: string[];

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(private processService: ProcessesService, private liveAnnouncer: LiveAnnouncer) {
    this.processService.getProcessesData().on("data", req => {
          if(req.toObject().processesList.length > 0){
            req.toObject().processesList.forEach(newProcess => {
            const existingIndex = this.allProcesses.findIndex(existingProcess => existingProcess.processid === newProcess.processid);
            if(existingIndex !== -1){
              this.allProcesses[existingIndex] = newProcess
            } else {
              this.allProcesses.push(newProcess);
            }
            });
          }
          this.displayedColumnsKeys = this.displayedColumns.map(column => column.key);
          this.dataSource = new MatTableDataSource<Process.AsObject>(this.allProcesses);

          this.dataSource.paginator = this.paginator;
          this.dataSource.sort = this.sort;
        })
  }

  getKeys(object: any): string[] {
    return Object.keys(object);
  }

  onItemSelected(idx: number) {
    console.log(idx);
  }

  announceSortChange(sortState: Sort) {
    if (sortState.direction) {
      this.liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this.liveAnnouncer.announce('Sorting cleared');
    }
  }
}

