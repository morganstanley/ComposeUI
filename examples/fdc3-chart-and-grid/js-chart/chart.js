import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';
import * as Highcharts from 'highcharts';

let chart;
let intentListener;

window.addEventListener('load', async function() {
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

window.addEventListener('close', async function(){
  if (this.intentListener) {
    await this.intentListener.unsubscribe();
  }
});

async function requestData() {
  (async () => {
    intentListener = await window.fdc3.addIntentListener("ViewChart", (context, contextMetada) => {
      if (context.id) {
        chart.setTitle({ text: "Monthly sales for " + context.id.ticker });
        chart.series[0].setData([]);
        chart.series[1].setData([]);

        context.id.buyData.forEach(function (p) {
          chart.series[0].addPoint(p, false);
        });
      
        context.id.sellData.forEach(function (p) {
          chart.series[1].addPoint(p, false);
        });

        chart.redraw();
      }
    });
  })();
}