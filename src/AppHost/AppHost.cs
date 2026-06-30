using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("../../keycloak/studio-realm.json");

var studioApi = builder.AddProject<Projects.Studio_Api>("studio-api")
    .WithHttpEndpoint(port: 5191)
    .WithHttpsEndpoint(port: 7109)
    .WithReference(keycloak)
    .WithEnvironment("Keycloak__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/studio"))
    .WithEnvironment("Keycloak__Audience", "studio-api")
    .WaitFor(keycloak);

var studioWeb = builder.AddProject<Projects.Studio_Web>("studio-web")
    .WithHttpEndpoint(port: 5058)
    .WithHttpsEndpoint(port: 7113)
    .WithReference(studioApi)
    .WaitFor(studioApi);

studioApi.WithEnvironment("Cors__AllowedOrigins",
    ReferenceExpression.Create($"{studioWeb.GetEndpoint("http")},{studioWeb.GetEndpoint("https")}"));

await builder.Build().RunAsync();
