using System.Runtime.CompilerServices;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Every machine has its picture: the grid draws <c>machine_&lt;kind&gt;</c>, and a missing one throws on the first frame it is seen.</summary>
public class MachineArtTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    private static readonly string Art = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game", "art"));

    [Fact]
    public void EveryMachineHasItsPicture() =>
        Assert.All(Spec.Machines.All, m => Assert.True(File.Exists(Path.Combine(Art, $"machine_{m.Kind}.png")), $"game/art/machine_{m.Kind}.png"));
}
