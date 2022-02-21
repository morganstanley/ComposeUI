
namespace ProcessMonitor.Models
{
    public class InfoAggregatorDto
    {
        public Guid? Id { get; set; } = default;
        public AppUserInfoDto? User { get; set; } = default;
        public RegistrationMonitorDto? Registrations { get; set; } = default;
        public EnvironmentMonitorDto? EnvironmentVariables { get; set; } = default;
        public ConnectionMonitorDto? Connections { get; set; } = default;
        public ModuleMonitorDto? Modules { get; set; } = default;
        public ProcessMonitorDto? Processses { get; set; } = default;
    }

    public class ProcessMonitorDto
    {
        public List<ProcessInfoDto> processes { get; set; } = new List<ProcessInfoDto>();
    }

    public class ModuleMonitorDto
    {
        public List<ModuleInfoDto>? currentModules { get; set; } = new List<ModuleInfoDto>();
    }

    public class ConnectionMonitorDto
    {
        public List<ConnectionDto>? connections { get; set; } = new List<ConnectionDto>();
    }

    public class EnvironmentMonitorDto
    {
        public List<EnvironmentInfoDto> environmentVariables { get; set; } = new List<EnvironmentInfoDto>();
    }

    public class RegistrationMonitorDto
    {
        public List<RegistrationDto>? Services { get; set; } = new List<RegistrationDto>();
    }

    public class AppUserInfoDto
    {
        public string? UserName { get; set; } =  default;
        public bool? IsAdmin { get; set; } = default;
        public MachineInfoDto? MachineInfo { get; set; } = default;
    }
}
