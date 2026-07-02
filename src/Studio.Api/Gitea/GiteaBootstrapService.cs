namespace Chatr.Studio.Api.Gitea;

public sealed class GiteaBootstrapService(IGiteaClient gitea, ILogger<GiteaBootstrapService> logger)
    : IHostedService
{
    private const string Org = "team-dev";
    private const string Repo = "team-dev/my-app";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Waiting for Gitea to become ready...");
        var healthy = false;
        for (var i = 0; i < 60; i++)
        {
            if (await gitea.IsHealthyAsync(cancellationToken))
            {
                healthy = true;
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        if (!healthy)
        {
            logger.LogWarning("Gitea did not become healthy after 120 seconds. Bootstrap skipped.");
            return;
        }

        logger.LogInformation("Bootstrapping Gitea dev data...");

        try
        {
            await gitea.EnsureOrgAsync(Org);
            await gitea.EnsureRepoAsync(Org, "my-app");
            await gitea.EnsureBranchAsync(Repo, "draft", "main");

            await gitea.PutFileAsync(Repo, "draft", "tables/crm.chatr",
                "create table Customers (\n  Id string,\n  Name string\n)",
                "Seed: tables/crm.chatr");

            await gitea.PutFileAsync(Repo, "draft", "screens/home.chatr",
                "create table HomeScreen (\n  Title string\n)",
                "Seed: screens/home.chatr");

            logger.LogInformation("Gitea bootstrap complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gitea bootstrap failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
