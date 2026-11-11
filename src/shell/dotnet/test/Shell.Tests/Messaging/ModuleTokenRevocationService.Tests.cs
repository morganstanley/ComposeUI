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

using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Messaging;

public class ModuleTokenRevocationServiceTests : IDisposable
{
    private readonly ModuleTokenStore _store = new();
    private readonly TestLifetimeEvents _lifetimeEvents = new();
    private readonly ModuleTokenRevocationService _service;

    public ModuleTokenRevocationServiceTests()
    {
        var moduleLoader = new Mock<IModuleLoader>();
        moduleLoader.SetupGet(_ => _.LifetimeEvents).Returns(_lifetimeEvents);
        _service = new ModuleTokenRevocationService(moduleLoader.Object, _store);
    }

    public void Dispose() => _service.Dispose();

    [Fact]
    public void The_token_is_revoked_when_the_module_stops()
    {
        var instance = CreateInstance();
        var token = _store.Issue(instance.InstanceId, "module-a");

        _lifetimeEvents.Publish(new LifetimeEvent.Stopped(instance));

        _store.TryValidate(token, out _).Should().BeFalse();
    }

    [Fact]
    public void The_token_is_revoked_when_the_module_stops_unexpectedly()
    {
        var instance = CreateInstance();
        var token = _store.Issue(instance.InstanceId, "module-a");

        _lifetimeEvents.Publish(new LifetimeEvent.Stopped(instance, isExpected: false));

        _store.TryValidate(token, out _).Should().BeFalse();
    }

    [Fact]
    public void Only_the_token_of_the_stopped_instance_is_revoked()
    {
        var stopped = CreateInstance();
        var running = CreateInstance();
        _store.Issue(stopped.InstanceId, "module-a");
        var runningToken = _store.Issue(running.InstanceId, "module-b");

        _lifetimeEvents.Publish(new LifetimeEvent.Stopped(stopped));

        _store.TryValidate(runningToken, out _).Should().BeTrue();
    }

    [Fact]
    public void The_token_stays_valid_for_other_lifetime_events()
    {
        var instance = CreateInstance();
        var token = _store.Issue(instance.InstanceId, "module-a");

        _lifetimeEvents.Publish(new LifetimeEvent.Starting(instance));
        _lifetimeEvents.Publish(new LifetimeEvent.Started(instance));
        _lifetimeEvents.Publish(new LifetimeEvent.Stopping(instance));

        _store.TryValidate(token, out _).Should().BeTrue();
    }

    [Fact]
    public void The_host_token_is_not_affected()
    {
        _lifetimeEvents.Publish(new LifetimeEvent.Stopped(CreateInstance()));

        _store.IsHostToken(_store.HostToken).Should().BeTrue();
    }

    [Fact]
    public void After_dispose_events_are_ignored()
    {
        var instance = CreateInstance();
        var token = _store.Issue(instance.InstanceId, "module-a");

        _service.Dispose();
        _lifetimeEvents.Publish(new LifetimeEvent.Stopped(instance));

        _store.TryValidate(token, out _).Should().BeTrue();
    }

    private static IModuleInstance CreateInstance()
    {
        var instance = new Mock<IModuleInstance>();
        instance.SetupGet(_ => _.InstanceId).Returns(Guid.NewGuid());

        return instance.Object;
    }

    private sealed class TestLifetimeEvents : IObservable<LifetimeEvent>
    {
        private readonly List<IObserver<LifetimeEvent>> _observers = new();

        public IDisposable Subscribe(IObserver<LifetimeEvent> observer)
        {
            _observers.Add(observer);

            return new Subscription(() => _observers.Remove(observer));
        }

        public void Publish(LifetimeEvent lifetimeEvent)
        {
            foreach (var observer in _observers.ToList())
            {
                observer.OnNext(lifetimeEvent);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _onDispose;

            public Subscription(Action onDispose) => _onDispose = onDispose;

            public void Dispose() => _onDispose();
        }
    }
}
