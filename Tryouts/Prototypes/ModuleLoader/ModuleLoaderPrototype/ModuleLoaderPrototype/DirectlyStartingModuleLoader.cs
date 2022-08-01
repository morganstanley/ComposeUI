using ModuleLoaderPrototype.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class DirectlyStartingModuleLoader : ITypeAModuleLoader
    {
        private readonly Subject<ProcessRestarted> _processRestarted = new Subject<ProcessRestarted>();
        public IObservable<ProcessRestarted> ProcessRestarted => _processRestarted;

        private readonly Dictionary<int, Process> _processes = new Dictionary<int, Process>();

        public int StartProcess(LaunchRequest request, CancellationToken cancellationToken = default)
        {
            return StartProcessImpl(request.path, cancellationToken);
        }

        private int StartProcessImpl(string path, CancellationToken cancellationToken = default)
        {
            Process process = new Process();
            process.StartInfo.FileName = path;
            process.EnableRaisingEvents = true;
            process.StartInfo.UseShellExecute = false;

            process.Exited += HandleProcessExitedUnexpectedly;
            process.Start();
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
            var pid = StartProcessImpl(filename);

            _processRestarted.OnNext(new ProcessRestarted { oldPid = p.Id, newPid = pid });
        }
    }
}
