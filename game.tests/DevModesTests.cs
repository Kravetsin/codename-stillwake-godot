using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Stillwake.Game.Dev;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The dev arguments' registry (<see cref="DevModes"/>): every one parses in its shape, and it names exactly what the game reads.</summary>
public class DevModesTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    /// <summary>One way to write <paramref name="mode"/>, with "7" for its value.</summary>
    private static string[] Written(DevMode mode) => mode.Shape switch
    {
        DevShape.Flag => [mode.Name],
        DevShape.Next => [mode.Name, "7"],
        _ => [mode.Name + "=7"],
    };

    [Fact]
    public void EveryArgumentParsesInItsShapeAndReadsBack()
    {
        foreach (var mode in DevModes.All)
        {
            var args = Written(mode);
            Assert.True(DevArgs.Unknown(args).Count == 0, $"{mode.Name} does not parse as {mode.Shape}");
            var dev = new DevArgs(args);
            switch (mode.Shape)
            {
                case DevShape.Flag:
                    Assert.True(dev.Has(mode.Name), mode.Name);
                    break;
                case DevShape.Next:
                    Assert.Equal("7", dev.Arg(mode.Name));
                    break;
                case DevShape.Equals:
                    Assert.Equal("7", dev.Value(mode.Name));
                    break;
                case DevShape.Either:
                    Assert.Equal("7", dev.Given(mode.Name));
                    Assert.Equal("", new DevArgs([mode.Name]).Given(mode.Name));
                    Assert.Empty(DevArgs.Unknown([mode.Name]));
                    break;
            }
        }
    }

    [Fact]
    public void ACombinedRunParsesWholeAndAStrayOneIsCaught()
    {
        Assert.Empty(DevArgs.Unknown(["--testbed", "--card=door", "--pick", "--zoom", "3", "--layer-demo", "focus"]));
        Assert.Equal(["--no-such", "--zoom=3", "--testbed=1"], DevArgs.Unknown(["--no-such", "--zoom=3", "--testbed=1"]));
        Assert.Equal(["--zoom"], DevArgs.Unknown(["--zoom"]));
        Assert.Throws<InvalidOperationException>(() => new DevArgs([]).Has("--no-such"));
    }

    [Fact]
    public void NamesAreUniqueAndEachSaysWhatItDoes()
    {
        Assert.Equal(DevModes.All.Count, DevModes.All.Select(m => m.Name).Distinct().Count());
        Assert.All(DevModes.All, m => Assert.Matches("^--[a-z][a-z0-9-]*$", m.Name));
        Assert.All(DevModes.All, m => Assert.False(string.IsNullOrWhiteSpace(m.Doc), m.Name));
    }

    /// <summary>The game and the net read every argument they name through the registry: none unlisted, none listed and unread.</summary>
    [Fact]
    public void ListsExactlyTheArgumentsTheCodeReads()
    {
        var literal = new Regex(@"""(--[a-z][a-z0-9-]*)");
        var files = new[] { Path.Combine(Root, "game"), Path.Combine(Root, "net", "Net") }
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or ".godot"))
            .Where(f => Path.GetFileName(f) != "DevModes.cs");
        var read = files.SelectMany(f => literal.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value)).ToHashSet();
        var listed = DevModes.All.Select(m => m.Name).ToHashSet();
        Assert.True(read.Except(listed).Order().ToList() is { Count: 0 }, "read but not in DevModes.All: " + string.Join(", ", read.Except(listed).Order()));
        Assert.True(listed.Except(read).Order().ToList() is { Count: 0 }, "in DevModes.All but read nowhere: " + string.Join(", ", listed.Except(read).Order()));
    }
}
