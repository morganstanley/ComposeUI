import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MockProcesses } from './mock-processes';

@Injectable({
  providedIn: 'root'
})
export class MockProcessesService {
  public getData(tableName: string): Observable<any[]> {
    return of(MockProcesses[tableName]);
  }
}
