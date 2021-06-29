<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# Architecture Decision Record: Use of C4

## Context

We need a way to help the development team describe and communicate software architecture, both during up-front design sessions and ADRs and also when retrospectively documenting the codebase, including the ability to create maps of code, at various levels of details.

Although primarily aimed at software architects and developers, the C4 model provides a way for software teams to efficiently communicate their software architecture, telling different stories to different audiences.

The C4 model is a lean graphical notation technique for modelling the architecture of software systems, an abstraction first approach to diagramming architecture, based upon abstractions that reflect how software architects and developers think about and build software. The deliberately small set of abstractions and diagram types is what makes the C4 model easy to learn and use.

Good software architecture diagrams assist with communication (both inside and outside the development and product teams), onboarding new staff, easy of contribution, risk identification, threat modelling, etc.

## Decision

Within ComposeUI, we are to position C4 as the preferred way to capture system architecture and how it is deployed, both as a a means to creating consistent and easily to understand diagrams as code that can live in the source control, and also as a data model that can be used to feed static analysis systems thereby reducing the overhead for developers and improving accuracy of the decisions and suggestions of those systems.

## Status

Proposed

## Consequences
