using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The pixels a killed creature crumbles into (2026-10-29-kills-crumble): hanging, breaking off, flying, landing, heaping, fading.</summary>
public class PixelDustTests
{
    /// <summary>A floor whose top is row <see cref="FloorTop"/>, a wall at column <see cref="Wall"/> and right of it, space left of <see cref="SpaceLeft"/>.</summary>
    private sealed class Room(float gravity = 200) : PixelDust.IGround
    {
        public const int FloorTop = 40, Wall = 60, SpaceLeft = -100;
        public bool Solid(int x, int y) => x >= SpaceLeft && (y >= FloorTop || x >= Wall);
        public bool Afloat(int x, int y) => x < SpaceLeft;
        public float Gravity { get; } = gravity;
    }

    private static PixelDust.Mote At(float x, float y, float vx = 0, float vy = 0, float lasts = 10) =>
        new() { X = x, Y = y, Vx = vx, Vy = vy, Lasts = lasts, Colour = (200, 100, 50) };

    private static void Run(PixelDust dust, Room room, double seconds, double frame = 1 / 60.0)
    {
        for (double t = 0; t < seconds; t += frame) dust.Step(frame, room);
    }

    [Fact]
    public void APixelFallsAndLandsOnTheFloorStill()
    {
        var (dust, room) = (new PixelDust(100), new Room());
        dust.Add(At(10.5f, 5.5f), room);
        Run(dust, room, 2);
        var m = Assert.Single(dust.Motes);
        Assert.True(m.Landed);
        Assert.Equal((Room.FloorTop - 1, 0f, 0f), ((int)MathF.Floor(m.Y), m.Vx, m.Vy));
    }

    [Fact]
    public void PixelsLandingInOneColumnHeapOrSlideToALowerNeighbour()
    {
        var (dust, room) = (new PixelDust(100), new Room());
        for (int i = 0; i < 6; i++) dust.Add(At(20.5f, 10.5f - i * 4), room);
        Run(dust, room, 3);
        Assert.All(dust.Motes, m => Assert.True(m.Landed));
        // Never two in one pixel, never more than a few columns wide.
        Assert.Equal(6, dust.Motes.Select(m => ((int)m.X, (int)m.Y)).Distinct().Count());
        Assert.InRange(dust.Motes.Max(m => (int)m.X) - dust.Motes.Min(m => (int)m.X), 1, 2 * KillLooks.Slides + 1);
        Assert.Contains(dust.Motes, m => (int)m.Y < Room.FloorTop - 1);
    }

    [Fact]
    public void APixelBouncesOffAWallLosingSpeed()
    {
        var (dust, room) = (new PixelDust(100), new Room(gravity: 0));
        dust.Add(At(Room.Wall - 3.5f, 20.5f, vx: 100), room);
        Run(dust, room, 0.2);
        var m = Assert.Single(dust.Motes);
        Assert.True(m.X < Room.Wall);
        Assert.Equal(-100 * KillLooks.WallKeeps, m.Vx, 3);
    }

    [Fact]
    public void AfloatAPixelDriftsAsThrownAndFallsNot()
    {
        var (dust, room) = (new PixelDust(100), new Room());
        dust.Add(At(-200, 0, vx: 10, vy: -5), room);
        Run(dust, room, 1);
        var m = Assert.Single(dust.Motes);
        Assert.Equal((-190f, -5f, 10f, -5f), (m.X, m.Y, m.Vx, m.Vy), new Near());
    }

    [Fact]
    public void APixelFadesOverItsLastMomentsAndIsGoneItsHeapWithIt()
    {
        var (dust, room) = (new PixelDust(100), new Room());
        dust.Add(At(10.5f, 38.5f, lasts: 2), room);
        Run(dust, room, 2 - KillLooks.Fades / 2);
        Assert.InRange(Assert.Single(dust.Motes).Alpha, 0.3f, 0.7f);
        Run(dust, room, KillLooks.Fades);
        Assert.Empty(dust.Motes);
        // Its column free again: the next lands on the floor itself.
        dust.Add(At(10.5f, 30.5f), room);
        Run(dust, room, 1);
        Assert.Equal(Room.FloorTop - 1, (int)Assert.Single(dust.Motes).Y);
    }

