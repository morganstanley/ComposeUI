/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { AfterViewInit, ViewChild, Component } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ProcessInfo, ProcessTable } from 'src/app/DTOs/ProcessInfo';
import { ProcessesService } from 'src/app/services/processes.service';
import { animate, state, style, transition, trigger } from '@angular/animations';


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
})
export class ProcessesComponent implements AfterViewInit {
  expandedElement: any;
  processesData: Array<ProcessTable>;
  dataSource = new MatTableDataSource<ProcessTable>()

  displayedColumns = [{ key: 'ProcessName', header: 'Process Name' }, { key: 'PID', header: 'PID' }, { key: 'ProcessStatus', header: 'Process Status' }, { key: 'StartTime', header: 'Start Time' }, { key: 'ProcessorUsage', header: 'Processor Usage' }, { key: 'PhysicalMemoryUsageBit', header: 'Physical Mem Usage' }, { key: 'PriorityLevel', header: 'Priority Level' }, { key: 'VirtualMemorySize', header: 'Virtual Memory Size' }];
  displayedColumnsKeys: string[];

  @ViewChild(MatPaginator) paginator: MatPaginator;

  constructor(private processService: ProcessesService) {
    this.processService.getProcesses('Processes').subscribe(process => this.processesData = process);
    this.displayedColumnsKeys = this.displayedColumns.map(column => column.key);
    this.dataSource = new MatTableDataSource<ProcessTable>(this.processesData);
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }
  ngOnInit() {
  }

  getKeys(object: any): string[] {
    return Object.keys(object);
  }

  onItemSelected(idx: number) {
    console.log(idx);
  }
}

