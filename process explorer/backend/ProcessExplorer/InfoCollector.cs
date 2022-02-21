using ProcessMonitor.Models;
using System.Collections.Concurrent;

namespace ProcessExplorer
{
    public static class InfoCollector
    {
        public static ConcurrentDictionary<string, InfoAggregatorDto> Informations { get; set; } = new ConcurrentDictionary<string, InfoAggregatorDto>();
        public static void AddInformation(string assembly, InfoAggregatorDto info)
            => Informations.AddOrUpdate(assembly, info, (key, oldValue) => oldValue = info);
        public static void Remove(string assembly)
            => Informations.TryRemove(assembly, out _);
    }
}