    [Fact]
    public void AHangingPixelStaysPutAgelessTillItBreaksOffFlaringThenFalls()
    {
        var (dust, room) = (new PixelDust(100), new Room());
        dust.Add(At(10.5f, 5.5f, vx: 3) with { Wait = 1 }, room);
        Run(dust, room, 0.9);
        var hanging = Assert.Single(dust.Motes);
        Assert.Equal((10.5f, 5.5f, 0f, 0f), (hanging.X, hanging.Y, hanging.Age, hanging.Rim));
        Run(dust, room, 0.15);
        var broken = Assert.Single(dust.Motes);
        Assert.True(broken.Rim > 0);
        Assert.True(broken.Y > 5.5f && broken.X > 10.5f);
        Run(dust, room, 2);
        var landed = Assert.Single(dust.Motes);
        Assert.Equal((true, 0f), (landed.Landed, landed.Rim));
    }

    [Fact]
    public void ABodyCrumblesFromItsTopDownItsEdgeRaggedByAShare()
    {
        Assert.Equal(0, PixelDust.WaitOf(10, 10, 30, 0));
        Assert.Equal(KillLooks.CrumbleS / 2, PixelDust.WaitOf(20, 10, 30, 0), 3);
        Assert.Equal(KillLooks.CrumbleS, PixelDust.WaitOf(30, 10, 30, 0), 3);
        Assert.Equal(KillLooks.CrumbleS + KillLooks.Ragged, PixelDust.WaitOf(30, 10, 30, 1), 3);
        // A body one pixel tall breaks off at once.
        Assert.Equal(0, PixelDust.WaitOf(10, 10, 10, 0));
    }

    [Fact]
    public void NoMoreThanTheMostAndNoneInsideSomethingSolid()
    {
        var (dust, room) = (new PixelDust(3), new Room());
        dust.Add(At(10, Room.FloorTop + 2), room);
        Assert.Empty(dust.Motes);
        for (int i = 0; i < 5; i++) dust.Add(At(10 + i, 10), room);
        Assert.Equal(3, dust.Motes.Count);
    }

    /// <summary>Thrown any way at any speed and stepped by any frames, long ones too, a pixel never ends inside the floor or the wall.</summary>
    [Property(MaxTest = 300)]
    public Property NoPixelEverEndsInsideSomethingSolid()
    {
        var throws = from x in Gen.Choose(0, 59)
                     from y in Gen.Choose(0, 39)
                     from vx in Gen.Choose(-600, 600)
                     from vy in Gen.Choose(-600, 600)
                     from frame in Gen.Choose(5, 250)
                     select (x, y, vx, vy, frame);
        return Prop.ForAll(throws.ArrayOf().ToArbitrary(), all =>
        {
            var (dust, room) = (new PixelDust(1000), new Room());
            foreach (var (x, y, vx, vy, _) in all) dust.Add(At(x + 0.5f, y + 0.5f, vx, vy), room);
            foreach (var (_, _, _, _, frame) in all) dust.Step(frame / 1000.0, room);
            Run(dust, room, 1);
            Assert.All(dust.Motes, m => Assert.False(room.Solid((int)MathF.Floor(m.X), (int)MathF.Floor(m.Y)), $"{m.X},{m.Y}"));
        });
    }

    [Fact]
    public void ALandedStackSquashesThenHopsTwiceTheSecondLowerThenRests()
    {
        Assert.Equal((0f, 0f), FloorFeel.Landing(double.NaN));
        var (_, squashed) = FloorFeel.Landing(0);
        Assert.Equal((float)KillLooks.Squash, squashed, 3);
        double first = KillLooks.SquashMs + 0.3 * KillLooks.HopMs, second = KillLooks.SquashMs + 0.8 * KillLooks.HopMs;
        Assert.Equal(KillLooks.HopFirst, FloorFeel.Landing(first).Hop, 3);
        Assert.Equal(KillLooks.HopSecond, FloorFeel.Landing(second).Hop, 3);
        Assert.Equal((0f, 0f), FloorFeel.Landing(KillLooks.SquashMs + KillLooks.HopMs));
    }

    private sealed class Near : IEqualityComparer<(float, float, float, float)>
    {
        public bool Equals((float, float, float, float) a, (float, float, float, float) b) =>
            MathF.Abs(a.Item1 - b.Item1) < 0.5f && MathF.Abs(a.Item2 - b.Item2) < 0.5f && MathF.Abs(a.Item3 - b.Item3) < 0.01f && MathF.Abs(a.Item4 - b.Item4) < 0.01f;

        public int GetHashCode((float, float, float, float) obj) => 0;
    }
}
