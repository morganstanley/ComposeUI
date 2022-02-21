using System.Collections;
using System.Collections.Concurrent;

namespace ProcessExplorer.Entities.EnvironmentVariables
{
    public class EnvironmentMonitor
    {
        public ConcurrentBag<EnvironmentInfo> environmentVariables;
        public EnvironmentMonitor()
        {
            environmentVariables = new ConcurrentBag<EnvironmentInfo>();
            GetEnvironmentVariables();
        }
        public EnvironmentMonitor(ConcurrentBag<EnvironmentInfo> environmentVariables)
            => this.environmentVariables = environmentVariables;
        private void LoadEnvironmentVariables()
        {
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                environmentVariables.Add(new EnvironmentInfo(item.Key.ToString(), item.Value?.ToString()));
            }
        }
        private ConcurrentBag<EnvironmentInfo> GetEnvironmentVariables()
        {
            LoadEnvironmentVariables();
            return environmentVariables;
        }
    }
}
