
using ProcessExplorer.Entities;
using ProcessExplorer.Entities.Connections;
using ProcessExplorer.Entities.EnvironmentVariables;
using ProcessExplorer.Entities.Modules;
using ProcessExplorer.Entities.Registrations;
using System.Net.Http.Headers;

namespace ProcessExplorer
{
    public class InfoAggregator
    {
        public InfoAggregator(Guid id, AppUserInfo user, EnvironmentMonitor envs, ConnectionMonitor cons,
            ProcessMonitor processes)
        {
            Id = id;
            User = user;
            EnvironmentVariables = envs;
            Connections = cons;
            Processses = processes;
        }
        public InfoAggregator(Guid id, AppUserInfo user, EnvironmentMonitor envs, ConnectionMonitor cons, 
            ProcessMonitor processes, RegistrationMonitor registrations, ModuleMonitor modules) { 
            Id = id;
            User = user;
            EnvironmentVariables = envs;
            Connections = cons;
            Processses = processes;
            Registrations = registrations;
            Modules = modules;
        }
        public Guid? Id { get; set; } = default;
        public AppUserInfo User { get; set; }
        public RegistrationMonitor? Registrations { get; set; } = default;
        public EnvironmentMonitor? EnvironmentVariables { get; set; } = default;
        public ConnectionMonitor? Connections { get; set; } = default;
        public ModuleMonitor? Modules { get; set; } = default;
        public ProcessMonitor Processses { get; set; }

        //SAMPLE MESSAGE SENDING?
        public async void SendMessage(string url)
        {
            using(var client = new HttpClient())
            { 
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage responseMessage = await client.PostAsJsonAsync("/api/info", this);
                var result = await responseMessage.Content.ReadAsStringAsync();
                if (responseMessage.IsSuccessStatusCode)
                {
                    Uri? infoUrl = responseMessage.Headers.Location;
                    Console.WriteLine(infoUrl);
                }
            }
        }
    }
}
