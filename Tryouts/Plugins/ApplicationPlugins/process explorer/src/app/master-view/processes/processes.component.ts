/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Component, NgZone, OnInit, ViewChild } from '@angular/core';
import { MockProcessesService } from '../../services/mock-processes.service';
import * as Highcharts from 'highcharts';
import {  throttleTime } from 'rxjs/operators';
import { ProcessInfo } from 'src/app/DTOs/ProcessInfo';

@Component({
  selector: 'app-processes',
  templateUrl: './processes.component.html',
  styleUrls: ['./processes.component.scss']
})
export class ProcessesComponent implements OnInit {
  @ViewChild("treeGrid") TreeGrid : any;

  public mockProcessesData: Array<ProcessInfo>;
  public processes: any;

  Highcharts: typeof Highcharts = Highcharts;
  chartOptions: Highcharts.Options = {
    series: [{
      data: [1, 2, 3],
      type: 'line'
    }]
  };

  constructor(private ngZone: NgZone, private mockProcessesService: MockProcessesService) { 
  }

  ngOnInit() {
    this.mockProcessesData = this.mockProcessesService.getProcessServiceObjectsProcesses();
    //depending on implementation, data subscriptions might need to be unsubbed later
      const throttlingProcesses = this.mockProcessesService.getProcessServiceObject()
                                      .subjectAddProcesses.pipe(throttleTime(1000));
      const subscribingToProcesses = throttlingProcesses.subscribe((data) => {
        this.mockProcessesData = data;
        this.TreeGrid.markForCheck();
      });
  }

  KillProcessById(pid: number){
    this.mockProcessesService.KillProcessByID(pid);
  }
}


