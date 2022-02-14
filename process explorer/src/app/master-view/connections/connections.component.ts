import { Component, OnInit } from '@angular/core';
import { MockConnectionsService } from '../../services/mock-connections.service';
import * as Highcharts from 'highcharts';

@Component({
  selector: 'app-connections',
  templateUrl: './connections.component.html',
  styleUrls: ['./connections.component.scss']
})

export class ConnectionsComponent implements OnInit {
  public mockConnectionsData: any = null;
   Highcharts: typeof Highcharts = Highcharts;
  chartOptions: Highcharts.Options = {
    series: [{
      data: [
        ['Connection1', 1],['Connection2',2],['Local',3], ['Connection3',3], ['Etc',3]],
      type: 'pie'
    }]
  }; 

  constructor(private mockConnectionsService: MockConnectionsService) {
  }
   public chartSliceClickEvent(e: any): void {
      e.args.isExploded = !e.args.isExploded;
  } 

  ngOnInit() {
    // depending on implementation, data subscriptions might need to be unsubbed later
    this.mockConnectionsService.getData('Connections').subscribe(data => this.mockConnectionsData = data);
  }
}