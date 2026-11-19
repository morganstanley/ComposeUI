# MorganStanley.ComposeUI.Messaging.Server.ModuleAccessTokens

`MorganStanley.ComposeUI.Messaging.Server.ModuleAccessTokens` lets a ComposeUI host give every module instance its own Message Router access token, and lets the Message Router server tell who is connecting.

Instead of one shared token that every module knows, each running module instance gets a token of its own. When a client connects, the server maps its token to the identity of the module instance it was issued to. When the module stops, its token is revoked.

## Features

- One random 256-bit access token per module instance (64 hex characters)
- A separate host token for the shell's own in-process Message Router client, which is never given to modules
- An `IClientAuthenticator` implementation that turns a token into a `ClaimsPrincipal` (the client's identity)
- Automatic revocation when a module stops, including when it fails to start
- Fixed-time token comparison

## Concepts

| Type | Role |
| --- | --- |
| `AddModuleAccessTokens()` | The single entry point. Registers the token store, the authenticator and the revocation service, and returns the store. |
| `IModuleTokenStore` | Issues, validates and revokes tokens, and holds the host token. |
| `ModuleIdentity` | The module instance a token was issued to: its instance ID and its module ID (from the manifest). |
| `MessageRouterClaimTypes` | The claim types and values set on the authenticated client's principal. |

`AddModuleAccessTokens()` registers three things, which are implementation details of this library:

- an in-memory token store that implements `IModuleTokenStore`,
- an authenticator that implements the server's `IClientAuthenticator` using that store,
- a hosted service that revokes the token of a module instance when the module stops.

The `IClientAuthenticator` abstraction lives in the Message Router server library, not here. The Message Router server picks up a registered `IClientAuthenticator` automatically, so a host can plug in a different authenticator without taking a dependency on module tokens.

## Usage

### 1. Register the tokens and the server

```csharp
using MorganStanley.ComposeUI.Messaging.Server.ModuleAccessTokens;

var tokenStore = services.AddModuleAccessTokens();

services.AddMessageRouterServer(mr => mr.UseWebSockets());

// The host's in-process client connects with the host token.
services.AddMessageRouter(
    mr => mr
        .UseServer()
        .UseAccessToken(tokenStore.HostToken));
```

Call `AddModuleAccessTokens()` only once. It returns the store because the host's own client needs the host token when it is configured, before the service provider is built.

An `IModuleLoader` must also be registered. The revocation service subscribes to the module loader's lifetime events and revokes the token of an instance on `LifetimeEvent.Stopped`. This also covers modules that fail to start, because the module loader publishes `Stopped` in that case.

### 2. Issue a token when a module starts

Issue the token in a startup action and hand it to the module. The token is issued per module instance, and calling `Issue` again for the same instance returns the same token.

```csharp
var token = tokenStore.Issue(
    startupContext.ModuleInstance.InstanceId,
    startupContext.ModuleInstance.Manifest.Id);
```

How the token reaches the module depends on the module type. The ComposeUI shell does this:

- **Native modules:** the token is added to the module's environment variables (`MorganStanley.ComposeUI.Messaging.EnvironmentVariableNames.AccessToken`), together with the WebSocket URI.
- **Web modules:** the token is written into the page as `window.composeui.messageRouterConfig.accessToken`, together with the WebSocket URL.

See `WebModuleMessageRouterConfigurator` and `NativeModuleMessageRouterConfigurator` in the shell for the full implementations.

## Client identity

After a client connects, the server stores the `ClaimsPrincipal` returned by the authenticator for the lifetime of the connection.

| Claim | Host client | Module client |
| --- | --- | --- |
| `ClaimTypes.Name` | `MessageRouterClaimTypes.HostName` | The module ID |
| `MessageRouterClaimTypes.IdentityType` | `host` | `module` |
| `MessageRouterClaimTypes.ModuleInstanceId` | not set | The module instance ID |
| `MessageRouterClaimTypes.ModuleId` | not set | The module ID |

The authentication type of the identity is `MessageRouterClaimTypes.AuthenticationType`.

A connection is rejected if its token is missing, unknown or has been revoked.

> **Note:** the server currently stores the principal but does not yet expose it to services or use it to authorise requests. Establishing the identity is the first step, and enforcing access rules on top of it is not part of this library yet.

## Security notes

- Tokens are generated with a cryptographically secure random number generator.
- Tokens are compared in fixed time, and every stored token is compared on a lookup, so the time taken does not reveal which token matched.
- Tokens are kept in memory only. They are lost when the host stops, and nothing is persisted.
- The host token is different from every module token and is valid for the lifetime of the store. Do not pass it to modules.
- `IModuleTokenStore.Issue` rejects an empty module instance ID and an empty module ID.

## Dependencies

- `MorganStanley.ComposeUI.Messaging.Server`
- `MorganStanley.ComposeUI.ModuleLoader.Abstractions`
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions)
- [Microsoft.Extensions.Hosting.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions)

## When to Use

- When your host starts modules and you want each module instance to have its own Message Router access token
- When you need the Message Router server to know which module a client belongs to
- When tokens must stop working as soon as their module stops
