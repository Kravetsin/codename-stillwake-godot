using Stillwake.Game.Features.Atmosphere;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air as drawn: the field easing cell by cell, and the motes kept to the air.</summary>
public class AirLookTests
{
    // A strip six wide: row 1's cells 1 to 4 are one room; the cell right of it, 11, its opening.
    private const int W = 6, H = 3, Cell = 16, Opening = 11;
    private static readonly int[] RoomOf = [-1, -1, -1, -1, -1, -1, -1, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1, -1];
    private static readonly Dictionary<int, IReadOnlyList<int>> Sealed = [], Open = new() { [0] = [Opening] };

    private static AirField Field(Func<int, bool> aired, Dictionary<int, IReadOnlyList<int>>? venting = null)
    {
        var field = new AirField(W, H);
        field.Survey(RoomOf, aired, venting ?? Sealed);
        return field;
    }

    [Fact]
    public void ShowsTheWorldAsItIsAtFirstThenEasesACellNewlyAiredInRatherThanAtOnce()
    {
        var field = Field(cell => cell == 7);
        Assert.Equal((1f, 0f), (field.Air(7), field.Air(8)));
        field.Survey(RoomOf, cell => cell is 7 or 8, Sealed);
        field.Ease(50);
        Assert.InRange(field.Air(8), 0.1f, 0.5f);
        Assert.False(field.Settled);
        for (int i = 0; i < 40; i++) field.Ease(50);
        Assert.Equal(1f, field.Air(8));
        Assert.True(field.Settled);
    }

    [Fact]
    public void WritesNothingOutsideARoomAndTheFlowOfAVentingRoomTowardItsOpening()
    {
        var field = Field(_ => true, Open);
        var texels = new byte[W * H * 4];
        field.Write(texels);
        Assert.Equal(0, texels[0 * 4 + 3]);
        Assert.Equal(255, texels[7 * 4 + 3]);
        // The opening lies to the right of every cell of the room: the air runs at heading 0.
        Assert.Equal(0, field.FlowAt(7)!.Value, 5);
        Assert.Null(field.FlowAt(0));
        Assert.Null(Field(_ => true).FlowAt(7));
    }

    [Fact]
    public void KeepsARoomsMotesInItsAirAndNeverMoreThanItsShare()
    {
        var random = new Random(3);
        var motes = new AirMotes(Cell, random.NextDouble);
        var field = Field(cell => cell is 7 or 8);
        for (int i = 0; i < 300; i++) motes.Step(16, field, [7, 8], [], null);
        Assert.NotEmpty(motes.Live);
        Assert.True(motes.Live.Count <= Math.Ceiling(2 * AirMotes.PerCell) + 1);
        foreach (var m in motes.Live)
            Assert.Contains((int)(m.Y / Cell) * W + (int)(m.X / Cell), new[] { 7, 8 });
    }

    [Fact]
    public void SweepsAVentingRoomsMotesOutThroughItsOpening()
    {
        var random = new Random(5);
        var motes = new AirMotes(Cell, random.NextDouble);
        var field = Field(_ => true);
        for (int i = 0; i < 120; i++) motes.Step(16, field, [7, 8, 9, 10], [], null);
        field.Survey(RoomOf, _ => true, Open);
        bool out_ = false;
        for (int i = 0; i < 120 && !out_; i++)
        {
            motes.Step(16, field, [7, 8, 9, 10], [], null);
            out_ = motes.Live.Any(m => m.Out && m.X >= 5 * Cell);
        }
        Assert.True(out_);
    }

    [Fact]
    public void StreamsFromThePodsValveToASuitItFillsAndStopsWithIt()
    {
        var random = new Random(7);
        var motes = new AirMotes(Cell, random.NextDouble);
        var field = Field(_ => false);
        var pod = new PodAir((0, 0, 96, 48), (10, 30), [(60, 30)]);
        double nearest = double.MaxValue;
        for (int i = 0; i < 120; i++)
        {
            motes.Step(16, field, [], [], pod);
            foreach (var m in motes.Live.Where(m => m.Kind == MoteKind.Feed)) nearest = Math.Min(nearest, Math.Abs(m.X - 60) + Math.Abs(m.Y - 30));
        }
        Assert.True(nearest < 6, $"{nearest}");
        for (int i = 0; i < 20; i++) motes.Step(16, field, [], [], pod with { Feeds = [] });
        Assert.DoesNotContain(motes.Live, m => m.Kind == MoteKind.Feed);
        Assert.Contains(motes.Live, m => m.Kind == MoteKind.Pod);
    }
}
