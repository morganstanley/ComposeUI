<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title          |
| ------------- | ------------- | ------------- | -------------- |
| ADR           | adr-012       | Approved      | Message Router |


# Architecture Decision Record: Message Router

## Context

As [ADR-004](adr-004-module-loading.md) states, ComposeUI needs to be as modular as possible.
This means any module and functionality should be configurable ([ADR-002](adr-002-configuration.md)).
One of the core modules of ComposeUI is the messaging module, or the message router. The message router
should be responsible for every communication that happens between processes.
It should be an independent pluggable module that runs under the main ComposeUI process, delivering
messages between processes and optionally other devices.

In [ADR-005](adr-005-messaging.md) SignalR was selected for inter-process communication. As development progressed we needed to revisit this decision along the following points:
 - We only need the most basic features of SignalR
 - We cannot ensure that SignalR is available and supported for all technologies potentially used by users
 - The benefits of adding a third party dependency like SignalR should outweight the added risk

## Decision

At the current stage only cross-process messaging is in scope. We need efficient, multi-channel communication between processes with a simple well defined protocol that is easy to connect to using any technologies.

 - We will implement our own lightweight MessageRouter service
 - The service will use JSON format messages as it is a well-known, efficient format
 - Clients can connect to the service via WebSockets, as it is generally available and efficient
 - Clients can subscribe and publish to topics
 - We will provide client libraries for the MessageRouter for popular frontend technologies (Initially for JavaScript and .NET)
 - The protocol is well documented and the client is easy to implement with any mainstream technology

## Consequences

 - Applications that normally consist of multiple processes (e.g. trading applications) won't have to setup their own
messaging infrastructure
 - The message router comes with a message format that is easily extendable without breaking changes, making it
flexible to use for clients
 - It will be an easy to understand and easy to setup module where the client doesn't have to worry about creating
WebSocket connections and maintaining them
 - We will have to maintain the MessageRouter Service and Client
 - We are not depending on a third party communication library
 - We will have to revisit requirements for cross-machine communication when it gets into scope
