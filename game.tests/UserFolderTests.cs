using System;
using System.IO;
using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The worlds and settings kept under the game's old name move into the new folder once.</summary>
public sealed class UserFolderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "userfolder-" + Guid.NewGuid().ToString("N"));
    private string Old => Path.Combine(_root, UserFolder.OldName);
    private string New => Path.Combine(_root, "Castoff");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void MovesTheWorldsAndSettingsButLeavesWhatTheNewFolderHas()
    {
        Directory.CreateDirectory(Path.Combine(Old, "saves", "worlds", "w1"));
        File.WriteAllText(Path.Combine(Old, "saves", "worlds", "w1", "world.json"), "old world");
        File.WriteAllText(Path.Combine(Old, "settings.cfg"), "old settings");
        Directory.CreateDirectory(Path.Combine(Old, "logs"));
        File.WriteAllText(Path.Combine(Old, "logs", "godot.log"), "old log");
        Directory.CreateDirectory(Path.Combine(New, "logs"));
        File.WriteAllText(Path.Combine(New, "logs", "godot.log"), "new log");

        Assert.Equal(["saves", "settings.cfg"], UserFolder.Adopt(Old, New));

        Assert.Equal("old world", File.ReadAllText(Path.Combine(New, "saves", "worlds", "w1", "world.json")));
        Assert.Equal("old settings", File.ReadAllText(Path.Combine(New, "settings.cfg")));
        Assert.Equal("new log", File.ReadAllText(Path.Combine(New, "logs", "godot.log")));
        Assert.True(File.Exists(Path.Combine(Old, "logs", "godot.log")));
        Assert.False(Directory.Exists(Path.Combine(Old, "saves")));
    }

    [Fact]
    public void KeepsWhatTheNewFolderAlreadyHoldsAndRunsOnlyOnce()
    {
        Directory.CreateDirectory(Old);
        File.WriteAllText(Path.Combine(Old, "settings.cfg"), "old");
        Directory.CreateDirectory(New);
        File.WriteAllText(Path.Combine(New, "settings.cfg"), "new");

        Assert.Empty(UserFolder.Adopt(Old, New));
        Assert.Equal("new", File.ReadAllText(Path.Combine(New, "settings.cfg")));
        Assert.Empty(UserFolder.Adopt(Path.Combine(_root, "none"), New));
    }
}
