import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MockProcesses } from './mock-processes';
import { ProcessTable } from '../DTOs/ProcessInfo';

@Injectable({
  providedIn: 'root'
})
export class ProcessesService {
  public getProcesses(tableName: string): Observable<ProcessTable[]> {
    return of(MockProcesses[tableName]);
  }
}
