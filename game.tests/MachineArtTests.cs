using System.Buffers.Binary;
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

    /// <summary>One with an exhaust is drawn as much wider as its nozzle reaches out of its left side (the engine), its footprint's height.</summary>
    [Fact]
    public void AMachineWithAnExhaustIsDrawnItsNozzleWider()
    {
        var exhausting = Spec.Machines.All.Where(m => m.Exhaust != null).ToList();
        Assert.NotEmpty(exhausting);
        foreach (var m in exhausting)
        {
            // A PNG's width and height, big-endian, after its signature and the IHDR chunk's length and type.
            var head = File.ReadAllBytes(Path.Combine(Art, $"machine_{m.Kind}.png")).AsSpan(16, 8);
            var size = (BinaryPrimitives.ReadInt32BigEndian(head), BinaryPrimitives.ReadInt32BigEndian(head[4..]));
            Assert.Equal(((m.Width + m.Exhaust!.Value) * 8, m.Height * 8), size);
        }
    }
}
