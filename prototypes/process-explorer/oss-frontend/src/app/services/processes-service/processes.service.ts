/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MockProcesses } from './mock-processes';
import { ProcessTable } from '../../DTOs/ProcessInfo';
import { ProcessExplorerMessageHandlerClient } from 'src/app/generated-protos-files/ProcessExplorerMessages_pb_service';
import { Message, Process } from 'src/app/generated-protos-files/ProcessExplorerMessages_pb';
import { Empty } from 'google-protobuf/google/protobuf/empty_pb';

@Injectable({
  providedIn: 'root'
})
export class ProcessesService {

  public getProcesses(tableName: string): Observable<ProcessTable[]> {
    return of(MockProcesses[tableName]);
  }

  public getProcessesData(){
    const client = new ProcessExplorerMessageHandlerClient('http://localhost:5056');
    const response = client.subscribe(new Empty) 
    return response;
  }
}
