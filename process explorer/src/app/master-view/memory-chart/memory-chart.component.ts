import { Component } from '@angular/core';

import * as Highcharts from 'highcharts';

@Component({
  selector: 'app-memory-chart',
  templateUrl: './memory-chart.component.html',
  styleUrls: ['./memory-chart.component.scss']
})
export class MemoryChartComponent {

  Highcharts: typeof Highcharts = Highcharts;
  chartOptions: Highcharts.Options = {
    series: [{
      data: [1, 2, 3],
      type: 'line'
    }]
  };
}
