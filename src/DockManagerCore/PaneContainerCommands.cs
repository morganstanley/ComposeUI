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
using System.Collections.Generic;
using System.Windows.Input;

namespace DockManagerCore
{
    public static class PaneContainerCommands
    {
        static readonly List<RoutedCommand> commands = new List<RoutedCommand>(); 
        static PaneContainerCommands()
        { 
            commands.Add(HideHeader = new RoutedUICommand("Hide header", "HideHeader", typeof(PaneContainer))); 
            commands.Add(ShowHeader = new RoutedUICommand("Show header", "ShowHeader", typeof(PaneContainer))); 
            commands.Add(Minimize = new RoutedUICommand("Minimize", "Minimize", typeof(PaneContainer))); 
            commands.Add(Restore = new RoutedUICommand("Restore", "Restore", typeof(PaneContainer))); 
            commands.Add(Lock = new RoutedUICommand("Lock", "Lock", typeof(PaneContainer))); 
            commands.Add(Unlock = new RoutedUICommand("Unlock", "Unlock", typeof(PaneContainer)));
            commands.Add(Close = new RoutedUICommand("Close", "Close", typeof(PaneContainer)));
            commands.Add(Maximize = new RoutedUICommand("Maximize", "Maximize", typeof(PaneContainer)));
            commands.Add(TearOff = new RoutedUICommand("Tear Off", "TearOff", typeof(PaneContainer)));
        }

        public static readonly RoutedCommand HideHeader;
        public static readonly RoutedCommand ShowHeader;
        public static readonly RoutedCommand Minimize;
        public static readonly RoutedCommand Maximize;
        public static readonly RoutedCommand Restore;
        public static readonly RoutedCommand Lock;
        public static readonly RoutedCommand Unlock;
        public static readonly RoutedCommand Close;
        public static readonly RoutedCommand TearOff;

        public static IList<RoutedCommand> GetAllCommands()
        {
            return commands;
        }
    }

    public static class ContentPaneCommands
    {

        static readonly List<RoutedCommand> commands = new List<RoutedCommand>();
        static ContentPaneCommands()
        { 
            commands.Add(Close = new RoutedUICommand("Close", "Close", typeof(ContentPane)));
            commands.Add(Rename = new RoutedUICommand("Rename", "Rename", typeof(ContentPane)));
            commands.Add(Activate = new RoutedUICommand("Activate", "Activate", typeof(ContentPane)));
            commands.Add(TearOff = new RoutedUICommand("TearOff", "TearOff", typeof(ContentPane)));
        }

        public static readonly RoutedCommand Rename; 
        public static readonly RoutedCommand Close;
        public static readonly RoutedCommand Activate;
        public static readonly RoutedCommand TearOff;

        public static IList<RoutedCommand> GetAllCommands()
        {
            return commands;
        }
    }
}
