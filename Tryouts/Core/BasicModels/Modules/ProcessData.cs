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

using MorganStanley.ComposeUI.Tryouts.Core.Utilities;
using System;
using System.Xml.Serialization;

namespace MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules
{
    public class ProcessData : ViewModelBase
    {
        [XmlAttribute]
        public Guid InstanceId { get; set; }

        [XmlAttribute]
        public string? ProcessName { get; set; }

        [XmlAttribute]
        public int WindowNumber { get; set; }

        long? _mainWindowHandle;
        [XmlIgnore]
        public long? ProcessMainWindowHandle 
        {
            get => _mainWindowHandle; 
            set
            {
                if (_mainWindowHandle == value)
                {
                    return;
                }

                _mainWindowHandle = value;

                OnPropertyChanged(nameof(ProcessMainWindowHandle));
            }
        }
    }
}
