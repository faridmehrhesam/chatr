using Chatr.Studio.Api.Packages;

namespace Chatr.Studio.Api.L0Test.Packages;

public class PackageRegistryTests
{
    private static readonly Guid DevId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void GetRepo_known_id_returns_repo_path()
    {
        var registry = new PackageRegistry();
        Assert.Equal("team-dev/my-app", registry.GetRepo(DevId));
    }

    [Fact]
    public void GetRepo_unknown_id_returns_null()
    {
        var registry = new PackageRegistry();
        Assert.Null(registry.GetRepo(Guid.NewGuid()));
    }
}
