// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->
import { Guid } from "igniteui-angular-core";
import { Subject } from "rxjs";
import { SubsystemInfo } from "src/app/DTOs/SubsystemInfo";

export class SubsystemServiceObject{

    private subsystemsFromDotnet: Map<string, SubsystemInfo> = new Map<string, SubsystemInfo>();
    private subsystemInfos: Map<string, SubsystemInfo> = new Map<string, SubsystemInfo>();
    public subjectAddSubsystems: Subject<Map<string, SubsystemInfo>> = new Subject<Map<string, SubsystemInfo>>();
    public subjectAddSubsystem: Subject<SubsystemInfo> = new Subject<SubsystemInfo>();
    public subjectUpdateSubsystemInfo: Subject<SubsystemInfo> = new Subject<SubsystemInfo>();
    public subjectRemoveSubsystem: Subject<string> = new Subject<string>();

    constructor() {
    }

    public AddSubsystemsAsync(subsystems: Map<string, SubsystemInfo>){
        if(this.subsystemsFromDotnet.size == 0 || this.subsystemsFromDotnet.keys.length == 0){
            this.subsystemsFromDotnet = subsystems;
            this.createNormalMap();
        }else{
            this.updateElementsInMap(subsystems);
        }
        this.subjectAddSubsystems.next(this.subsystemInfos);
    }

    public AddSubsystemAsync(subsystemId: string, subsystem: SubsystemInfo ){
        console.log(subsystem);
        this.UpdateSubsystemInfoAsync(subsystemId, subsystem);
        this.subjectAddSubsystem.next(subsystem);
    }

    public RemoveSubsystemAsync(subsystemId: string){
        console.log("subsystem removed: ", subsystemId)
        var subsystem = this.getSubsystemOfSubsystems(subsystemId);
        if(subsystem != undefined){
            this.subsystemInfos.delete(subsystemId)
            this.subjectRemoveSubsystem.next(subsystemId);
        }
    }

    public UpdateSubsystemInfoAsync(subsystemId: string, subsystem: SubsystemInfo){
        this.subsystemInfos.set(subsystemId, subsystem);
        this.subjectUpdateSubsystemInfo.next(subsystem);
        this.subjectAddSubsystems.next(this.subsystemInfos);
    }

    private getSubsystemOfSubsystems(key: string) : SubsystemInfo | undefined {
        return this.subsystemInfos.get(key);
    }

    private createNormalMap(){
        var map = new Map(Object.entries(this.subsystemsFromDotnet))
        map.forEach((value: SubsystemInfo, key:string) =>{
            this.subsystemInfos.set(key, value);
        })
    }

    private updateElementsInMap(subsystems: Map<any, SubsystemInfo>){
        new Map(Object.entries(subsystems)).forEach((value: SubsystemInfo, key: string) => {
            this.UpdateSubsystemInfoAsync(key, value);
        });
    }

    public getSubsystemInfos(): SubsystemInfo[] {
        return Array.from(this.subsystemInfos.values());
    }
}

