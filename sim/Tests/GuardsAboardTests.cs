using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Places;
using Xunit;
using Aboard = Stillwake.Content.Vessels.Aboard;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Guards aboard an intact vessel (derelict-enemies, G-1): where <see cref="Shipyard.Guards"/> docks them, and the creatures module
/// spawning one asleep at each dock as the vessel world opens, kept there whatever the keep boxes say.
/// </summary>
public class GuardsAboardTests
{
    private static Arbitrary<(VesselClass Class, uint Seed)> Vessels() =>
        Gen.Zip(Gen.Elements(Content.Vessels.Classes.ToArray()), ArbMap.Default.GeneratorFor<uint>()).ToArbitrary();

    /// <summary>The rooms of <paramref name="rule"/>'s size in <paramref name="v"/>, by index.</summary>
    private static List<int> RoomsOf(Vessel v, Aboard.Rule rule) =>
        [.. Enumerable.Range(0, v.Compartments.Count).Where(i => v.Compartments[i].Decks > 1 == rule.Big)];

    /// <summary>The arrival room's index: the one a hero's feet stand in, just inside the way in.</summary>
    private static int Arrival(Vessel v)
    {
        var (x, y) = Shipyard.Door(v);
        return Enumerable.Range(0, v.Compartments.Count).Single(i => v.Compartments[i].Holds(x, y));
    }

    /// <summary>The cells the stamped vessel's structures and machines cover, and those with a back wall: what a dock is read against.</summary>
    private static (HashSet<(int, int)> Covered, HashSet<(int, int)> Backed) Stamped(Vessel v)
    {
        var covered = new HashSet<(int, int)>();
        var backed = new HashSet<(int, int)>();
        foreach (var p in Shipyard.Stamp(v))
            if (p.Layer == "back") backed.Add((p.X, p.Y));
            else covered.UnionWith(Shipyard.Covers(p));
        return (covered, backed);
    }

    /// <summary>Whether a box <paramref name="w"/> by <paramref name="h"/> from (<paramref name="x"/>, <paramref name="y"/>) is all back wall, nothing stamped before it.</summary>
    private static bool Clear((HashSet<(int, int)> Covered, HashSet<(int, int)> Backed) s, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                if (s.Covered.Contains((x + dx, y + dy)) || !s.Backed.Contains((x + dx, y + dy))) return false;
        return true;
    }

    /// <summary>Whether <paramref name="rule"/>'s box stands clear on room <paramref name="c"/>'s floor at some column.</summary>
    private static bool FitsSomewhere((HashSet<(int, int)>, HashSet<(int, int)>) s, Vessel.Compartment c, Aboard.Rule rule) =>
        c.Floor - rule.Height >= c.Top
        && Enumerable.Range(c.Left, Math.Max(0, c.Width - rule.Width + 1)).Any(x => Clear(s, x, c.Floor - rule.Height, rule.Width, rule.Height));

    [Property]
    public Property EachKindIsOnePerItsRoomsRoundedUpEachInARoomOfItsOwnClearOfFurnitureNeverTheArrivalRoom() => Prop.ForAll(Vessels(), cv =>
    {
        var v = Shipyard.Describe(cv.Class, cv.Seed);
        var docks = Shipyard.Guards(v);
        var stamped = Stamped(v);
        int arrival = Arrival(v);
        Assert.Equal(docks, docks.OrderBy(d => Aboard.Rules.ToList().FindIndex(r => r.Species == d.Species)).ThenBy(d => d.Room));
        Assert.Equal(docks.Count, docks.Select(d => d.Room).Distinct().Count());
        Assert.InRange(docks.Count, 0, Spec.Max - 1);
        foreach (var rule in Aboard.Rules)
        {
            var rooms = RoomsOf(v, rule);
            var mine = docks.Where(d => d.Species == rule.Species).ToList();
            int wanted = (rooms.Count + rule.Per - 1) / rule.Per;
            Assert.InRange(mine.Count, 0, wanted);
            foreach (var d in mine)
            {
                var c = v.Compartments[d.Room];
                Assert.Contains(d.Room, rooms);
                Assert.NotEqual(arrival, d.Room);
                Assert.Equal((rule.Width, rule.Height, c.Floor), (d.Width, d.Height, d.Y + d.Height));
                Assert.True(d.X >= c.Left && d.X + d.Width <= c.Left + c.Width && d.Y >= c.Top, $"{d} out of its room {c}");
                Assert.True(Clear(stamped, d.X, d.Y, d.Width, d.Height), $"{d} over something stamped in {cv.Class.Id} {cv.Seed}");
            }
            // Fewer than the rule asks only where no other room of the size has room for one.
            if (mine.Count < wanted)
                foreach (int i in rooms)
                    if (i != arrival && !mine.Exists(d => d.Room == i))
                        Assert.False(FitsSomewhere(stamped, v.Compartments[i], rule), $"{rule.Species} short in {cv.Class.Id} {cv.Seed}, room {i} has room");
        }
    });

