using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Every sound shipped is credited as CC0, every cue's files are there, and every bus named exists (ADR 0033).</summary>
public class SoundTests
{
    private static readonly string[] AudioExtensions = [".wav", ".ogg", ".mp3", ".flac", ".opus"];

    private static string GameFolder([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "game"));

    private static string Audio => Path.Combine(GameFolder(), "audio");

    /// <summary>The team's own music made on Suno's free plan (0035): fine for playtests, replaced before any sale.</summary>
    private const string Placeholder = "Suno Basic, non-commercial: replace before sale";

    /// <summary>Each file named in CREDITS.md's table, by its path under <c>game/audio</c>, with its licence.</summary>
    private static Dictionary<string, string> Credited() =>
        File.ReadAllLines(Path.Combine(Audio, "CREDITS.md"))
            .Where(line => line.StartsWith('|'))
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries)[1..^1])
            .Where(cells => cells.Length == 4 && cells[0].StartsWith('`'))
            .ToDictionary(cells => cells[0].Trim('`'), cells => cells[3]);

    [Fact]
    public void EveryFileUnderAudioIsCreditedAsCc0OrAMarkedPlaceholder()
    {
        var credited = Credited();
        var files = Directory.EnumerateFiles(Audio, "*", SearchOption.AllDirectories)
            .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => Path.GetRelativePath(Audio, f).Replace('\\', '/'))
            .ToList();
        Assert.NotEmpty(files);
        foreach (string file in files)
        {
            Assert.True(credited.TryGetValue(file, out string? licence), $"{file} has no line in CREDITS.md");
            Assert.True(licence is "CC0" or Placeholder, $"{file} is {licence}, neither CC0 nor a marked placeholder");
        }
    }

    [Fact]
    public void EveryCueHasItsFiles()
    {
        foreach (var cue in Sound.All)
        {
            Assert.NotEmpty(cue.Files);
            foreach (string file in cue.Files)
            {
                Assert.StartsWith("res://", file);
                Assert.True(File.Exists(Path.Combine(GameFolder(), file["res://".Length..])), $"{file} is missing");
            }
        }
    }

    [Fact]
    public void EveryBusNamedIsInTheLayout()
    {
        string layout = File.ReadAllText(Path.Combine(GameFolder(), "default_bus_layout.tres"));
        // Master is bus 0, there in every layout without a line of its own.
        foreach (string bus in Sound.Buses.Where(b => b != Sound.Master))
            Assert.Contains($"name = &\"{bus}\"", layout);
    }
}
