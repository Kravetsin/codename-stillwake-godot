using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Xunit.Abstractions;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>The dark inside an abandoned vessel (derelict-looks): lamps, the one dying, screens, door dots, jammed doors.</summary>
public class VesselLightsTests(ITestOutputHelper output)
{
    private static Vessel Of(string cls, uint seed) => Shipyard.Describe(V.Class(cls)!, seed);

    private static IEnumerable<(string Cls, uint Seed)> Many() =>
        from cls in (string[])["freighter", "shuttle"] from seed in Enumerable.Range(1, 30) select (cls, (uint)seed);

    [Fact]
    public void OneSeedIsLitAsPinned()
    {
        var lights = new VesselLights(Of("freighter", 1));
        output.WriteLine(string.Concat(lights.Consoles.Select(c => c.Lit ? c.Glitching ? 'G' : 'L' : '-')));
        var dying = lights.Lamps[lights.Dying!.Value];
        Assert.Equal((LampState.Dying, 88, 27), (dying.State, dying.X, dying.Y));
        Assert.Equal("rrr...r.rr.r.yr.", string.Concat(lights.Lamps.Select(l => l.State switch { LampState.Red => 'r', LampState.Dying => 'y', _ => '.' })));
        Assert.All(lights.Lamps, l => Assert.Equal(V.Lamp, l.Kind));
        Assert.Equal("-L", string.Concat(lights.Consoles.Select(c => c.Lit ? c.Glitching ? 'G' : 'L' : '-')));
        Assert.Equal(["salvageConsole2", V.Console], lights.Consoles.Select(c => c.Kind));
    }

    [Fact]
    public void OneLinerIsLitAsPinnedItsChandeliersLampsAndItsAirlockFoundByItsRoom()
    {
        var v = Of("liner", 1);
        var lights = new VesselLights(v);
        string states = string.Concat(lights.Lamps.Select(l => l.State switch { LampState.Red => 'r', LampState.Dying => 'y', _ => '.' }));
        Assert.Equal("rrrrrrr...r.r...r....rr.rrrrrr....y...rrrrr.r....rr.r.rr..r..rr..r.r.r..rrr...r...", states);
        Assert.Equal(2, lights.Lamps.Count(l => l.Kind == V.Chandelier));
        Assert.True(Light.Lamps.ContainsKey(V.Chandelier));
        // The airlock is the room just inside the way in, its lamps red.
        var (sx, sy) = Shipyard.Door(v);
        var airlock = v.Compartments.Single(c => c.Holds(sx, sy));
        Assert.Contains(lights.Lamps, l => airlock.Holds(l.X, l.Y));
        Assert.All(lights.Lamps.Where(l => airlock.Holds(l.X, l.Y)), l => Assert.Equal(LampState.Red, l.State));
        var m = VesselDressTests.Laid(v);
        var chandelier = lights.Lamps.First(l => l.Kind == V.Chandelier);
        int cell = VesselLights.CellOf(chandelier.X, chandelier.Y, m.Grid.Width);
        m.Grid.Place(cell, new Placement("machine", V.Chandelier));
        Assert.True(VesselLights.Claims(m.Grid.MachineAt(cell)!, lights.LampCells(m.Grid.Width)));
    }

    [Fact]
    public void OneTankerIsLitAsPinnedItsAirlockUnderItsBridgeRed()
    {
        var v = Of("tanker", 1);
        var lights = new VesselLights(v);
        string states = string.Concat(lights.Lamps.Select(l => l.State switch { LampState.Red => 'r', LampState.Dying => 'y', _ => '.' }));
        output.WriteLine(states);
        // Re-recorded once its boom widened its frame (big-vessels stage 3), and once its hangar went and a second well came (big-vessels
        // fixes): a lamp's draw is by its cell in the frame.
        Assert.Equal("rrrrrrrrrrrrr...rr...r.r.r..rr.r.rr..rr.ry..r.r..r...", states);
        Assert.All(lights.Lamps, l => Assert.Equal(V.Lamp, l.Kind));
        var (sx, sy) = Shipyard.Door(v);
        var airlock = v.Compartments.Single(c => c.Holds(sx, sy));
        Assert.Contains(lights.Lamps, l => airlock.Holds(l.X, l.Y));
        Assert.All(lights.Lamps.Where(l => airlock.Holds(l.X, l.Y)), l => Assert.Equal(LampState.Red, l.State));
        // Every room lit by its lamps, the tall tank rooms too.
        Assert.All(v.Compartments, c => Assert.Contains(lights.Lamps, l => c.Holds(l.X, l.Y)));
    }

