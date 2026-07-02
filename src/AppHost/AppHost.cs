using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

var builder = DistributedApplication.CreateBuilder(args);

var giteaAdminUser = builder.Configuration["Gitea:AdminUser"]
    ?? throw new InvalidOperationException("Gitea:AdminUser is not configured. Set it via user-secrets.");
var giteaAdminPassword = builder.Configuration["Gitea:AdminPassword"]
    ?? throw new InvalidOperationException("Gitea:AdminPassword is not configured. Set it via user-secrets.");

var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("../../keycloak/studio-realm.json");

// GITEA_ADMIN_* env vars are not supported by the gitea/gitea image entrypoint.
// Instead we override CMD to start Gitea, wait for it, then create the admin via CLI.
var adminCreateCmd =
    $"su git -c \"gitea admin user create --username $GITEA_ADMIN_USER --password $GITEA_ADMIN_PASSWORD --email $GITEA_ADMIN_EMAIL --admin\" 2>/dev/null || true";

var giteaStartupScript =
    "su git -c '/app/gitea/gitea web' & " +
    "GITEA_PID=$! ; " +
    "RETRY=0 ; while [ $RETRY -lt 60 ] ; do " +
    "  wget -qO- http://localhost:3000/api/v1/version >/dev/null 2>&1 && break ; " +
    "  sleep 2 ; RETRY=$((RETRY+1)) ; " +
    "done ; " +
    adminCreateCmd + " ; " +
    "wait $GITEA_PID";

var gitea = builder.AddContainer("gitea", "gitea/gitea")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithEnvironment("GITEA__security__INSTALL_LOCK", "true")
    .WithEnvironment("GITEA__server__HTTP_PORT", "3000")
    .WithEnvironment("GITEA__database__DB_TYPE", "sqlite3")
    .WithEnvironment("GITEA_ADMIN_USER", giteaAdminUser)
    .WithEnvironment("GITEA_ADMIN_PASSWORD", giteaAdminPassword)
    .WithEnvironment("GITEA_ADMIN_EMAIL", "admin@gitea.local")
    .WithContainerRuntimeArgs("--tmpfs", "/data")
    .WithArgs("/bin/sh", "-c", giteaStartupScript);

var studioApi = builder.AddProject<Projects.Studio_Api>("studio-api")
    .WithHttpEndpoint(port: 5191)
    .WithHttpsEndpoint(port: 7109)
    .WithReference(keycloak)
    .WithReference(gitea.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/studio"))
    .WithEnvironment("Keycloak__Audience", "studio-api")
    .WithEnvironment("Gitea__AdminUser", giteaAdminUser)
    .WithEnvironment("Gitea__AdminPassword", giteaAdminPassword)
    .WaitFor(keycloak)
    .WaitFor(gitea);

var studioWeb = builder.AddProject<Projects.Studio_Web>("studio-web")
    .WithHttpEndpoint(port: 5058)
    .WithHttpsEndpoint(port: 7113)
    .WithReference(studioApi)
    .WaitFor(studioApi);

studioApi.WithEnvironment("Cors__AllowedOrigins",
    ReferenceExpression.Create($"{studioWeb.GetEndpoint("http")},{studioWeb.GetEndpoint("https")}"));

await builder.Build().RunAsync();
