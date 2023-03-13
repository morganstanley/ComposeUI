import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";

let chart;
let client;

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

  window.document.getElementById("close-button").onclick =
    async ev => {
      if (!client) return;
      const tmpClient = client;
      client = undefined;
      tmpClient.close();
    };

});

async function requestData() {

  (async () => {
    client = createMessageRouter();

    window.client = client;

    await client.connect();

    client.subscribe('proto_select_monthlySales', (message) => {

      const payload = JSON.parse(message.payload);
      const symbol = payload.symbol;
      const buyData = payload.buy;
      const sellData = payload.sell;

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