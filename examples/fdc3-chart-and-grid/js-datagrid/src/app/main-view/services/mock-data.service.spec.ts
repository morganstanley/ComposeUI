/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { DesktopAgent } from '@finos/fdc3';
import { MockDataService } from './mock-data.service';

const dummyDesktopAgent = jasmine.createSpyObj<DesktopAgent>(
  'DesktopAgent',
  [
    'joinChannel',
    'getSystemChannels',
    'getAppMetadata',
    'getInfo',
    'leaveCurrentChannel',
    'getCurrentChannel',
    'createPrivateChannel',
    'getOrCreateChannel',
    'joinUserChannel',
    'getUserChannels',
    'addContextListener',
    'addIntentListener',
    'raiseIntentForContext',
    'raiseIntent',
    'broadcast',
    'findInstances',
    'findIntentsByContext',
    'findIntent',
    'open',
  ]
)

describe('MockDataService', () => {

  beforeEach(() => {
    window.fdc3 = dummyDesktopAgent;
  });

  it('should be created', () => {
    const mockDataService = new MockDataService();
    expect(mockDataService).toBeTruthy();
  });
});
