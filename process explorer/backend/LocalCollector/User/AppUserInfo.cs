using ProcessExplorer.Entities.EnvironmentVariables;

namespace ProcessExplorer.Entities
{
    public class AppUserInfo
    {
        #region Constructors
        public AppUserInfo()
            :this(false)
        {

        }
        public AppUserInfo(string userName, bool admin = false)
            :this(admin)
        {
            UserName = userName;
        }

        public AppUserInfo(bool admin = false)
        {
            UserName = Environment.UserName;
            MachineInfo = new MachineInfo();
            IsAdmin = admin;
        }
        public AppUserInfo(string userName, MachineInfo machine, bool admin = false)
            :this(userName, admin)
        {
            MachineInfo = machine;
        }
        public AppUserInfo(MachineInfo machine)
            :this(false)
        {
            MachineInfo = machine;
        }
        public AppUserInfo(string userName, MachineInfo machine)
            : this(userName, machine, false)
        {

        }
        #endregion

        #region Properties
        public string? UserName { get; set; }
        public bool? IsAdmin { get; set; }
        public MachineInfo? MachineInfo { get; set; } = default;
        #endregion
    }
}
