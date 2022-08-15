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

namespace ModuleLoaderPrototype;

public struct LifecycleEvent
{
    public string name;
    public int pid;
    public string eventType;
    public bool expected;

    public static LifecycleEvent Stopped(string name, int pid, bool expected = true) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.Stopped, pid = pid, expected = expected };
    public static LifecycleEvent Started(string name, int pid) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.Started, pid = pid, expected = true };
    public static LifecycleEvent StoppingCanceled(string name, int pid, bool expected) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.StoppingCanceled, pid = pid, expected = expected };
    public static LifecycleEvent FailedToStart(string name) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.FailedToStart, expected = false };
}

public class LifecycleEventType
{
    private LifecycleEventType() { }

    public const string Stopped = "stopped";
    public const string Started = "started";
    public const string StoppingCanceled = "stoppingCanceled";
    public const string FailedToStart = "failedToStart";
}