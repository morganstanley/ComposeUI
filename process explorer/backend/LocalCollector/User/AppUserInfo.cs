/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities.User;

namespace ProcessExplorer.Entities
{
    public class AppUserInfo
    {
        #region Constructors
        AppUserInfo()
        {
            Data = new AppUserInfoDto();
        }
        public AppUserInfo(string userName, bool admin = false)
            :this(admin)
        {
            Data.UserName = userName;
        }

        public AppUserInfo(bool admin = false)
            :this()
        {
            Data.UserName = Environment.UserName;
            Data.MachineInfo = new MachineInfo(true).Data;
            Data.IsAdmin = admin;
        }
        public AppUserInfo(string userName, MachineInfo machine, bool admin = false)
            :this(userName, admin)
        {
            Data.MachineInfo = machine.Data;
        }
        public AppUserInfo(MachineInfo machine)
            :this(false)
        {
            Data.MachineInfo = machine.Data;
        }
        public AppUserInfo(string userName, MachineInfo machine)
            : this(userName, machine, false)
        {

        }
        #endregion

        public AppUserInfoDto Data { get; set; }
    }

    public class AppUserInfoDto
    {
        #region Properties
        public string? UserName { get; set; }
        public bool? IsAdmin { get; set; }
        public MachineDto? MachineInfo { get; set; } = default;
        #endregion
    }
}
