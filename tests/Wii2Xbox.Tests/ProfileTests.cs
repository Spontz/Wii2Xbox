using Xunit;

namespace Wii2Xbox.Tests;

public sealed class ProfileTests
{
    [Fact]
    public void ProfilesPersistIndependentlyAndInvalidReplacementPreservesPriorFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Wii2Xbox-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new ProfileStore(folder);
            var calibration = new InfraredCalibration(160, 620, 850, 110);
            store.Save("wii-a", new Profile { Name = "Juego A", InfraredCalibration = calibration });
            store.Save("wii-b", new Profile { Name = "Juego B", Horizontal = false });
            var invalid = new Profile { SchemaVersion = 2 };
            Assert.Throws<InvalidDataException>(() => store.Save("WII-A", invalid));
            var reopened = new ProfileStore(folder);
            Assert.Equal("Juego A", reopened.Read("WII-A").Name);
            Assert.Equal(calibration, reopened.Read("WII-A").InfraredCalibration);
            Assert.Equal("Juego B", reopened.Read("wii-b").Name);
            Assert.Null(reopened.Read("wii-b").InfraredCalibration);
            Assert.False(reopened.Read("wii-b").Horizontal);
            Assert.Equal(2, Directory.GetFiles(folder).Length);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
                Directory.Delete(folder);
            }
        }
    }

    [Theory]
    [InlineData("1", "A")]
    [InlineData("B", "Unknown")]
    [InlineData("A", null)]
    public void InvalidBindingsAreRejected(string source, string? target)
    {
        var profile = new Profile();
        profile.Bindings[source] = target!;
        Assert.Throws<InvalidDataException>(profile.Validate);
    }

    [Fact]
    public void ExplicitDpadBindingsReplaceRotationAndOppositeDirectionsCancel()
    {
        var profile = new Profile();
        profile.Bindings["Up"] = "RightTrigger";
        profile.Bindings["A"] = "Up";
        profile.Bindings["B"] = "Down";
        var map = new CoreInputMap(profile);
        var state = map.Map(WiiButtons.Up | WiiButtons.A | WiiButtons.B);
        Assert.Equal((byte)255, state.RightTrigger);
        Assert.Equal((ushort)0, state.Buttons);
    }

    [Fact]
    public void StableIdentityFallsBackToExistingHidProfile()
    {
        var root = Path.Combine(Path.GetTempPath(), "Wii2Xbox-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new ProfileStore(root);
            store.Save("old-hid-path", new Profile { Name = "Anterior" });
            Assert.Equal("Anterior", store.Read("bluetooth:AA11223344BB", "old-hid-path").Name);
            store.Save("bluetooth:AA11223344BB", new Profile { Name = "Persistente" });
            Assert.Equal("Persistente", store.Read("bluetooth:AA11223344BB", "other-path").Name);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.GetFiles(root)) File.Delete(file);
                Directory.Delete(root);
            }
        }
    }

    [Fact]
    public void SharedDestinationHoldsUntilAllSourcesAreReleased()
    {
        var profile = new Profile();
        profile.Bindings["One"] = profile.Bindings["Two"] = "A";
        var map = new CoreInputMap(profile);
        Assert.Equal((ushort)4096, map.Map(WiiButtons.One | WiiButtons.Two).Buttons);
        Assert.Equal((ushort)4096, map.Map(WiiButtons.One).Buttons);
        Assert.Equal(default, map.Map(WiiButtons.None));
    }
}
