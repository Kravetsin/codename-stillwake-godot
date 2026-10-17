using Stillwake.Game.Features.Atmosphere;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The air as drawn: the field easing cell by cell, and the motes kept to the air.</summary>
public class AirLookTests
{
    // A strip six wide: row 1's cells 1 to 4 are one room; the cell right of it, 11, its opening.
    private const int W = 6, H = 3, Cell = 16, Opening = 11;
    private static readonly int[] RoomOf = [-1, -1, -1, -1, -1, -1, -1, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1, -1];
    private static readonly Dictionary<int, IReadOnlyList<int>> Sealed = [], Open = new() { [0] = [Opening] };
    /// <summary>What is built: the room; the field keeps a cell round it, the whole strip.</summary>
    private static readonly Box Built = new(1, 1, 4, 1);

    private static List<int> Cells(Func<int, bool> aired) => Enumerable.Range(0, W * H).Where(c => RoomOf[c] >= 0 && aired(c)).ToList();

    private static AirField Field(Func<int, bool> aired, Dictionary<int, IReadOnlyList<int>>? venting = null)
    {
        var field = new AirField(W, H);
        field.Survey(RoomOf, Cells(aired), venting ?? Sealed, Built);
        return field;
    }

    [Fact]
    public void ShowsTheWorldAsItIsAtFirstThenEasesACellNewlyAiredInRatherThanAtOnce()
    {
        var field = Field(cell => cell == 7);
        Assert.Equal((1f, 0f), (field.Air(7), field.Air(8)));
        field.Survey(RoomOf, [7, 8], Sealed, Built);
        field.Ease(50);
        Assert.InRange(field.Air(8), 0.1f, 0.5f);
        Assert.False(field.Settled);
        for (int i = 0; i < 40; i++)
        {
            field.Ease(50);
            field.Write();
        }
        Assert.Equal(1f, field.Air(8));
        Assert.True(field.Settled);
    }

    [Fact]
    public void WritesNothingOutsideARoomAndTheFlowOfAVentingRoomTowardItsOpening()
    {
        var field = Field(_ => true, Open);
        field.Write();
        var texels = field.Texels;
        Assert.Equal(0, texels[0 * 4 + 3]);
        Assert.Equal(255, texels[7 * 4 + 3]);
        // The opening lies to the right of every cell of the room: the air runs at heading 0.
        Assert.Equal(0, field.FlowAt(7)!.Value, 5);
        Assert.Null(field.FlowAt(0));
        Assert.Null(Field(_ => true).FlowAt(7));
    }

    /// <summary>A still ship writes nothing again: the texture is uploaded only for cells that moved.</summary>
    [Fact]
    public void WritesNothingAgainWhileNothingMoves()
    {
        var field = Field(cell => cell == 7);
        Assert.True(field.Write());
        field.Survey(RoomOf, [7], Sealed, Built);
        field.Ease(16);
        Assert.False(field.Write());
        Assert.True(field.Settled);
        field.Survey(RoomOf, [7, 8], Sealed, Built);
        Assert.False(field.Settled);
        field.Ease(16);
        Assert.True(field.Write());
    }

    /// <summary>
    /// The field keeps what is built and a cell round it, the rest of the world space; built on, it grows
    /// and every cell keeps the air it showed.
    /// </summary>
    [Fact]
    public void KeepsWhatIsBuiltAndACellRoundItGrowingWithItAndKeepingEachCellsAir()
    {
        const int w = 12, h = 6;
        var roomOf = Enumerable.Repeat(-1, w * h).ToArray();
        int a = 2 * w + 2, b = 2 * w + 3, far = 2 * w + 8;
        roomOf[a] = roomOf[b] = a;
        var field = new AirField(w, h);
        field.Survey(roomOf, [a, b], Sealed, new Box(2, 2, 3, 2));
        Assert.Equal(new Box(1, 1, 4, 3), field.Region);
        field.Survey(roomOf, [a], Sealed, new Box(2, 2, 3, 2));
        field.Ease(50);
        float fading = field.Air(b);
        Assert.InRange(fading, 0.1f, 0.9f);
        roomOf[far] = far;
        field.Survey(roomOf, [a, far], Sealed, new Box(2, 2, 8, 2));
        Assert.Equal(new Box(1, 1, 9, 3), field.Region);
        Assert.Equal((1f, fading, 0f), (field.Air(a), field.Air(b), field.Air(far)));
        Assert.Equal(0f, field.Air(0));
        field.Write();
        // The texel of cell (8, 2), at (7, 1) in the region nine wide, is in a room.
        Assert.Equal(255, field.Texels[(1 * 9 + 7) * 4 + 3]);
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
        field.Survey(RoomOf, Cells(_ => true), Open, Built);
        bool out_ = false;
        for (int i = 0; i < 120 && !out_; i++)
        {
            motes.Step(16, field, [7, 8, 9, 10], [], null);
            out_ = motes.Live.Any(m => m.Out && m.X >= 5 * Cell);
        }
        Assert.True(out_);
    }

    [Fact]
    public void KeepsAFewMotesInThePodsReachAndLetsThemGoWithoutAPod()
    {
        var random = new Random(7);
        var motes = new AirMotes(Cell, random.NextDouble);
        var field = Field(_ => false);
        (double, double, double, double) reach = (0, 0, 96, 48);
        for (int i = 0; i < 600; i++) motes.Step(16, field, [], [], reach);
        Assert.InRange(motes.Live.Count, 1, AirMotes.InReach);
        Assert.All(motes.Live, m => Assert.True(m.X is >= 0 and <= 96 && m.Y is >= 0 and <= 48, $"{m.X}, {m.Y}"));
        for (int i = 0; i < 30; i++) motes.Step(16, field, [], [], null);
        Assert.Empty(motes.Live);
    }
}
