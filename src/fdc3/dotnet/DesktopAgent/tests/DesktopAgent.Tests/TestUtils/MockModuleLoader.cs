/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Reactive.Subjects;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;

public class MockModuleLoader : IModuleLoader
{
    public IObservable<LifetimeEvent> LifetimeEvents => _subject;
    public IEnumerable<IModuleInstance> StartRequests { 
        get
        {
            lock (_lock)
            {
                return new List<IModuleInstance>(_startRequests);
            }
        }
    }

    private List<IModuleInstance> _startRequests = new();
    private readonly object _lock = new();
    private Subject<LifetimeEvent> _subject = new();

    public Task<IModuleInstance> StartModule(StartRequest startRequest)
    {
        IModuleInstance instance;
        lock (_lock)
        {
            instance = new MockModuleInstance(startRequest, new MockModuleManifest() { Id = startRequest.ModuleId });
            _startRequests.Add(instance);
            _subject.OnNext(new LifetimeEvent.Starting(instance));
            _subject.OnNext(new LifetimeEvent.Started(instance));
        }
        return Task.FromResult(instance);
    }

    public Task StopModule(StopRequest stopRequest)
    {
        lock (_lock)
        {
            var instance = _startRequests.FirstOrDefault(instance => instance.InstanceId == stopRequest.InstanceId);
            if (instance != null)
            {
                _startRequests = _startRequests.Where(request => request.InstanceId != stopRequest.InstanceId).ToList();
                _subject.OnNext(new LifetimeEvent.Stopping(instance));
                _subject.OnNext(new LifetimeEvent.Stopped(instance));
            }
        }

        return Task.CompletedTask;
    }

    public void StopAllModules()
    {
        lock (_lock)
        {
            var modules = _startRequests.AsEnumerable().Reverse().ToArray();
            _startRequests.Clear();
            foreach (var module in modules)
            {
                _subject.OnNext(new LifetimeEvent.Stopping(module));
                _subject.OnNext(new LifetimeEvent.Stopped(module));
            }
        }
    }

    private class MockModuleInstance : IModuleInstance
    {
        private readonly IModuleManifest _moduleManifest;
        private readonly StartRequest _startRequest;

        public Guid InstanceId => _instanceId;
        private readonly Guid _instanceId = Guid.NewGuid();

        public IModuleManifest Manifest => _moduleManifest;

        public StartRequest StartRequest => _startRequest;

        public IEnumerable<object> GetProperties()
        {
            return Enumerable.Empty<object>();
        }

        public MockModuleInstance(StartRequest startRequest, IModuleManifest moduleManifest)
        {
            _moduleManifest = moduleManifest;
            _startRequest = startRequest;
        }
    }

    private class MockModuleManifest : IModuleManifest
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string ModuleType { get; set; }
    }
}
