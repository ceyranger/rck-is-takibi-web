using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class LastSaveMetadataServiceTests
{
    [Fact]
    public async Task Save_And_Load_Roundtrip_Last_Successful_Save_Time()
    {
        var root = CreateTempRoot();
        var metadataPath = Path.Combine(root, "last-save.json");

        try
        {
            var service = new LastSaveMetadataService(metadataPath);
            var expected = new DateTime(2026, 3, 28, 16, 45, 0);

            await service.SaveLastSuccessfulSaveAtAsync(expected);

            Assert.Equal(expected, service.LoadLastSuccessfulSaveAt());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Load_Returns_Null_For_Invalid_Metadata()
    {
        var root = CreateTempRoot();
        var metadataPath = Path.Combine(root, "last-save.json");

        try
        {
            await File.WriteAllTextAsync(metadataPath, "{ invalid json");
            var service = new LastSaveMetadataService(metadataPath);

            Assert.Null(service.LoadLastSuccessfulSaveAt());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectory(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
