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

public class MockModuleLoader : Mock<IModuleLoader>
{
    public MockModuleLoader()
    {
        Setup(_ => _.StartModule(It.IsAny<StartRequest>()))
            .Returns(async(StartRequest startRequest) => await HandleStartRequest(startRequest));

        Setup(_ => _.StopModule(It.IsAny<StopRequest>()))
            .Callback(async (StopRequest stopRequest) => await HandleStopRequest(stopRequest));

        Setup(_ => _.LifetimeEvents)
            .Returns(() => LifetimeEvents);
    }

    public IObservable<LifetimeEvent> LifetimeEvents => _subject;
    private List<IModuleInstance> _startRequests = new();
    private readonly object _lock = new();
    private Subject<LifetimeEvent> _subject = new();

    private Task<IModuleInstance> HandleStartRequest(StartRequest startRequest)
    {
        IModuleInstance instance = new MockModuleInstance(
            startRequest,
            new MockModuleManifest() { Id = startRequest.ModuleId });

        lock (_lock)
        {
            _startRequests.Add(instance);
            _subject.OnNext(new LifetimeEvent.Starting(instance));
            _subject.OnNext(new LifetimeEvent.Started(instance));
        }

        return Task.FromResult(instance);

    }

    private Task HandleStopRequest(StopRequest stopRequest)
    {
        lock (_lock)
        {
            var instance = _startRequests.FirstOrDefault(inst => inst.InstanceId == stopRequest.InstanceId);
            if (instance != null)
            {
                _startRequests.Remove(instance);
                _subject.OnNext(new LifetimeEvent.Stopping(instance));
                _subject.OnNext(new LifetimeEvent.Stopped(instance));
            }
        }

        return Task.CompletedTask;
    }

    private class MockModuleInstance : IModuleInstance
    {
        private readonly IModuleManifest _moduleManifest;
        private readonly StartRequest _startRequest;

        public Guid InstanceId => _instanceId;
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly string _fdc3InstanceId = Guid.NewGuid().ToString();
        public IModuleManifest Manifest => _moduleManifest;

        public StartRequest StartRequest => _startRequest;

        public IEnumerable<object> GetProperties()
        {
            return new object[]
            {
                new Fdc3StartupProperties() { InstanceId = _fdc3InstanceId },
            };
        }

        public IEnumerable<T> GetProperties<T>()
        {
            return GetProperties().OfType<T>();
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

        public string[] Tags => ["tag1", "tag2"];

        public Dictionary<string, string> AdditionalProperties => new() { { "color", "blue" } };
    }
}
