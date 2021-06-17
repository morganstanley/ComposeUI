# Architecture Decision Record: Configuration

## Context

ComposeUI has to fulfill a number of different goals.

1. It needs to be *modular*. Different software packages, written by
   different developers, should be usable and swappable in the same
   application with a minimum of effort.

2. ComposeUI applications need to be *transparent* and
   *introspectable*. It should always be as clear as possible what is
   going on at any given moment, and why the application is behaving
   in the way it does.

3. As a general-purpose UI framework, it needs to provide a strong
   set of default settings which are also highly overridable, and
   *configurable* to suit the unique needs of users.

## Decision

ComposeUI will take the "Configurability" philosophy to its logical
extreme, and encode as much information about the application as
possible in a single, highly general structure, that can be queried
through the communication bridge. This will include
not just configuration that is normally thought of as "config" data -
whether it is an information about which server to connect to or
where a button is on a ribbon, this would be stored in an in memory
data store that could be changed and get notified on the fly.

Some concrete examples include (but not limited to):

- Dependency injection components
- Runtime entities
- User interface component models
- Persistent schemas
- Locations of static and dynamic assets

This configuration value will have a *schema* that defines what types
of entities can exist in the configuration, and what their expected
properties are.

Each distinct module will have the ability to contribute to the schema
and define entity types specific to its own domain. Modules may
interact by referencing entity types and properties defined in other
modules.

## Status

Proposed

## Consequences

- Applications will be defined comprehensively and declaratively by a
  rich data structure.
- The config schema provides an explicit, reliable contract and set of
  extension points, which can be used by other modules to modify
  entities or behaviors.
- It will be easy to understand and inspect an application by
  inspecting or querying its configuration. It will be possible to
  write tools to make exploring and visualizing applications even easier.
- Developers will need to carefully decide what types of things are
  appropriate to encode statically in the configuration, and what must
  be dynamic at runtime.
  