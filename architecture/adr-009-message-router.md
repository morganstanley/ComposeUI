<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title         |
| ------------- | ------------- | ------------- | ------------- |
| ADR           | adr-009       | Proposed      | Message Router Requirements |


# Architecture Decision Record: Message Router Requirements

## Context

The Message Router is one of ComposeUI's core components. It serves as a generic communication layer that modules can
build their APIs upon, and as such, needs to be simple, robust and high performing. In this ADR we are listing the
requirements for the MVP of the Message Router.

## Decision

### Simple, topic-based pub-sub

The Message Router should provide a topic-based publisher-subscriber platform with the publish API being as simple as

```
publish(topic, payload)
```

where `topic` is a string and `payload` is a property bag (or key-value collection). 

Language bindings may support serialization/deserialization of strongly typed data from the payload, however the core
API will only define the format for the most common, primitive types only. Keys and values should be considered to be
strings for the communication layer.

Payload size should be limited to avoid using the Message Router as a content delivery mechanism.
For the sake of simplicity, message headers are not supported, mostly to discourage the creation of high level, 
custom protocols that should be implemented by more suitable technologies.

### Service calls

The router should also provide APIs allowing modules to register services that can be invoked by other modules.
Service calls should be synchronous (ie. wait for a response). It is the responsibility of language bindings
to correlate messages into request-response pairs and provide a simple API for service calls:

```
response = invoke(service, request)
```

where `service` is a string, and `request` and `response` are property bags.

The router should handle recursive service calls (A invokes B who invokes A again before returning a
response). Such communication patterns should not degrade performance or cause deadlocks.

### Plugable message pipelines

There should be an easy-to-configure, plugable pipeline for mapping topics to external services such as Kafka or
Azure Service Bus. These plugins will be .NET assemblies loaded into the process of the router, with the ability to
inject and intercept messages using a simple API.

### High throughput

Since ComposeUI apps are GUI applications, and application components communicate over the Message Router, it should
be designed from the ground up for high performance, with cross-process (local) communication in the center.

### Delivery guarantees

For cross-process communication, the Message Router should guarantee Exactly Once delivery. However, messages
should not be persisted between sessions, that is, pending messages should be deleted when the application shuts down.
Messages should also be delivered in strict order at least within a topic or service address, eg. subscribers of a
topic should receive messages in the order in which the publisher sent them;

For cross-machine communication, no such guarantees are provided.

## Consequences

For the MVP, we must define supported communication protocols early on, as language bindings must also support them.
We should use well-known protocols and formats to minimize the cost of implementing language bindings - for this
reason, we will likely drop SignalR and similar non-standard mechanism from the list of candidates.
