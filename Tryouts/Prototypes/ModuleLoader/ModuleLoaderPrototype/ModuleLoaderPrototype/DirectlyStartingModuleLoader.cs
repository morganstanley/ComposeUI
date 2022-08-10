using ModuleLoaderPrototype.Interfaces;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ModuleLoaderPrototype
{
    public class DirectlyStartingModuleLoader : ITypeAModuleLoader
    {
        private readonly Subject<ProcessRestarted> _processRestarted = new Subject<ProcessRestarted>();
        public IObservable<ProcessRestarted> ProcessRestarted => _processRestarted;

        private readonly Dictionary<int, Process> _processes = new Dictionary<int, Process>();

        private readonly bool _autoRestart;
        public DirectlyStartingModuleLoader(bool autoRestart)
        {
            _autoRestart = autoRestart;
        }

        public int StartProcess(LaunchRequest request)
        {
            return StartProcessImpl(request.path);
        }

        private int StartProcessImpl(string path)
        {
            Process process = ProcessLauncher.LaunchProcess(path);
            process.Exited += HandleProcessExitedUnexpectedly;
            _processes.Add(process.Id, process);
            return process.Id;
        }

        public async Task<bool> StopProcess(int pid, CancellationToken cancellationToken = default)
        {
            Process p;
            if (!_processes.TryGetValue(pid, out p))
            {
                throw new Exception("This PID is not owned by the module loader");
            }
            p.Exited -= HandleProcessExitedUnexpectedly;
            await Task.WhenAny(Task.Run(() => p.CloseMainWindow()), Task.Delay(TimeSpan.FromSeconds(1)));
            if (p.HasExited)
            {
                _processes.Remove(pid);
                return true;
            }

            p.Kill();
            if (p.HasExited)
            {
                _processes.Remove(pid);
                return true;
            }

            return false;
        }

        private void HandleProcessExitedUnexpectedly(object sender, EventArgs e)
        {
            Process p = (Process)sender;
            p.Exited -= HandleProcessExitedUnexpectedly;

            var filename = p.StartInfo.FileName;
            _processes.Remove(p.Id);
            if (_autoRestart)
            {
                var pid = StartProcessImpl(filename);
                _processRestarted.OnNext(new ProcessRestarted { oldPid = p.Id, newPid = pid });
            }
        }
    }
}
