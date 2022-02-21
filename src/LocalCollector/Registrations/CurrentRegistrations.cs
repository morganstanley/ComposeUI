namespace ProcessExplorer.Entities.Registrations
{
    public interface IRegistration
    { 
        public string ImplementationType { get; set; }
        public string LifeTime { get; set; }
        public string ServiceType { get; set; }
    }

    public class Registration : IRegistration
    {
        public Registration(string type, string serviceType, string lifeTime)
        {
            ImplementationType = type;
            ServiceType = serviceType;
            LifeTime = lifeTime;
        }
        public string ImplementationType { get ; set ; }
        public string LifeTime { get; set; }
        public string ServiceType { get; set; }
    }

    public class RegistrationMonitor
    {
        public IEnumerable<IRegistration>? Services { get; set; }
        public RegistrationMonitor(ICollection<Registration> services)
        {
            Services =  services;
        }

        public IEnumerable<IRegistration>? GetServices()
            => Services;
    }
}
