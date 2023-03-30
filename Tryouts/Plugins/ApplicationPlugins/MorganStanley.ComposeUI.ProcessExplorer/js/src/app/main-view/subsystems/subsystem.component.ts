import { Component, OnInit } from '@angular/core';
import {  throttleTime } from 'rxjs/operators';
import { SubsystemInfo } from 'src/app/DTOs/SubsystemInfo';
import { MockSubsystemsService } from 'src/app/services/mock-subsystems.service';

@Component({
  selector: 'app-subsystem',
  templateUrl: './subsystem.component.html',
  styleUrls: ['./subsystem.component.scss']
})
export class SubsystemComponent implements OnInit {

  public mockSubsystemsData2: SubsystemInfo[] = new Array<SubsystemInfo>();
  public mockSubsystemsData: Map<string, SubsystemInfo> = new Map<string, SubsystemInfo>();

  constructor(private mockSubsystemsService: MockSubsystemsService) { }

  ngOnInit(): void {
    this.mockSubsystemsData2 = this.mockSubsystemsService.getSubsystemObjectsSubsystemInfos();

    const throttlingAddSubsystems = this.mockSubsystemsService.getSubsystemServiceObject()
                                      .subjectAddSubsystems.pipe(throttleTime(1000));

    const throttlingModifyState = this.mockSubsystemsService.getSubsystemServiceObject()
                                      .subjectUpdateSubsystemInfo.pipe(throttleTime(100));

    const subscribingToSubsystems = throttlingAddSubsystems.subscribe(data => {
        this.mockSubsystemsData = data;
        this.mockSubsystemsData2 = Array.from(this.mockSubsystemsData.values());
      });
    
    const subscribingToModifiedSubsystems = throttlingModifyState.subscribe(data => {
      this.ModifySubsystemState(data);
    })
  }

  private ModifySubsystemState(data: SubsystemInfo) {
    for(let subsystem of this.mockSubsystemsData2){
      if(data.Path === subsystem.Path){
        subsystem.State = data.State;
        break;
      }
    }
  }

  public LaunchSubsystem(subsystem: SubsystemInfo){
    var id = this.getRelevantKeyToModify(subsystem);
    if(id != ""){
      this.mockSubsystemsService.LaunchSubsystem(id);
    }
  }

  public LaunchSubsystems(subsystems: SubsystemInfo[]): void{
    var ids = this.getRelevantSubsystemsToModify(subsystems);
    this.mockSubsystemsService.LaunchSubsystems(ids);
  }

  public RestartSubsystem(subsystem: SubsystemInfo): void{
    var id = this.getRelevantKeyToModify(subsystem);
    if(id != ""){
      this.mockSubsystemsService.RestartSubsystem(id);
    }
  }

  public RestartSubsystems(subsystems: SubsystemInfo[]): void{
    var ids = this.getRelevantSubsystemsToModify(subsystems);
    this.mockSubsystemsService.RestartSubsystems(ids);
  }

  public ShutdownSubsystem(subsystem: SubsystemInfo){
    var id = this.getRelevantKeyToModify(subsystem);
    if(id != ""){
      this.mockSubsystemsService.ShutdownSubsystem(id);
    }
  }

  public ShutdownSubsystems(subsystems: SubsystemInfo[]){
    var ids = this.getRelevantSubsystemsToModify(subsystems);
    this.mockSubsystemsService.ShutdownSubsystems(ids);
  }

  private getRelevantSubsystemsToModify(subsystems: SubsystemInfo[]): string[] {
    var ids = new Array<string>()
    subsystems.forEach((subsystem: SubsystemInfo) => {
      if(subsystem != undefined){
        for(let [key, value] of this.mockSubsystemsData){
          const sub = value as SubsystemInfo;
          if(sub.Path === subsystem.Path){
            ids.push(key)
            break;
          }
        }
      }
    })
    return ids;
  }

  private getRelevantKeyToModify(subsystem: SubsystemInfo): string{
    var id = "";
    if(subsystem != undefined){
      for(let [key, value] of this.mockSubsystemsData){
        const sub = value as SubsystemInfo;
        if(sub.Path === subsystem.Path){
          id = key;
          break;
        }
      }
    }
    return id;
  }
}
