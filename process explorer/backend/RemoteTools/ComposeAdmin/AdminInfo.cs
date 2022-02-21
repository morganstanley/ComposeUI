/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
using ProcessExplorer.Entities.ConfigurationFiles;
using ProcessExplorer.Entities.Subsystems;

namespace ProcessExplorer.Entities.AdminCommands
{
    public class AdminInfo : ISubsystemHandler, IConfigurationHandler
    {
        public AdminInfo(AppUserInfo user) => Admin = user;
        public AppUserInfo Admin { get; set; }

        public void ManageSubsystem(SubsystemInfo subsystemInfo, CommandType commandType)
        {
            throw new NotImplementedException();
        }
    }
}
