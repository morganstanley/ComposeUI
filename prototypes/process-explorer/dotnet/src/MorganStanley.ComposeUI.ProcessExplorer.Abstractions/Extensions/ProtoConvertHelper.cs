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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using Process = ProcessExplorer.Abstractions.Infrastructure.Protos.Process;
using ProcessInfoCollectorData = ProcessExplorer.Abstractions.Infrastructure.Protos.ProcessInfoCollectorData;
using ModuleProto = ProcessExplorer.Abstractions.Infrastructure.Protos.Module;

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Extensions;

public static class ProtoConvertHelper
{
    //n Proto3, all fields are optional and have a default value. For example, a string field has a default value of empty string ("") and an int field has a default value of zero (0).
    //If you want to create a proto message without a certain field, you have to set its value to the default value.

    public static Process DeriveProtoProcessType(this ProcessInfoData process)
    {
        List<ProcessThreadInfo> threads = new();

        if (process.Threads != null && !process.Threads.Equals(default))
            foreach (var thread in process.Threads)
            {
                if (thread.Id == 0) continue;
                if (thread.ThreadState == System.Diagnostics.ThreadState.Terminated) continue;

                GetStartTime(thread, out string startTime);
                //we should add the try catch block because the thread might exit during the excution so the information won't be available
                try
                {
                    threads.Add(new ProcessThreadInfo()
                    {
                        Id = thread.Id,
                        StartTime = startTime,
                        PriorityLevel = thread.CurrentPriority,
                        Status = thread.ThreadState.ToStringCached() ?? string.Empty,
                        WaitReason = thread.ThreadState == System.Diagnostics.ThreadState.Wait ? thread.WaitReason.ToStringCached() : string.Empty,
                        ProcessorUsageTime = Duration.FromTimeSpan(thread.TotalProcessorTime)
                    });
                }
                catch (Exception)
                {
                    continue;
                }
            }

        return new()
        {
            StartTime = process.StartTime ?? string.Empty,
            ProcessorUsageTime = process.ProcessorUsageTime != null ?
                                        Duration.FromTimeSpan((TimeSpan)process.ProcessorUsageTime)
                                        : Duration.FromTimeSpan(TimeSpan.Zero),
            PhysicalMemoryUsageBit = process.PhysicalMemoryUsageBit ?? 0,
            ProcessName = process.ProcessName ?? string.Empty,
            ProcessId = process.ProcessId,
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

    private static bool GetStartTime(System.Diagnostics.ProcessThread thread, out string startTime)
    {
        startTime = string.Empty;

        try
        {
            startTime = thread.StartTime.ToString();
        }
        catch (Exception) { }
        
        return startTime == string.Empty;
    }

    public static Connection DeriveProtoConnectionType(this IConnectionInfo connection)
    {
        return new()
        {
            Id = connection.Id.ToString(),
            Name = connection.Name,
            LocalEndpoint = connection.LocalEndpoint ?? string.Empty,
            RemoteEndpoint = connection.RemoteEndpoint ?? string.Empty,
            RemoteApplication = connection.RemoteApplication ?? string.Empty,
            ConnectionInformation = { connection.ConnectionInformation?.DeriveProtoDictionaryType() ?? new MapField<string, string>() },
            Status = connection.Status
        };
    }

    public static ProcessInfoCollectorData DeriveProtoRuntimeInfoType(this Entities.ProcessInfoCollectorData runtimeInfo)
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

    public static Registration DeriveProtoRegistrationType(this RegistrationInfo registration)
    {
        return new()
        {
            ServiceType = registration.ServiceType ?? string.Empty,
            ImplementationType = registration.ImplementationType ?? string.Empty,
            LifeTime = registration.LifeTime ?? string.Empty
        };
    }

    public static ModuleProto DeriveProtoModuleType(this ModuleInfo module)
    {
        return new()
        {
            Name = module.Name ?? string.Empty,
            Location = module.Location ?? string.Empty,
            Version = module.Version.ToString() ?? string.Empty,
            VersionRedirectedFrom = module.VersionRedirectedFrom ?? string.Empty,
            PublicKeyToken = module.PublicKeyToken == null ? ByteString.Empty: ByteString.CopyFrom(module.PublicKeyToken)
        };
    }

    public static Subsystem DeriveProtoSubsystemType(this SubsystemInfo subsystem)
    {
        return new()
        {
            Name = subsystem.Name,
            Path = subsystem.Path ?? string.Empty,
            Port = subsystem.Port ?? 0,
            UiType = subsystem.UIType ?? string.Empty,
            Url = subsystem.Url ?? string.Empty,
            StartupType = subsystem.StartupType ?? string.Empty,
            State = subsystem.State ?? SubsystemState.Stopped,
            AutomatedStart = subsystem.AutomatedStart,
            Arguments = { subsystem.Arguments?.ToList() ?? new List<string>() },
            Description = subsystem.Description ?? string.Empty,
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

    public static MapField<string, TResult> DeriveProtoDictionaryType<T, R, TResult>(
        this IEnumerable<KeyValuePair<T, R>> dict,
        Func<R, TResult> converter)
    {
        var map = new MapField<string, TResult>();

        if (dict != null && dict.Any())
        {
            foreach (var kvp in dict)
            {
                var key = kvp.Key?.ToString();
                if (key == null || kvp.Value == null) continue;
                map.Add(key, converter.Invoke(kvp.Value));
            }
        }

        return map;
    }

    public static MapField<string, ProcessInfoCollectorData> DeriveRuntimeInformation(
        this IEnumerable<KeyValuePair<RuntimeInformation, MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.ProcessInfoCollectorData>> runtimeInformation)
    {
        var map = new MapField<string, ProcessInfoCollectorData>();

        if (runtimeInformation == null || !runtimeInformation.Any()) return map;

        foreach (var runtimeInfo in runtimeInformation)
        {
            map.Add(runtimeInfo.Key.Name, runtimeInfo.Value.DeriveProtoRuntimeInfoType());
        }

        return map;
    }
}
