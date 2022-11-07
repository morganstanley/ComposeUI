// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using ModuleProcessMonitor.Processes;

namespace ModuleProcessMonitor;

internal class ProcessInfoDataComparer : EqualityComparer<ProcessInfoData>
{
    public ProcessInfoDataComparer()
    {
    }

    public override bool Equals(ProcessInfoData? thisData, ProcessInfoData? otherData)
    {
        if(thisData == null && otherData == null) return true;
        if (thisData == null || otherData == null) return false;

        return (thisData.PID == otherData.PID);
    }

    public override int GetHashCode(ProcessInfoData data)
    {
        int hCode = (int)data.PID! ^ (int)data.ParentId!;
        return hCode.GetHashCode();
    }
}

