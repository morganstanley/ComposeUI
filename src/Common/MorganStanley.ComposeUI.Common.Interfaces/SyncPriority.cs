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


namespace MorganStanley.ComposeUI.Common.Interfaces
{
    ///
    /// Summary:
    ///     Defines the priorities with which jobs can be invoked on a Avalonia.Threading.Dispatcher.
    public enum SyncPriority
    {
        ///
        /// Summary:
        ///     Minimum possible priority
        MinValue = 1,
        ///
        /// Summary:
        ///     The job will be processed when the system is idle.
        SystemIdle = 1,
        ///
        /// Summary:
        ///     The job will be processed when the application is idle.
        ApplicationIdle = 2,
        ///
        /// Summary:
        ///     The job will be processed after background operations have completed.
        ContextIdle = 3,
        ///
        /// Summary:
        ///     The job will be processed after other non-idle operations have completed.
        Background = 4,
        ///
        /// Summary:
        ///     The job will be processed with the same priority as input.
        Input = 5,
        ///
        /// Summary:
        ///     The job will be processed after layout and render but before input.
        Loaded = 6,
        ///
        /// Summary:
        ///     The job will be processed with the same priority as render.
        Render = 7,
        ///
        /// Summary:
        ///     The job will be processed with the same priority as render.
        Layout = 8,
        ///
        /// Summary:
        ///     The job will be processed with the same priority as data binding.
        DataBind = 9,
        ///
        /// Summary:
        ///     The job will be processed with normal priority.
        Normal = 10,
        ///
        /// Summary:
        ///     The job will be processed before other asynchronous operations.
        Send = 11,
        ///
        /// Summary:
        ///     Maximum possible priority
        MaxValue = 11
    }
}
