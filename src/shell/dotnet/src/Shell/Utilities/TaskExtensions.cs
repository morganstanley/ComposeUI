﻿// Morgan Stanley makes this available to you under the Apache License,
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

using System.Threading.Tasks;
using System.Windows.Threading;

namespace MorganStanley.ComposeUI.Shell.Utilities;

public static class TaskExtensions
{
    /// <summary>
    /// Synchronously waits for the task to complete, while keeping the UI thread responsive.
    /// </summary>
    /// <param name="task"></param>
    public static void WaitOnDispatcher(this Task task)
    {
        if (task.IsCompleted)
        {
            task.Wait();
            
            return;
        }

        var frame = new DispatcherFrame(exitWhenRequested: true);

        _ = task.ContinueWith(_ => frame.Continue = false);
        
        Dispatcher.PushFrame(frame);

        task.Wait(); // Propagate exceptions
    }
}