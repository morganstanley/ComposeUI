import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';
    
const chart = new Chart('container', {
    chart: {
      type: 'column'
    },
    title: {
      text: 'Stock Prices'
    },
    xAxis: {
      categories: [
        'IBM',
        'Apple',
        'Google',
        'Samsung',
        'Tesla'
      ],
      crosshair: true
    },
    yAxis: {
      min: 0,
      title: {
        text: 'Price'
      }
    },
    tooltip: {
      headerFormat: '<span style="font-size:10px">{point.key}</span><table>',
      pointFormat: '<tr><td style="color:{series.color};padding:0">{series.name}: </td>' +
        '<td style="padding:0"><b>{point.y:.1f} mm</b></td></tr>',
      footerFormat: '</table>',
      shared: true,
      useHTML: true
    },
    plotOptions: {
      column: {
        pointPadding: 0.2,
        borderWidth: 0
      }
    },
    series: [{
      name: 'Buy',
      data: [49.9, 71.5, 106.4, 129.2, 144.0]
  
    }, {
      name: 'Sell',
      data: [83.6, 78.8, 98.5, 93.4, 106.0]
    }
  ]
});
  
