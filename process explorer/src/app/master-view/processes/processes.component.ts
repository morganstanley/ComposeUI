import { Component, OnInit } from '@angular/core';
import { MockProcessesService } from '../../services/mock-processes.service';
import * as Highcharts from 'highcharts';

@Component({
  selector: 'app-processes',
  templateUrl: './processes.component.html',
  styleUrls: ['./processes.component.scss']
})
export class ProcessesComponent implements OnInit {
  public mockProcessesData: any = null;

  Highcharts: typeof Highcharts = Highcharts;
  chartOptions: Highcharts.Options = {
    series: [{
      data: [1, 2, 3],
      type: 'line'
    }]
  };

  constructor(private mockProcessesService: MockProcessesService) {}

  ngOnInit() {
    // depending on implementation, data subscriptions might need to be unsubbed later
    this.mockProcessesService.getData('Processes').subscribe(data => this.mockProcessesData = data);
  }
}
