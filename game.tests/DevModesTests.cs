using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Stillwake.Game.Dev;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The dev arguments' registry (<see cref="DevModes"/>, and the scenarios' <c>[Dev]</c>): every one parses in its shape, and it
/// names exactly what the game reads.
/// </summary>
public class DevModesTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, ".."));

    private static IEnumerable<string> Sources(params string[] dirs) => dirs
        .SelectMany(d => Directory.EnumerateFiles(Path.Combine(Root, d), "*.cs", SearchOption.AllDirectories))
        .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or ".godot"));

    /// <summary>
    /// The scenarios' arguments, read from their <c>[Dev("--name", "doc", Shape = …)]</c>: game.tests compiles only <c>logic/</c>,
    /// so it cannot reflect over the scenarios as the game does (<c>DevScenarios.Modes</c>).
    /// </summary>
    private static readonly IReadOnlyList<DevMode> Scenarios = Sources("game")
        .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\[Dev\(""(--[^""]*)"",\s*""([^""]*)""(?:,\s*Shape\s*=\s*DevShape\.(\w+))?"))
        .Select(m => new DevMode(m.Groups[1].Value, m.Groups[3].Success ? Enum.Parse<DevShape>(m.Groups[3].Value) : DevShape.Flag, m.Groups[2].Value))
        .ToList();

    private static IEnumerable<DevMode> Every => DevModes.All.Concat(Scenarios);

    /// <summary>One way to write <paramref name="mode"/>, with "7" for its value.</summary>
    private static string[] Written(DevMode mode) => mode.Shape switch
    {
        DevShape.Flag => [mode.Name],
        DevShape.Next => [mode.Name, "7"],
        _ => [mode.Name + "=7"],
    };

    [Fact]
    public void FindsTheScenariosArguments() => Assert.Contains(Scenarios, m => m.Name == "--ground" && m.Shape == DevShape.Equals);

    [Fact]
    public void EveryArgumentParsesInItsShapeAndReadsBack()
    {
        foreach (var mode in Every)
        {
            var args = Written(mode);
            Assert.True(DevArgs.Unknown(args, Scenarios).Count == 0, $"{mode.Name} does not parse as {mode.Shape}");
            var dev = new DevArgs(args, Scenarios);
            Assert.True(dev.Gives(mode), mode.Name);
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
                    Assert.Equal("", new DevArgs([mode.Name], Scenarios).Given(mode.Name));
                    Assert.Empty(DevArgs.Unknown([mode.Name], Scenarios));
                    break;
            }
            Assert.False(new DevArgs([], Scenarios).Gives(mode), mode.Name);
        }
    }

    [Fact]
    public void ACombinedRunParsesWholeAndAStrayOneIsCaught()
    {
        Assert.Empty(DevArgs.Unknown(["--testbed", "--card=door", "--pick", "--zoom", "3", "--layer-demo", "focus"], Scenarios));
        Assert.Equal(["--no-such", "--zoom=3", "--testbed=1"], DevArgs.Unknown(["--no-such", "--zoom=3", "--testbed=1"], Scenarios));
        Assert.Equal(["--zoom"], DevArgs.Unknown(["--zoom"]));
        Assert.Throws<InvalidOperationException>(() => new DevArgs([], Scenarios).Has("--no-such"));
        // A scenario's argument is known only with the scenarios handed in, as Main hands them.
        Assert.Throws<InvalidOperationException>(() => new DevArgs([]).Has("--testbed"));
    }

    [Fact]
    public void NamesAreUniqueAndEachSaysWhatItDoes()
    {
        var every = Every.ToList();
        Assert.Equal(every.Count, every.Select(m => m.Name).Distinct().Count());
        Assert.All(every, m => Assert.Matches("^--[a-z][a-z0-9-]*$", m.Name));
        Assert.All(every, m => Assert.False(string.IsNullOrWhiteSpace(m.Doc), m.Name));
    }

    /// <summary>The game and the net read every argument they name through the registry: none unlisted, none listed and unread.</summary>
    [Fact]
    public void ListsExactlyTheArgumentsTheCodeReads()
    {
        var literal = new Regex(@"""(--[a-z][a-z0-9-]*)");
        var files = Sources("game", Path.Combine("net", "Net")).Where(f => Path.GetFileName(f) != "DevModes.cs");
        var read = files.SelectMany(f => literal.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value)).ToHashSet();
        var listed = Every.Select(m => m.Name).ToHashSet();
        Assert.True(read.Except(listed).Order().ToList() is { Count: 0 }, "read but not registered: " + string.Join(", ", read.Except(listed).Order()));
        Assert.True(listed.Except(read).Order().ToList() is { Count: 0 }, "registered but read nowhere: " + string.Join(", ", listed.Except(read).Order()));
    }
}
