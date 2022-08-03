using System.Diagnostics;

namespace ModuleLoaderPrototype
{
    internal static class ProcessLauncher
    {
        internal static Process LaunchProcess(string path)
        {
            Process process = new Process();
            process.StartInfo.FileName = path;
            process.EnableRaisingEvents = true;
            process.Start();
            return process;
        }
    }
}
