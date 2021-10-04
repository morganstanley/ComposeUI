/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DockManagerCore.Desktop
{
    public class WindowViewModel:ViewModelBase
    {
        public WindowViewModel(InitialWindowParameters initialParameters_)
        {
            ID = "view" + Guid.NewGuid().ToString("N");
            HeaderItems = new HeaderItemsCollection();
            InitialParameters = initialParameters_;
        }
        public string ID { get; private set; }

        public object Content { get; set; }

        public Icon Icon { get; set; }

        public string Title { get; set; }

        public IList<object> HeaderItems { get; private set; }

        public InitialWindowParameters InitialParameters { get; private set; }

        public bool IsActive { get; set; }
    }
}
