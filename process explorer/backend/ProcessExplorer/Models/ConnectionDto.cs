using System.Collections.Concurrent;

namespace ProcessMonitor.Models
{
    public class ConnectionDto
    {
        public Guid? Id { get; init; }
        public string? Name { get; set; }
        public string? LocalEndpoint { get; set; }
        public string? RemoteEndpoint { get; set; }
        public string? RemoteApplication { get; set; }
        public string? RemoteHostname { get; set; }
        public ConcurrentDictionary<string, string>? ConnectionInformation { get; set; } = new ConcurrentDictionary<string, string>();
        public object? Status { get; set; }
    }
}