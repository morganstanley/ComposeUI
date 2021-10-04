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
using System.Windows;

namespace DockManagerCore
{
    public static class ResourceKeys
    {
        public static readonly ComponentResourceKey CaptionBarUnselectedBrushKey =
            new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarUnselectedBrush");
        public static readonly ComponentResourceKey CaptionBarActiveBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarActiveBrush");
        public static readonly ComponentResourceKey CaptionBarGroupedBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarGroupedBrush");

        public static readonly ComponentResourceKey ScreenDockignGridBorderBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "ScreenDockingGridBorderBrush");
         
        public static readonly ComponentResourceKey ScreenDockingGridBackgroundBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "ScreenDockingGridBackgroundBrush");

        public static readonly ComponentResourceKey DockignGridBorderBrushKey =
 new ComponentResourceKey(typeof(ResourceKeys), "DockingGridBorderBrush");

        public static readonly ComponentResourceKey DockingGridBackgroundBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "DockingGridBackgroundBrush");

        public static readonly CornerRadius FloatingWindowBorderCornerRadius = new CornerRadius(5);


        public static readonly double PaneHeaderHeight = 20;
        public static readonly double CaptionBarHeight = PaneHeaderHeight + 5;

    }
}
