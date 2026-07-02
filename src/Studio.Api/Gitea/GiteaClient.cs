using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chatr.Studio.Api.Gitea;

public sealed class GiteaClient(HttpClient http) : IGiteaClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync("/api/v1/version", ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureOrgAsync(string orgName, CancellationToken ct = default)
    {
        var check = await http.GetAsync($"/api/v1/orgs/{orgName}", ct);
        if (check.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        var resp = await http.PostAsJsonAsync("/api/v1/orgs",
            new { username = orgName, visibility = "private" }, JsonOpts, ct);

        if (resp.StatusCode is not HttpStatusCode.Created and not HttpStatusCode.UnprocessableEntity)
        {
            resp.EnsureSuccessStatusCode();
        }
    }

    public async Task EnsureRepoAsync(string orgName, string repoName, CancellationToken ct = default)
    {
        var check = await http.GetAsync($"/api/v1/repos/{orgName}/{repoName}", ct);
        if (check.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        var resp = await http.PostAsJsonAsync($"/api/v1/orgs/{orgName}/repos",
            new { name = repoName, auto_init = true, default_branch = "main", @private = true }, JsonOpts, ct);

        if (resp.StatusCode is not HttpStatusCode.Created and not HttpStatusCode.UnprocessableEntity)
        {
            resp.EnsureSuccessStatusCode();
        }
    }

    public async Task EnsureBranchAsync(string repo, string branch, string fromBranch, CancellationToken ct = default)
    {
        var check = await http.GetAsync($"/api/v1/repos/{repo}/branches/{branch}", ct);
        if (check.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        var resp = await http.PostAsJsonAsync($"/api/v1/repos/{repo}/branches",
            new { new_branch_name = branch, old_branch_name = fromBranch }, JsonOpts, ct);

        if (resp.StatusCode is not HttpStatusCode.Created and not HttpStatusCode.UnprocessableEntity)
        {
            resp.EnsureSuccessStatusCode();
        }
    }

    public async Task<string[]> GetTreeAsync(string repo, string branch, CancellationToken ct = default)
    {
        var branchResp = await http.GetAsync($"/api/v1/repos/{repo}/branches/{branch}", ct);
        if (branchResp.StatusCode == HttpStatusCode.NotFound)
        {
            throw new GiteaNotFoundException($"Branch '{branch}' not found in '{repo}'");
        }

        branchResp.EnsureSuccessStatusCode();

        var branchInfo = await branchResp.Content.ReadFromJsonAsync<GiteaBranchInfo>(JsonOpts);
        var sha = branchInfo!.Commit.Id;

        var treeResp = await http.GetAsync($"/api/v1/repos/{repo}/git/trees/{sha}?recursive=true", ct);
        treeResp.EnsureSuccessStatusCode();

        var tree = await treeResp.Content.ReadFromJsonAsync<GiteaTreeResponse>(JsonOpts);
        return tree!.Tree
            .Where(e => e.Type == "blob")
            .Select(e => e.Path)
            .ToArray();
    }

    public async Task<string> GetFileAsync(string repo, string branch, string path, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/api/v1/repos/{repo}/contents/{path}?ref={branch}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            throw new GiteaNotFoundException($"File '{path}' not found in '{repo}@{branch}'");
        }

        resp.EnsureSuccessStatusCode();

        var file = await resp.Content.ReadFromJsonAsync<GiteaFileContent>(JsonOpts);
        return Encoding.UTF8.GetString(Convert.FromBase64String(file!.Content));
    }

    public async Task PutFileAsync(string repo, string branch, string path, string content, string commitMessage, CancellationToken ct = default)
    {
        string? sha = null;
        var getResp = await http.GetAsync($"/api/v1/repos/{repo}/contents/{path}?ref={branch}", ct);
        if (getResp.IsSuccessStatusCode)
        {
            var existing = await getResp.Content.ReadFromJsonAsync<GiteaFileContent>(JsonOpts);
            sha = existing?.Sha;
        }
        else if (getResp.StatusCode != HttpStatusCode.NotFound)
        {
            getResp.EnsureSuccessStatusCode();
        }

        var body = new GiteaPutRequest(
            commitMessage,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            sha,
            branch);

        var putResp = await http.PutAsJsonAsync($"/api/v1/repos/{repo}/contents/{path}", body, JsonOpts, ct);
        putResp.EnsureSuccessStatusCode();
    }

    private record GiteaBranchInfo(GiteaCommit Commit);
    private record GiteaCommit(string Id);
    private record GiteaTreeResponse(List<GiteaTreeEntry> Tree);
    private record GiteaTreeEntry(string Path, string Type);
    private record GiteaFileContent(string Content, string Sha);
    private record GiteaPutRequest(string Message, string Content, string? Sha, string Branch);
}
