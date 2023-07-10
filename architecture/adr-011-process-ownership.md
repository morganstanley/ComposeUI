# Architecture Decision Suggestion: Process Ownership

## Status

Accepted

## Context
ComposeUI aims to support many different kind of modules and applications, offering the possibility to use different graphical shells for displaying them. One of the main features is the possibility to run these applications (even ones without any intended ComposeUI integration) in isolated processes. We wish to provide features for monitoring the state of the applications and the possibility to e.g. restart crashed processes. In this ADR we decide what components of ComposeUI are responsible for starting and maintaining processes.

## Decision
 - The central components of ComposeUI are the owners of processes with exclusive rights to start and stop processes
 - The shell or other modules must request starting or stopping processes via the interface of the core components
 - The core components are responsible for tracking process lifecycle and notifying about changes
 
## Consequences
 - ComposeUI core must provide a complete solution for handling process lifecycle
 - We gain the ability to present advanced lifecycle handling features for modules developed with ComposeUI in mind
 - We need to design the interface for process handling in a way that suits the needs of desired scenarios
 - We need to design the interface for process handling in an extensible way so older shells/applications can work with newer versions of ComposeUI central components.

## Considerations
Letting non-core components have more direct control over processes could present more flexibility to developers wishing to customize Compose, while possibly making ComposeUI more lightweight and easier to handle, but it opens up way for several possible issues:
 - Process-handling functionality can have duplicated or multiple diverging implementations leading to incompatibilities between modules and shells
 - Opening up a main feature to depend on correct implementation, e.g. concurrency handling or lifecycle handling, may compromise end-user experience
 - Reduced ability to present complex lifecycle handling to modules targeted for use with Compose

If the need arises, we can introduce points of customization in the core components that developers can use to alter or extend the default ComposeUI behavior.