    [Fact]
    public void TheVesselsOwnLampsAreItsCeilingLampsClaimedFromThePowersLightAndBoltsAndNoOtherMachineIs()
    {
        var v = Of("freighter", 4);
        var m = VesselDressTests.Laid(v);
        var lights = new VesselLights(v);
        var cells = lights.LampCells(m.Grid.Width);
        foreach (var l in lights.Lamps) m.Grid.Place(VesselLights.CellOf(l.X, l.Y, m.Grid.Width), new Placement("machine", l.Kind));
        Assert.Equal(lights.Lamps.Count, m.Grid.Machines.Count(machine => VesselLights.Claims(machine, cells)));
        Assert.All(m.Grid.Machines, machine => Assert.Equal(machine.Kind == V.Lamp, VesselLights.Claims(machine, cells)));
        // One the ship's lamp, hung by a hero where a vessel lamp was, carries its bolt and its share of power.
        int first = VesselLights.CellOf(lights.Lamps[0].X, lights.Lamps[0].Y, m.Grid.Width);
        m.Grid.Clear(first, "machine");
        m.Grid.Place(first, new Placement("machine", "lamp"));
        Assert.False(VesselLights.Claims(m.Grid.MachineAt(first)!, cells));
        Assert.True(Light.Lamps.ContainsKey(V.Lamp));
    }

    [Fact]
    public void AVesselHasOneDyingLampAtMostAndItsAirlocksLampsBurnRed()
    {
        foreach (var (cls, seed) in Many())
        {
            var v = Of(cls, seed);
            var lights = new VesselLights(v);
            Assert.True(lights.Lamps.Count(l => l.State == LampState.Dying) <= 1, $"{cls} {seed}");
            var (sx, sy) = Shipyard.Door(v);
            var airlock = v.Compartments.Single(c => sx >= c.Left && sx < c.Left + c.Width && sy >= c.Top && sy < c.Top + V.Open);
            var inside = lights.Lamps.Where(l => l.X >= airlock.Left && l.X < airlock.Left + airlock.Width && l.Y >= airlock.Top && l.Y < airlock.Top + V.Open).ToList();
            Assert.NotEmpty(inside);
            Assert.All(inside, l => Assert.Equal(LampState.Red, l.State));
            // Every lamp but the airlock's could be the dying one: one is, while any is left.
            Assert.Equal(lights.Lamps.Count > inside.Count, lights.Dying != null);
        }
    }

    [Fact]
    public void ThreeConsolesInFourAreLitAndOneInFourOfThoseGlitches()
    {
        var consoles = Many().SelectMany(v => new VesselLights(Of(v.Cls, v.Seed)).Consoles).ToList();
        int lit = consoles.Count(c => c.Lit), glitching = consoles.Count(c => c.Glitching);
        Assert.All(consoles, c => Assert.True(c.Lit || !c.Glitching));
        Assert.InRange(lit * 100 / consoles.Count, 60, 90);
        Assert.InRange(glitching * 100 / lit, 10, 40);
        Assert.All(consoles, c => Assert.True(Light.ConsoleScreens.ContainsKey(c.Kind), c.Kind));
    }

