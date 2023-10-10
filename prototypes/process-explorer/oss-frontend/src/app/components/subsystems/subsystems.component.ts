import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Subsystems } from 'src/app/DTOs/Subsystems';

@Component({
  selector: 'app-subsystems',
  templateUrl: './subsystems.component.html',
  styleUrls: ['./subsystems.component.scss']
})
export class SubsystemsComponent {
  displayedColumns: string[] = ['Name', 'Path', 'AutomatedStart', 'State'];
  dataSource = new MatTableDataSource<Subsystems>(ELEMENT_DATA);

  @ViewChild(MatPaginator) paginator: MatPaginator;

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }
}

const ELEMENT_DATA: Subsystems[] = [
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},
  {Name: 'subsystem', Path: '/path/file', AutomatedStart: true , State: 'running'},

   
];