    [Fact]
    public void TheSameVesselDocksItsGuardsAlikeAndSeedsDockThemApart()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        Assert.Equal(Shipyard.Guards(Shipyard.Describe(freighter, 5)), Shipyard.Guards(Shipyard.Describe(freighter, 5)));
        var placed = Enumerable.Range(1, 12).Select(s => string.Join(';', Shipyard.Guards(Shipyard.Describe(freighter, (uint)s)))).Distinct().Count();
        Assert.True(placed > 6, $"{placed} placings of 12");
    }

    /// <summary>The most guards a vessel of each class docks over seeds 1–200, pinned under the creatures' cap: they spawn before any other.</summary>
    [Fact]
    public void TheLargestClassesGuardsFitUnderTheCreaturesCap()
    {
        var most = Content.Vessels.Classes.ToDictionary(c => c.Id, c => Enumerable.Range(1, 200).Max(s => Shipyard.Guards(Shipyard.Describe(c, (uint)s)).Count));
        Assert.All(most, kv => Assert.True(kv.Value < Spec.Max, $"{kv.Key}: {kv.Value} guards"));
        Assert.True(most["liner"] >= most["freighter"], $"a liner {most["liner"]}, a freighter {most["freighter"]}");
    }

    /// <summary>A vessel of <paramref name="cls"/> found and jumped to by the new game's hero: the scenario, the vessel world and its vessel.</summary>
    private static (Scenario W, Registry V, Vessel Vessel) Boarded(string cls, uint index = 0)
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        for (uint i = 0; i <= index; i++) w.Do(new Find(cls)).Run();
        var contact = w.M.Places.Contacts[^1];
        w.Do(new Jump(contact.Id)).Run();
        Assert.Empty(w.Refusals);
        var v = w.On(w.World.SurfaceOf(0));
        return (w, v, Shipyard.Describe(Content.Vessels.Class(cls)!, contact.Seed));
    }

    [Theory]
    [InlineData("freighter")]
    [InlineData("liner")]
    public void AVesselWorldOpensWithAGuardAsleepOnTheFloorOfEachDockBeforeAnyOtherCreature(string cls)
    {
        var (w, v, vessel) = Boarded(cls);
        var docks = Shipyard.Guards(vessel);
        Assert.NotEmpty(docks);
        Assert.Equal(docks, v.Creatures.Docks);
        var all = v.Creatures.Creatures;
        Assert.Equal(docks.Count, all.Count);
        int m = Content.Vessels.Margin;
        for (int i = 0; i < docks.Count; i++)
        {
            var (c, d) = (all[i], docks[i]);
            Assert.Equal((d.Species, i, Posture.Docked), (c.Species, CreaturesModule.DockOf(c), c.Posture));
            Assert.Equal(all[0].Id + i, c.Id);
            // Its body inside its dock's box, its middle within half a cell of the box's, its feet on the room's floor: the vessel stands by now.
            int half = Spec.Get(d.Species).Body.Width * CreaturesModule.Unit / 2, u = CreaturesModule.Unit;
            Assert.InRange(c.X - half, (m + d.X) * u, (m + d.X + d.Width) * u - 2 * half);
            Assert.InRange(c.X, (m + d.X) * u + d.Width * u / 2 - u / 2, (m + d.X) * u + d.Width * u / 2 + u / 2);
            Assert.Equal((m + d.Y + d.Height) * u, c.Y);
            Assert.True(v.Grid.Solid((m + d.Y + d.Height) * v.Grid.Width + c.X / CreaturesModule.Unit), $"{c.Species} {c.Id} over no floor");
        }
        w.Run(40);
        Assert.Equal(docks.Count, v.Creatures.Creatures.Count(c => c.Posture == Posture.Docked));
    }

    [Fact]
    public void AGuardFarPastEveryKeepBoxStaysAsleepAtItsDock()
    {
        var (w, v, _) = Boarded("liner");
        var hero = v.Crew.HeroOf(0)!.Cell % v.Grid.Width;
        var far = v.Creatures.Creatures.MaxBy(c => Math.Abs(c.X / CreaturesModule.Unit - hero))!;
        Assert.True(Math.Abs(far.X / CreaturesModule.Unit - hero) > Spec.Hard.X, "no guard past the hard box");
        var (x, y) = (far.X, far.Y);
        w.Run(Spec.Linger + Spec.Beat);
        Assert.Contains(far, v.Creatures.Creatures);
        Assert.Equal((x, y, Posture.Docked), (far.X, far.Y, far.Posture));
    }

    [Fact]
    public void AGuardIsNeverLetOut()
    {
        var (w, v, _) = Boarded("freighter");
        int cell = v.Crew.HeroOf(0)!.Cell;
        foreach (var rule in Aboard.Rules)
        {
            w.Do(new Release(rule.Species, cell)).Run();
            Assert.Equal("creatures.notHere", w.Refusals[^1].Reason);
        }
    }
}
