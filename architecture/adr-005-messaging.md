<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->
# Architecture Decision Record: Messaging

## Status

Approved

## Context

As [ADR-004](adr-004-module-loading.md) states, ComposeUI needs to be as modular as possible.
This means any module and functionality should be configurable ([ADR-002](adr-002-configuration.md)).
One of the core modules of ComposeUI is the messaging module, or the message router. The message router
should be responsible for every communication that happens between processes.
It should be an independent pluggable module that runs under the main ComposeUI process, delivering
messages between processes and optionally other devices.

## Decision

Since there are going to be
different types of consumers of ComposeUI (Web, WPF, WinForms, ASP.NET, etc) we differentiate two kinds of
messaging: cross-process and cross-machine. Cross-process messaging is strictly between processes on the same PC,
while Cross-machine is between one or more devices.
A few concrete examples include (but not limited to):

- Notifications
- Logging, telemetry
- Querying
- Pub-Sub
- Window management

Since consumers can decide which modules they want to plug into ComposeUI,
we provide one out-of-the-box implementation for being able to send and receive messages between processes.
The public API should not contain any dependency on any library.
This default implementation is going be based on SignalR Core.

There are multiple advantages when it comes to SignalR:

- An open-source out-of-the-box library that is fast and reliable
- Supports JSON and MessagePack for binary format
- With Azure SignalR, not only cross-process but cross-machine messaging can be done with low latency
- Uses WebSockets and has a JS library, so Web-based applications can be integrated
- Real-time solution that can easily be setup for Pub-Sub messaging

The message router is going to be an ASP.NET Core-based backend service that exposes one or more endpoints/SignalR Hubs
that the applications can connect to.

## Consequences

- Applications that normally consist of multiple processes (e.g. trading applications) won't have to setup their own
messaging infrastructure
- The message router comes with a message format that is easily extendable without breaking changes, making it
flexible to use for clients
- It will be an easy to understand and easy to setup module where the client doesn't have to worry about creating
WebSocket connections and maintaining them
