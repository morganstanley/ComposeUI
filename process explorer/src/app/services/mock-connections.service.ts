import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MockConnections } from './mock-connections';

@Injectable({
  providedIn: 'root'
})
export class MockConnectionsService {
  public getData(tableName: string): Observable<any[]> {
   
    return of(MockConnections[tableName]);
  }
}


