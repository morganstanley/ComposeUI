<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title         |
| ------------- | ------------- | ------------- | ------------- |
| ADR           | adr-007       | Accepted      | Use of C4 |


# Architecture Decision Record: Use of C4

## Context

We need a way to help the development team describe and communicate software architecture, both during up-front design sessions and ADRs and also when retrospectively documenting the codebase, including the ability to create maps of code, at various levels of details.

Although primarily aimed at software architects and developers, the C4 model provides a way for software teams to efficiently communicate their software architecture, telling different stories to different audiences.

The C4 model is a lean graphical notation technique for modelling the architecture of software systems, an abstraction first approach to diagramming architecture, based upon abstractions that reflect how software architects and developers think about and build software. The deliberately small set of abstractions and diagram types is what makes the C4 model easy to learn and use.

Good software architecture diagrams assist with communication (both inside and outside the development and product teams), onboarding new staff, easy of contribution, risk identification, threat modelling, etc. Also, good software architecture diagrams help to align everybody's understanding of the software being built, helping therefore the team to be more efficient.

When it comes to actually to render C4, once you have the architecture models composed of the common SDL format, there are a number of tools available to render them, like PlantUML, Mermaid, Structurizr, and more.

## Decision

Within ComposeUI, we are to position C4 as the preferred way to capture system architecture and how it is deployed, both as a means to creating consistent and easy to understand diagrams as code that can live in the source control, and also as a data model that can be used to feed static analysis systems thereby reducing the overhead for developers and improving accuracy of the decisions and suggestions of those systems.

To describe the actual C4, we would use PlantUML extensions and rendering.

## Consequences

- Instead of pictures, we would store C4 diagram descriptions via PlantUML in the repo
- As part of the documentation build process would we generate the relevant images that would be uploaded as part of gh-pages
