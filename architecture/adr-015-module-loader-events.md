<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title                   |
| ------------- | ------------- | ------------- | ----------------------- |
| ADR           | adr-015       | Approved      | Module Loader events    |


# Architecture Decision Record: Module Loader events

## Context
Module Loader is responsible for starting and stopping module instances. It is also responsible for emitting lifetime events of module instances, such as `Starting`, `Started`, `Stopping`, `Stopped`.

The current architecture is built on top of `IObservable<T>`:

```c#
public interface IModuleLoader
{
    IObservable<LifetimeEvent> LifetimeEvents { get; }

    // other members omitted for brevity
}
```

### Current usages

#### Raising events
The Module Loader is responsible for raising lifetime events. This is currently achieved by using a [Subject](https://learn.microsoft.com/en-us/previous-versions/dotnet/reactive-extensions/hh229173(v=vs.103)). Subjects implement both `IObservable<T>` and `IObserver<T>`, and when a lifetime event needs to be emitted we call `OnNext()` on the subject.

#### Consuming events
Module Loader lifetime events are consumed by ComposeUI infrastructure code, but can also be consumed by external applications.

Current infrastructure usages utilize `System.Reactive` extensions and its `LINQ` operators. For example:

```c#
internal sealed class ModuleService : IHostedService
{
    // ...
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _disposables.Add(
            _moduleLoader.LifetimeEvents
                .OfType<LifetimeEvent.Started>()
                .Where(e => e.Instance.Manifest.ModuleType == ModuleType.Web)
                .Subscribe(OnWebModuleStarted));
```

This usage filters lifetime events by type and by content to subscribe only to the `Started` events of `Web` module instances.

Another usage uses the `ObserveOn()` extension method which ensures that the observer's `OnNext()` method is invoked via the specified scheduler, in this case using the `DispatcherSynchronizationContext` from WPF.

In our FDC3 Desktop Agent implementation we use `System.Reactive.Async` to convert the `IObservable<T>` events to `IAsyncObservable<T>` so that the `OnNext()` calls are properly awaited:

```c#
        var observable = _moduleLoader.LifetimeEvents.ToAsyncObservable();
        var subscription = await observable.SubscribeAsync(async lifetimeEvent =>
        {
            // code containing await
```
## Alternatives considered

### Events
The most obvious alternative to `IObservable<T>` is the built-in `event` feature in C# / .NET. Raising lifetime events would be very similar to the current code, however subscribing to these events would be limited to the `+=` operator. Since we're already relying on the `LINQ` operators provided by Rx.NET using built-in `events` instead would be inferior.

The current implementation of the FDC3 Desktop Agent relies on awaiting in the `OnNextAsync()` handler. Implementing the same behavior by using built-in `events` would be challenging (if possible at all).

### `IAsyncObservable<T>`

`IAsyncObservable<T>` is part of `System.Reactive.Async` package, for which the latest available version as of today is 6.0.0-alpha.18, which is a pre-release version.

We are already using this package internally but we don't want to enforce pre-release dependencies on our customers by changing the public interface to use `IAsyncObservable<T>`.

## Decision

We continue using `IObservable<T>` for Module Loader lifetime events.

## Consequences

We will revisit this ADR once there will be a stable version of `System.Reactive.Async` package available.