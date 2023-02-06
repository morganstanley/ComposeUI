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

using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using LocalCollector.Connections;
using ModuleProcessMonitor.Processes;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Server.Server.Infrastructure.Protos;

namespace ProcessExplorer.Server.Server.Helper;

internal static class ProtoConvertHelper
{
    public static Process DeriveProtoProcessType(this ProcessInfoData process)
    {
        IEnumerable<ProcessThreadInfo> threads = new List<ProcessThreadInfo>();

        if (process.Threads == null || process.Threads.Equals(default))
            threads = Enumerable.Empty<ProcessThreadInfo>();
        else
            foreach (var thread in process.Threads)
            {
                if (thread.Id == 0) continue;
                if(thread.ThreadState == System.Diagnostics.ThreadState.Terminated) continue;

                try
                {
                    //TODO(Lilla): check this
                    threads.Append(new ProcessThreadInfo()
                    {
                        Id = thread.Id,
                        StartTime = thread.StartTime.ToString() ?? string.Empty,
                        PriorityLevel = thread.CurrentPriority,
                        Status = thread.ThreadState.ToStringCached() ?? string.Empty,
                        WaitReason = thread.ThreadState == System.Diagnostics.ThreadState.Wait ? thread.WaitReason.ToStringCached() : string.Empty,
                        ProcessorUsageTime = thread.TotalProcessorTime != null ?
                                            Duration.FromTimeSpan((TimeSpan)thread.TotalProcessorTime)
                                            : Duration.FromTimeSpan(TimeSpan.Zero),
                    });
                }
                catch (Exception)
                {
                    continue;
                }
            }

        return new()
        {
            InstanceId = string.Empty,
            UiType = string.Empty,
            UiHint = string.Empty,
            StartTime = process.StartTime ?? string.Empty,
            ProcessorUsageTime = process.ProcessorUsageTime != null ?
                                        Duration.FromTimeSpan((TimeSpan)process.ProcessorUsageTime)
                                        : Duration.FromTimeSpan(TimeSpan.Zero),
            PhysicalMemoryUsageBit = process.PhysicalMemoryUsageBit ?? 0,
            ProcessName = process.ProcessName ?? string.Empty,
            Pid = process.PID,
            ProcessPriorityClass = process.ProcessPriorityClass ?? string.Empty,
            Threads = { threads },
            VirtualMemorySize = process.VirtualMemorySize ?? 0,
            ParentId = process.ParentId ?? 0,
            PrivateMemoryUsage = process.PrivateMemoryUsage ?? 0,
            ProcessStatus = process.ProcessStatus ?? string.Empty,
            MemoryUsage = process.MemoryUsage ?? 0,
            ProcessorUsage = process.ProcessorUsage ?? 0,
        };
    }

    public static Connection DeriveProtoConnectionType(this ConnectionInfo connection)
    {
        return new()
        {
            Id = connection.Id.ToString(),
            Name = connection.Name,
            LocalEndpoint = connection.LocalEndpoint,
            RemoteEndpoint = connection.RemoteEndpoint,
            RemoteApplication = connection.RemoteApplication,
            ConnectionInformation = { connection.ConnectionInformation?.DeriveProtoDictionaryType() },
            Status = connection.Status
        };
    }

    public static ProcessInfoCollectorData DeriveProtoRuntimeInfoType(this LocalCollector.ProcessInfoCollectorData runtimeInfo)
    {
        return new()
        {
            Id = runtimeInfo.Id,
            Registrations = { runtimeInfo.Registrations.Select(reg => reg.DeriveProtoRegistrationType()) },
            EnvironmentVariables = { runtimeInfo.EnvironmentVariables.DeriveProtoDictionaryType() },
            Connections = { runtimeInfo.Connections.Select(conn => conn.DeriveProtoConnectionType()) },
            Modules = { runtimeInfo.Modules.Select(mod => mod.DeriveProtoModuleType()) }
        };
    }

    public static Registration DeriveProtoRegistrationType(this LocalCollector.Registrations.RegistrationInfo registration)
    {
        return new()
        {
            ServiceType = registration.ServiceType,
            ImplementationType = registration.ImplementationType,
            LifeTime = registration.LifeTime
        };
    }

    public static Module DeriveProtoModuleType(this LocalCollector.Modules.ModuleInfo module)
    {
        return new()
        {
            Name = module.Name,
            Location = module.Location,
            Version = module.Version.ToString(),
            VersionRedirectedFrom = module.VersionRedirectedFrom,
            PublicKeyToken = ByteString.CopyFrom(module.PublicKeyToken)
        };
    }

    public static Subsystem DeriveProtoSubsystemType(this SubsystemInfo subsystem)
    {
        return new()
        {
            Name = subsystem.Name,
            Path = subsystem.Path,
            Port = subsystem.Port ?? 0,
            UiType = subsystem.UIType,
            Url = subsystem.Url,
            StartupType = subsystem.StartupType,
            State = subsystem.State,
            AutomatedStart = subsystem.AutomatedStart,
            Arguments = { subsystem.Arguments?.ToList() },
            Description = subsystem.Description
        };
    }

    public static MapField<T, R> DeriveProtoDictionaryType<T, R>(
        this IEnumerable<KeyValuePair<T, R>> dict)
    {
        var map = new MapField<T, R>();

        if (dict != null && dict.Any())
            foreach (var kvp in dict)
                map.Add(kvp.Key, kvp.Value);

        return map;
    }

    public static MapField<T, TResult> DeriveProtoDictionaryType<T, R, TResult>(
        this IEnumerable<KeyValuePair<T, R>> dict,
        Func<R, TResult> converter)
    {
        var map = new MapField<T, TResult>();

        if (dict != null && dict.Any())
            foreach (var kvp in dict)
                map.Add(kvp.Key, converter.Invoke(kvp.Value));

        return map;
    }
}
