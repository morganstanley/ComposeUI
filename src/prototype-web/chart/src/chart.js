import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';

import {ComposeMessagingClient} from '../../../messaging-web-client/output/index.js';
import Mocks from './mockData.js';

//-----------------------------
//register Market data
//selec market data

//random time series data for the month
// table only sents the symbol
//------------------------------------------




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
      text: 'Live random data'
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
  let mockData = new Mocks();
  let client;
  (async () => {
    client = new ComposeMessagingClient("ws://localhost:5000/ws");
  
    window.client = client;
  
    await client.connect();
    /* message symbol*/
    let symbol;
    client.subscribe('proto_select_marketData', (message) => {
      symbol = message.symbol;
      
      console.log("symbol=", symbol);

      chart.setTitle({text: "Monthly sales for " + symbol});
    
      chart.series[0].setData([]);
      chart.series[1].setData([]);

      //let buyData = [49.9, 71.5, 106.4, 129.2, 144.0, 63.45, 89.13, 15.26, 203.2, 58.7, 115.4, 32.8];
      let buyData = mockData.getBuyDataBySymbol(symbol);

      buyData.forEach(function(p) {
          chart.series[0].addPoint(p, false);
      });

      //let sellData =  [83.6, 78.8, 98.5, 93.4, 106.0, 82.3, 45.6, 305.6, 263.5, 33.5, 112.6, 87.3];
      let sellData =  mockData.getSellDataBySymbol(symbol);

      sellData.forEach(function(p) {
          chart.series[1].addPoint(p, false);
      });

      chart.redraw(); 

    });
  })();
}