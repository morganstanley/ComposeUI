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

using ProcessExplorer.Abstraction.Processes;

namespace ProcessExplorer.Abstraction.Handlers;

/// <summary>
/// Used for sending modified process update to the UI(s).
/// </summary>
/// <param name="pid"></param>
public delegate void ProcessModifiedHandler(int pid);

/// <summary>
/// Used for sending terminated process update to the UI(s).
/// </summary>
/// <param name="pid"></param>
public delegate void ProcessTerminatedHandler(int pid);

/// <summary>
/// Used for sending created process information to the UI(s).
/// </summary>
/// <param name="pid"></param>
public delegate void ProcessCreatedHandler(int pid);

/// <summary>
/// Used for sending status change update of a process to the UI(s).
/// </summary>
/// <param name="process"></param>
public delegate void ProcessStatusChangedHandler(KeyValuePair<int, Status> process);

/// <summary>
/// Used for sending updated processes to the UI(s).
/// </summary>
/// <param name="ids"></param>
public delegate void ProcessesModifiedHandler(ReadOnlySpan<int> ids);
