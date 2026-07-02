using System.Text.RegularExpressions;
using Chatr.Studio.Api.Gitea;
using Microsoft.AspNetCore.Mvc;

namespace Chatr.Studio.Api.Packages;

[ApiController]
[Route("packages/{id:guid}")]
public sealed class PackageFilesController(PackageRegistry registry, IGiteaClient gitea) : ControllerBase
{
    private const string DraftBranch = "draft";

    // Allows letters, digits, underscore, dot, hyphen, and forward-slash only.
    // Rejects traversal sequences (..), query/fragment chars, and percent-encoding.
    private static readonly Regex ValidPath =
        new(@"^[A-Za-z0-9_./-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static bool IsValidPath(string path)
    {
        return !string.IsNullOrEmpty(path) &&
               ValidPath.IsMatch(path) &&
               !path.Split('/').Any(seg => seg is ".." or "." or "");
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(Guid id)
    {
        var repo = registry.GetRepo(id);
        if (repo is null)
        {
            return NotFound();
        }

        try
        {
            var paths = await gitea.GetTreeAsync(repo, DraftBranch);
            return Ok(paths);
        }
        catch (GiteaNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("files/{*path}")]
    public async Task<IActionResult> GetFile(Guid id, string path)
    {
        if (!IsValidPath(path))
        {
            return BadRequest();
        }

        var repo = registry.GetRepo(id);
        if (repo is null)
        {
            return NotFound();
        }

        try
        {
            var content = await gitea.GetFileAsync(repo, DraftBranch, path);
            return Ok(new { content });
        }
        catch (GiteaNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("files/{*path}")]
    public async Task<IActionResult> PutFile(Guid id, string path, [FromBody] FileContentBody body)
    {
        if (!IsValidPath(path))
        {
            return BadRequest();
        }

        var repo = registry.GetRepo(id);
        if (repo is null)
        {
            return NotFound();
        }

        try
        {
            await gitea.PutFileAsync(repo, DraftBranch, path, body.Content,
                $"Studio: update {path}");
            return NoContent();
        }
        catch (GiteaNotFoundException)
        {
            return NotFound();
        }
    }

    public record FileContentBody(string Content);
}
