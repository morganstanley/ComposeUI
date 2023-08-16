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

  displayedColumns = [{ key: 'ProcessName', header: 'Process name' }, { key: 'PID', header: 'PID' }, { key: 'ProcessStatus', header: 'Process status' }, { key: 'StartTime', header: 'Start time' }, { key: 'ProcessorUsage', header: 'Processor usage' }, { key: 'PhysicalMemoryUsageBit', header: 'Physical mem usage' }, { key: 'PriorityLevel', header: 'Priority level' }, { key: 'VirtualMemorySize', header: 'Virtual memory size' }];
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

