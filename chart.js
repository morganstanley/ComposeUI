﻿import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';
import * as Highcharts from 'highcharts';

let chart;


window.addEventListener('load', function () {
  chart = Highcharts.chart('container', {
    chart: {
      type: 'column',
      events: {
        load: requestData
      }
    },
    title: {
      text: 'Monthly Sales Data'
    },
    yAxis: {
      minPadding: 0.2,
      maxPadding: 0.2,
      title: {
        text: 'Value',
        margin: 80,
      }
    },
    xAxis: {
      categories: ['Jan','Feb','March','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'],
    },
    series: [{
      name: 'Buy',
      data: [] 
    }, {
      name: 'Sell',
      data: []
    }]
  });
});
