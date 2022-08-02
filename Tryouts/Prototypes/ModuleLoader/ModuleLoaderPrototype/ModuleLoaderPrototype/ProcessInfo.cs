using System.Diagnostics;

namespace ModuleLoaderPrototype
{
    internal class ProcessInfo
    {
        public ProcessInfo(string name, Process process)
        {
            Name = name;
            Process = process;
        }

        public string Name { get; }
        public Process Process { get; }
        public int ProcessId => Process.Id;
    }
}
