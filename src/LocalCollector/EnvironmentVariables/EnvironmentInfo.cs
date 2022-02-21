
namespace ProcessExplorer.Entities.EnvironmentVariables
{
    public class EnvironmentInfo
    {
        public EnvironmentInfo(string? variable, string? value)
        {
            Variable = variable;
            Value = value;
        }
        public string? Variable { get; protected set; }
        public string? Value { get; protected set; }
    }
}
