namespace Chatr.Studio.Api.Packages;

public sealed class PackageRegistry
{
    private static readonly Guid DevPackageId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly Dictionary<Guid, string> _repos = new()
    {
        [DevPackageId] = "team-dev/my-app"
    };

    public string? GetRepo(Guid packageId)
    {
        return _repos.TryGetValue(packageId, out var repo) ? repo : null;
    }
}
