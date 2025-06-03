# Google sign in, Jwt Bearer authentication using ASP.NET Core / React / Aspire

This project is a working example of using React as the front end, ASP.NET Core as the back end, with both managed by Aspire, to:

- Sign in on the back end using Google Oauth2.
- Authenticate both http/s and websocket/s back end endpoints using Jwt Bearer header authentication. 

## Purpose 

This was developed to provide a working example given the poor or outdated documentation and examples relating to how Microsoft's Google 
authentication classes work and how they should be configured, what middleware is and isn't required, what order 
middleware should be placed in the pipeline, how to set up React with Aspire, how Microsoft's Identity and Identity Core
should be involved, if at all, subtle changes in how setup is performed over different versions, etc. 
--  _some_ of the things that, if they didn't exist, would make working with ASP.NET Core much more of a joy.

## Some details

### Version 

This example is particularly focused on the **.NET 9** version of ASP.NET Core. 

### Implementation Notes

- It is mostly implemented with minimal APIs for clarity. However, the Websocket/Signalr hub is controller based.
- Front end is Vite-built basic react and does not include React-Router or other router.
- Comments in code note things discovered empirically when building this example (aren't clearly documented, or documentation is wrong).

### Features

- Includes a pattern for overriding authentication handlers (e.g. `WebApplicationAuthenticationExperiment.Google.SubscriberGoogleAuthenticationHandler`)
to use an injected `WebApplicationAuthenticationExperiment.Subscriber.ISubscriberAuthenticationHandler` implementation
which could, for example, use the authenticating user's **external unique id** to determine (e.g. by checking a subscriber db table) if the user 
 is a subscriber before considering the authentication successful.
- Includes a base hub class `WebApplicationAuthenticationExperiment.Subscriber.SubscriberHub` that obtains the connecting user's **external unique
id** which could be used, for example, to determine (e.g. by querying a tenant db) the user's tenant then add the connection to the signalr/websocket group for the tenant so
messages can be sent to just connected tenant users (e.g. to update only the tenant's connected users' alarm display).
- React Authenticator and Listener are both implemented as Typscript classes so they can be easily reused in other Typescript applications.
- Implementation of React `useEffect` in `App.tsx` correctly deals with signed in and signed out state.
- Shows how to configure Vite to proxy api requests to the back end without hardcoding endpoint urls by using an Aspire
`AppHost` [mechanism](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview#service-endpoint-environment-variable-format) that injects environment variables with information about another service into services that 
reference it with `WithReference()`

## Prerequisites

1. Google authentication requires setup with google as documented [here](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-9.0#create-the-google-oauth-20-client-id-and-secret). Note that **both** of the following must be added in Google's **Authorized redirect URIs** section for this example to work.
It appears the second is needed due to a bug/poor design in Microsoft's Google implementation (tell me if [this disaster](https://github.com/dotnet/aspnetcore/issues/58855)
makes any sense!). Take note of the `clientId` and `clientSecret` for the **Setup** step that follows.

- https://localhost:7080/api/google-response
- https://localhost:7080/signin-google

2. .NET 9 SDK 

## Setup

Fill in the values marked [FILL IN] in `WebApplicationAuthenticationExperiment/appsettings.Development.json`:

- `Jwt:secretKey` with a 15+ character self-generated secret string.
- `Google:clientId` and `Google:clientSecret` obtained from set with Google as identified in **Prerequisites** #1 immediately above.

```  "Authentication": {
    "Jwt": {
      "issuer": "compass-point",
      "audience": "iot-app",
      "secretKey": "[FILL IN]"
    },
    "Google": {
      "clientId": "[FILL IN]",
      "clientSecret": "[FILL IN]"
    }
  }
```