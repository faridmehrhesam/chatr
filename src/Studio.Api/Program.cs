using System.Text;
using Chatr.ServiceDefaults;
using Chatr.Studio.Api.Gitea;
using Chatr.Studio.Api.Packages;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Keycloak:Authority"];
        opts.Audience = builder.Configuration["Keycloak:Audience"];
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IGiteaClient, GiteaClient>(client =>
{
    client.BaseAddress = new Uri("http://gitea");
    var user = builder.Configuration["Gitea:AdminUser"]!;
    var pass = builder.Configuration["Gitea:AdminPassword"]!;
    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
})
.AddServiceDiscovery();

builder.Services.AddHostedService<GiteaBootstrapService>();

builder.Services.AddSingleton<PackageRegistry>();

var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

await app.RunAsync();
