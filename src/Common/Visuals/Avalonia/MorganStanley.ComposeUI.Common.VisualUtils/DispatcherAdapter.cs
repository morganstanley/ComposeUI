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

using Avalonia.Threading;
using MorganStanley.ComposeUI.Common.Interfaces;
using NP.Utilities.Attributes;


namespace MorganStanley.ComposeUI.Common.VisualUtils
{
    /// <summary>
    /// this class implements Avalonia UI thread marshalling functionality. 
    /// </summary>
    [Implements(typeof(ISyncContext), isSingleton:true)]
    public class DispatcherAdapter : ISyncContext
    {
        /// <summary>
        /// Returns true, if the current thread the same as the Avalonia UI thread (no marshaling is needed),
        /// false otherwise.
        /// </summary>
        public bool CheckAccess()
        {
            return Dispatcher.UIThread.CheckAccess();
        }

        /// <summary>
        /// performs the action within the Avalonia UI thread
        /// </summary>
        /// <param name="action">Action to perform</param>
        /// <param name="priority">Thread priority</param>
        public void Post(Action action, SyncPriority priority = SyncPriority.Normal)
        {
            if (!CheckAccess())
            {
                Dispatcher.UIThread.Post(action, (DispatcherPriority) priority);
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}
