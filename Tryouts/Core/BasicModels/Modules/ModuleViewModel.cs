/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;

namespace MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules
{
    public class ModuleViewModel
    {
        public ModuleManifest Manifest { get; init;  }

        public ModuleViewModel(ModuleManifest manifest)
        {
            Manifest = manifest;
        }

        public ModuleViewModel
        (
            string name, 
            StartupType startupType, 
            string uiType, 
            string pathOrUrl) 
            : 
            this(new ModuleManifest { Name = name, StartupType = startupType, UIType = uiType })
        {
            if (uiType == UIType.Window)
            {
                Manifest.Path = pathOrUrl;
            }    
            else
            {
                Manifest.Url = pathOrUrl;
            }
        }

        public event Action<ModuleManifest>? LaunchEvent;

        public void Launch()
        { 
            LaunchEvent?.Invoke(Manifest);
        }
    }
}