    [Fact]
    public void ALampsLightIsGoneOnceItsMachineIsGone()
    {
        var v = Of("freighter", 4);
        var m = VesselDressTests.Laid(v);
        var lamp = new VesselLights(v).Lamps.First(l => l.State == LampState.Red);
        int cell = VesselLights.CellOf(lamp.X, lamp.Y, m.Grid.Width);
        Assert.False(VesselLights.Stands(m.Grid, lamp.Kind, cell));
        m.Grid.Place(cell, new Placement("machine", lamp.Kind));
        Assert.True(VesselLights.Stands(m.Grid, lamp.Kind, cell));
        m.Grid.Clear(cell, "machine");
        Assert.False(VesselLights.Stands(m.Grid, lamp.Kind, cell));
    }

    [Fact]
    public void ADoorsDotShowsOnlyWhileTheLiveGridSaysItIsShut()
    {
        var v = Of("freighter", 2);
        var m = VesselDressTests.Laid(v);
        var lights = new VesselLights(v);
        Assert.Equal(v.Doors.Select(d => (d.X, d.Y)).Order(), lights.Doors.Select(d => (d.X, d.Y)).Order());
        Assert.All(lights.Doors, d => Assert.Equal(V.Open, d.Rows));
        foreach (var door in lights.Doors)
        {
            bool stampedOpen = v.Doors.Single(d => (d.X, d.Y) == (door.X, door.Y)).Open;
            int cell = VesselLights.CellOf(door.X, door.Y, m.Grid.Width);
            // Laid as Places.Opened lays it: shut, or open as stamped.
            m.Grid.SetDoorway(cell, open: stampedOpen);
            Assert.Equal(!stampedOpen, VesselLights.Shut(m.Grid, door));
            m.Grid.SetDoorway(cell, open: !stampedOpen);
            Assert.Equal(stampedOpen, VesselLights.Shut(m.Grid, door));
        }
        Assert.Contains(v.Doors, d => d.Open);
        // A door taken away shows no dot.
        var gone = lights.Doors[0];
        foreach (var piece in Enumerable.Range(0, gone.Rows))
            for (int dx = 0; dx < 2; dx++) m.Grid.Clear(VesselLights.CellOf(gone.X + dx, gone.Y + piece, m.Grid.Width), "structure");
        Assert.False(VesselLights.Shut(m.Grid, gone));
    }

    [Fact]
    public void OnlyDoorsStampedOpenAreJammedAndTheSameInTwoRuns()
    {
        int jammed = 0, open = 0;
        foreach (var (cls, seed) in Many())
        {
            var v = Of(cls, seed);
            VesselDress a = new(v), b = new(Of(cls, seed));
            var doors = new VesselLights(v).Doors;
            for (int y = 0; y < v.Height; y++)
                for (int x = 0; x < v.Width; x++)
                {
                    int gx = x + V.Margin, gy = y + V.Margin;
                    Assert.Equal(a.Jammed(gx, gy), b.Jammed(gx, gy));
                    if (!a.Jammed(gx, gy)) continue;
                    var door = doors.Single(d => x >= d.X && x < d.X + 2 && y >= d.Y && y < d.Y + d.Rows);
                    Assert.True(v.Doors.Single(d => (d.X, d.Y) == (door.X, door.Y)).Open, $"{cls} {seed} ({x}, {y})");
                }
            foreach (var door in v.Doors.Where(d => d.Open))
            {
                open++;
                bool whole = Enumerable.Range(0, V.Open).All(dy => a.Jammed(door.X + V.Margin, door.Y + dy + V.Margin) && a.Jammed(door.X + 1 + V.Margin, door.Y + dy + V.Margin));
                bool none = Enumerable.Range(0, V.Open).All(dy => !a.Jammed(door.X + V.Margin, door.Y + dy + V.Margin) && !a.Jammed(door.X + 1 + V.Margin, door.Y + dy + V.Margin));
                Assert.True(whole || none, "a doorway is jammed whole or not at all");
                if (whole) jammed++;
            }
        }
        // About one in JamOneIn of the open ones.
        Assert.InRange(jammed, open / (2 * VesselLooks.JamOneIn), 2 * open / VesselLooks.JamOneIn);
    }
}
