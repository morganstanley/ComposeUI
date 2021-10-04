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
using System.Windows;

namespace DockManagerCore.Desktop
{
    [Serializable]
    public class InitialWindowParameters
    { 

        /// <summary>
        /// Gets or sets if the window can be resized by the user or code.
        /// </summary>
        public SizingMethod SizingMethod { get; set; }

        /// <summary>
        /// Gets or sets the width of the window when it is first shown.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the window when it is first shown.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets if the window is allowed to be larger than a single display.
        /// </summary>
        public bool EnforceSizeRestrictions { get; set; }

        /// <summary>
        /// Gets or sets if the window is always on top. would only take effect when UseDockManager is false
        /// </summary>
        public bool Topmost { get; set; }

        /// <summary>
        /// Gets or sets if the window is shown on the taskbar.
        /// </summary>
        public bool ShowInTaskbar { get; set; }

        /// <summary>
        /// Gets or sets if the window can be moved partially offscreen.
        /// </summary>
        public bool AllowPartiallyOffscreen { get; set; }

        /// <summary>
        /// Gets or sets whether the window's header is initially visible.
        /// </summary>
        public bool IsHeaderVisible { get; set; }

        public bool Transient { get; set; }
        /// <summary>
        /// Gets or sets the resources applied to the window.
        /// </summary>
        public ResourceDictionary Resources { get; set; }
 

        public InitialWindowParameters()
        { 
            SizingMethod = SizingMethod.SizeToContent;
            Width = double.NaN;
            Height = double.NaN;
            EnforceSizeRestrictions = true;
            Transient = false;
            Topmost = false;
            ShowInTaskbar = true;
            AllowPartiallyOffscreen = true;
            IsHeaderVisible = true; 
        }
    }
}