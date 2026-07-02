using System.Net.Http.Headers;
using System.Text;
using Chatr.Studio.Api.Gitea;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Chatr.Studio.Api.L2Test.Gitea;

public sealed class GiteaClientTests : IAsyncLifetime
{
    private static readonly string[] _createAdminCmd =
    [
        "su", "git", "-c",
        "gitea admin user create --username test-admin --password test-pass123! --email admin@test.local --admin"
    ];

    private IContainer _container = null!;
    private IGiteaClient _client = null!;
    private const string Repo = "test-org/test-repo";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("gitea/gitea:latest")
            .WithPortBinding(3000, true)
            .WithEnvironment("GITEA__security__INSTALL_LOCK", "true")
            .WithEnvironment("GITEA__server__HTTP_PORT", "3000")
            .WithEnvironment("GITEA__database__DB_TYPE", "sqlite3")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(3000))
            .WithReuse(true)
            .Build();

        await _container.StartAsync();

        var baseUrl = $"http://localhost:{_container.GetMappedPublicPort(3000)}";
        var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-admin:test-pass123!"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);

        _client = new GiteaClient(http);

        for (var i = 0; i < 30; i++)
        {
            if (await _client.IsHealthyAsync())
            {
                break;
            }

            await Task.Delay(1000);
        }

        await _container.ExecAsync(_createAdminCmd);

        await _client.EnsureOrgAsync("test-org");
        await _client.EnsureRepoAsync("test-org", "test-repo");
        await _client.EnsureBranchAsync(Repo, "draft", "main");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task PutFile_then_GetFile_roundtrip()
    {
        const string content = "CREATE TABLE T ( Id STRING )";

        await _client.PutFileAsync(Repo, "draft", "tables/t.chatr", content, "seed");
        var result = await _client.GetFileAsync(Repo, "draft", "tables/t.chatr");

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task PutFile_twice_updates_content()
    {
        await _client.PutFileAsync(Repo, "draft", "tables/u.chatr", "v1", "create");
        await _client.PutFileAsync(Repo, "draft", "tables/u.chatr", "v2", "update");
        var result = await _client.GetFileAsync(Repo, "draft", "tables/u.chatr");

        Assert.Equal("v2", result);
    }

    [Fact]
    public async Task GetTreeAsync_returns_committed_file()
    {
        await _client.PutFileAsync(Repo, "draft", "screens/home.chatr", "screen home", "seed");
        var paths = await _client.GetTreeAsync(Repo, "draft");

        Assert.Contains("screens/home.chatr", paths);
    }
}
