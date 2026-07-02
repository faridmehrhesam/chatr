namespace Chatr.Studio.Api.Gitea;

public interface IGiteaClient
{
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
    Task EnsureOrgAsync(string orgName, CancellationToken ct = default);
    Task EnsureRepoAsync(string orgName, string repoName, CancellationToken ct = default);
    Task EnsureBranchAsync(string repo, string branch, string fromBranch, CancellationToken ct = default);
    Task<string[]> GetTreeAsync(string repo, string branch, CancellationToken ct = default);
    Task<string> GetFileAsync(string repo, string branch, string path, CancellationToken ct = default);
    Task PutFileAsync(string repo, string branch, string path, string content, string commitMessage, CancellationToken ct = default);
}
