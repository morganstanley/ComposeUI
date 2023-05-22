import { Injectable } from '@angular/core';
import { SuperRPC } from 'super-rpc';
import { ISubsystemController } from '../DTOs/ISubsystemController';
import { SubsystemInfo } from '../DTOs/SubsystemInfo';
import { SubsystemServiceObject } from '../main-view/subsystems/ServiceSubsystemObject';

@Injectable({
  providedIn: 'root'
})
export class MockSubsystemsService {
  private ws: WebSocket = new WebSocket('ws://localhost:5056/subsystems');
  private rpc : SuperRPC;
  private connected: any;
  private subsystemServiceObject: SubsystemServiceObject
  private subsystemController: ISubsystemController;

  constructor() {
    this.subsystemServiceObject = new SubsystemServiceObject();
    this.connected = new Promise( (resolve, reject) => 
    {
      try{
        this.ws.addEventListener('open', async() => {
          this.rpc = new SuperRPC( () => (Math.random()*1e17).toString(36));
          this.rpc.connect({
            sendAsync: (message) => this.ws.send(JSON.stringify(message)),
            receive: (callback) => { this.ws.addEventListener('message', (msg) => callback(JSON.parse(msg.data)))}
          });

        this.rpc.registerHostObject('SubsystemServiceObject', this.subsystemServiceObject, {
          functions:['AddSubsystemsAsync', 'AddSubsystemAsync', 'RemoveSubsystemAsync', 'UpdateSubsystemInfoAsync']});

        await this.rpc.requestRemoteDescriptors();
        this.subsystemController = this.rpc.getProxyObject('subsystemController');
        resolve(undefined);
      })}catch(ex){
        reject(ex);}
    });
   }
  
  public getSubsystemServiceObject() : SubsystemServiceObject{
    return this.subsystemServiceObject;
  }

  public getSubsystemObjectsSubsystemInfos(): SubsystemInfo[]{
    return this.subsystemServiceObject.getSubsystemInfos();
  }

  public getSubsystemController() : ISubsystemController{
    return this.subsystemController;
  }

  public LaunchSubsystem(id: string): void{
    this.subsystemController.LaunchSubsystem(id);
  }

  public LaunchSubsystems(ids: string[]): void{
    this.subsystemController.LaunchSubsystems(ids);
  }

  public RestartSubsystem(id: string): void{
    this.subsystemController.RestartSubsystem(id);
  }

  public RestartSubsystems(ids: string[]): void{
    this.subsystemController.RestartSubsystems(ids);
  }

  public ShutdownSubsystem(id: string){
    this.subsystemController.ShutdownSubsystem(id);
  }

  public ShutdownSubsystems(ids: string[]){
    this.subsystemController.ShutdownSubsystems(ids);
  }
}
