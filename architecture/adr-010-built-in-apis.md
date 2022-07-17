# Architecture Decision Record: Built-in APIs should be built on top of the Message Router

## Context

ComposeUI as a framework will provide a set of standard APIs to modules. 
One of these standard APIs is the pub/sub/invoke functionality of the Message Router.
Modules will communicate and provide services for each other via the Message Router.
However, there might be other framework services that we provide, such as login,
local/profile storage, telemetry, etc. These APIs need client libraries for all the
languages and technologies we support, including at minimum JavaScript and .NET.
Since ComposeUI modules can run in their own process, we have to implement cross-process
communication and build our client libraries around technologies like WebSocket and gRPC
- we can call these communication technologies _ports_, each port harbouring _endpoints_ for
individual APIs.
Every built-in API must work using any of the ports, resulting in a cascade of dependencies and liabilities:
adding one more API requires us to add a new endpoint to all the ports on all the platforms;
adding another port requires us to add all endpoints on all platforms; and supporting a new platform requires us
to implement all the ports and endpoints on that platform.

## Decision

To reduce the overhead of extending the framework with more ports and APIs, built-in APIs
will all go through the Message Router, effectively using it as a general-purpose RPC solution.
Adding a new API will consist of defining a set of new request/response types.
Adding a new port will only require us to implement communication with the Message Router.
Supporting one more platform will require a new client library that can connect to 
a number of ports, each supported port having a fixed number of endpoints (those that communicate
with the Message Router).

## Status

Draft

## Consequences

This decision affects the design of our APIs. We will have a message-based architecture 
where APIs are defined in terms of request-response pairs instead of service objects.
We can still decide to group and encapsulate related services into service objects, but then
these service objects should be auto-generated from the message definitions.
