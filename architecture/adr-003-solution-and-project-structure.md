<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# Architecture Decision Record: Solution and Project Structure

## Context

When it comes to creating and maintaining projects, multiple parallel
approaches are known, like monorepo, individual repos, submodules, etc.
And same way, when it comes to maintaining the actual source code, multiple
different approaches do exist, depending on each particular language or
technology and its capabilities or best practices.

## Decision

- We would avoid the use of submodules
- If needed, we would employ multiple individual repos instead
- We would provide editor configuration files for both Visual Studio Code and Visual Studio .NET
- We would depend on Directory.Build.targets/props and folder structures instead of custom csproj customizations
- We would depend on using an eng/Dependencies.props file instead of having the versions of Nuget files
repeatedly entered into the csproj files
- We would use automated tooling to achieve the maintenance of Dependencies.props file

## Status

Approved

## Consequences

- We would need to implement an automated tooling for csproj maintenance
- We would need to add that tooling as part of a build check (e.g. run that tool in dry run and break if it would
have done any changes)
