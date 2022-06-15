<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# Architecture Decision Record: Message Router Abstraction Layers

## Context

The Message Router is one of ComposeUI's core components. 
The functionality of the Router should be customizable and pluggable, therefore we separate
the distinct layers and their APIs, while we provide a default implementation for all of them.
This document describes the abstraction layers of the Message Router and how they interact with each other.

## Decision

The Message Router should be divided into three layers:
* Messaging
* Serialization
* Channel

### Messaging API

The Messaging API is a high level, object based communication API with the following features.

* Provides a publish-subscribe communication API
    * `Task PublishAsync<TMessage>(string topic, TMessage message)`
    * `Task SubscribeAsync<TMessage>(string topic, Action<RouterMessage<TMessage>> handler)`
    * `Task UnsubscribeAsync(string topic, Action<RouterMessage<TMessage>> handler)`
* Provides a request-response type service calls
    * `Task RegisterQueryResponderAsync<TResult>(string serviceId, IServiceResponder<TResult> responder)`
    * `Task UnregisterQueryResponderAsync<TResult>(string serviceId)`
    * `Task<TResult> QueryAsync(string serviceId, IServiceParams params)`
* Provides a way to swap out the default serialization and channel implementations
    * `void SetMessageSerializer(ISerializer serializer)`
    * `void SetMessageChannel(IChannel channel)`
* Error callback when something happens - e.g. the connection goes down
    * `void OnError(Action<RouterError> handler)`

### Serialization API

The serialization layer provides the functionality to convert message objects to byte sequences and vice versa. 

* `ReadOnlyMemory<byte> Serialize(object message)`
* `object Deserialize(ReadOnlyMemory<byte> message)`
* `void RegisterConverter<TCustom>(IConverter<TCustom> converter)`

Here the converter is for a custom type that is passed as (part of) the payload, 
but the serializer converts the whole message object that the higher level messaging API sends.

### Channel API

The channel API provides the functionality to send and receive packages asynchronously.

* `Task ConnectAsync()`
* `Task SendAsync(ReadOnlyMemory<byte> package)`
* `Task<ReadOnlyMemory<byte>> ReceiveAsync()`
* `Task DisconnectAsync()`

## Status

Draft

## Consequences
The messaging module (Message Router) uses the default implementation for serialization and
channel, but developers can register their own implementation to customize the behavior.
