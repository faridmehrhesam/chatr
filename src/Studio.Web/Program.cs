using Chatr.Studio.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOidcAuthentication(opts =>
    builder.Configuration.Bind("Oidc", opts.ProviderOptions));

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured.");
}

if (string.IsNullOrEmpty(builder.Configuration["Oidc:Authority"]))
{
    throw new InvalidOperationException("Oidc:Authority is not configured.");
}

if (string.IsNullOrEmpty(builder.Configuration["Oidc:ClientId"]))
{
    throw new InvalidOperationException("Oidc:ClientId is not configured.");
}

builder.Services.AddHttpClient("StudioApi", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp =>
        sp.GetRequiredService<AuthorizationMessageHandler>()
          .ConfigureHandler(authorizedUrls: [apiBaseUrl]));

await builder.Build().RunAsync();
