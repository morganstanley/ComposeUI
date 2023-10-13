/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
// tslint:disable
export const MockSubSystems:{ [key: string]: any[] } = {
    'Subsystems': [ 
      {
        "Name": "ChartWeb",
        "Id": "chart-web",
        "State": "Started",
        "ModuleType": "web"
      }, {
        "Name": "ChartWpf",
        "Id": "chart-wpf",
        "State": "Starting",
        "ModuleType": "native"
      }, {
        "Name": "Google Chrome",
        "Id": "chrome.exe",
        "State": "Started",
        "ModuleType": "native"
      },{
        "Name": "GridWeb",
        "Id": "grid-web",
        "State": "Stopping",
        "ModuleType": "web",
      },{
        "Name": "GridWpf",
        "Id": "grid-wpf",
        "State": "Stopped",
        "ModuleType": "native",
      },{
        "Name": "Symphony",
        "Id": "symphony.exe",
        "State": "Started",
        "ModuleType": "native"
      }
    ]
  };
  // tslint:disable
  
 