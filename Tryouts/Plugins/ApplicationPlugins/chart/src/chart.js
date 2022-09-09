import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';

import {ComposeMessagingClient} from '../../../../Core/Services/Messaging/messaging-web-client/output/index.js';

let chart;

window.addEventListener('load', function () {
  chart = new Chart({
    chart: {
      renderTo: 'container',
      defaultSeriesType: 'column',
      events: {
        load: requestData
      }
    },
    title: {
      text: 'Monthly Sales Data'
    },
    xAxis: {
      categories: [
        'Jan',
        'Feb',
        'March',
        'Apr',
        'May',
        'Jun',
        'Jul',
        'Aug',
        'Sep',
        'Oct',
        'Nov',
        'Dec'
      ],
    },
    yAxis: {
      minPadding: 0.2,
      maxPadding: 0.2,
      title: {
        text: 'Value',
        margin: 80
      }
    },
    series: [{
      name: 'Buy'
    }, {
      name: 'Sell'
    }
    ]
  });
});

async function requestData() {
  let client;
  (async () => {
    client = new ComposeMessagingClient("ws://localhost:5000/ws");

    window.client = client;

    await client.connect();

    client.subscribe('proto_select_monthlySales', (message) => {
      let symbol = message.symbol;
      let buyData = message.buy;
      let sellData = message.sell;

      chart.setTitle({ text: "Monthly sales for " + symbol });
      chart.series[0].setData([]);
      chart.series[1].setData([]);

      buyData.forEach(function (p) {
        chart.series[0].addPoint(p, false);
      });

      sellData.forEach(function (p) {
        chart.series[1].addPoint(p, false);
      });

      chart.redraw();
    });
  })();
}