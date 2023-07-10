<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->
# Architecture Decision Record: Module Structure & Loading

## Status

Approved

## Context

ComposeUI needs to be as modular as possible. Not only do we want the community to be able to contribute new abilities and features that integrate well with the core and with each other, we want some of the basic functionality of ComposeUI to be swappable for alternatives as well.

[ADR-002](adr-002-configuration.md) specifies that one role of modules
is to contribute schema to the application config. Other roles of
modules would include providing code (as any library does), and
querying and updating the config during the startup
process or later. Additionally, since modules can depend upon each other, they
must specify which modules they depend upon.

Ideally there will be as little overhead as possible for creating and
consuming modules.

Some of the general problems associated with plugin/module systems include:

- Finding and downloading the implementation of the module.
- Discovering and activating the correct set of installed modules.
- Managing module versions and dependencies.

## Decision

- Discovery and loading of modules would be separated
- Communication would be based on the internal messaging bus between them
- Smart defaults would be provided to enable easy startup and to avoid 22 catch problem

## Consequences

- Applications could chose between a multitude of different config stores for storing their modules and a number of ways to load them
- Applications would clearly need to express their module dependencies, would not be able to depend on some magical order anymore
