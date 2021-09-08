<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# Architecture Decision Record: Module Communication Security

## Context

ComposeUI is modular, the modules communicate with each other and the host application via a central
Messaging Module. The aim of this ADR is to establish how we make the communications secure.

When a module connects to the Messaging Module, it is likely to be with a WebSocket connection.
We could use Secure WebSocket (wss://) to encrypt the messages on the channel, but how do we make sure 
that the Module that connects is who it says it is? What if some malicious party tries to connect to our 
message bus and send messages?

There is no way to make the application 100% secure and prevent every angle of attack. Especially with a
web based client application, where all the data is available on the client side. One way to make it harder 
for an attacker to send harmful messages is to send some secret to the client and require it to send back
(some form of) the secret with every message. This is still vulnerable to man-in-the-middle attacks.

## Decision
- We are conscious that there is no way to fully secure the communication channel
- We want modules to easily communicate, therefore the default is that we do not enforce any security measures
- Provide an option to make it harder for an attacker to do harm

## Status

Proposed

## Consequences
- Modules can simply connect to the central Messaging Module and start communicating
- A Module can choose (by config) to use stricter security measures if needed
