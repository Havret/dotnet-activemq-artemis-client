# ArtemisNetClient.Extensions.DependencyInjection

`ArtemisNetClient.Extensions.DependencyInjection` integrates ArtemisNetClient with `Microsoft.Extensions.DependencyInjection`.

Use this package when you want to:

- register Artemis connections in `IServiceCollection`
- wire consumers and typed producers through dependency injection
- register request-reply clients for ASP.NET Core or other hosted applications

For ASP.NET Core and generic hosted apps, pair it with `ArtemisNetClient.Extensions.Hosting` so Artemis resources start automatically with the host.

## Packages

```bash
dotnet add package ArtemisNetClient.Extensions.DependencyInjection
dotnet add package ArtemisNetClient.Extensions.Hosting
```

## Start Here

- Guide: https://havret.github.io/dotnet-activemq-artemis-client/docs/dependency-injection
- Minimal sample: https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/samples/Testing/Application/Program.cs
- ASP.NET Core sample: https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/samples/ArtemisNetClient.Examples.AspNetCore/Startup.cs