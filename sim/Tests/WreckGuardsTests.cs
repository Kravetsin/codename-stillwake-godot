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
/// Guards aboard a wreck (derelict-guards): fewer than aboard an intact vessel (<c>WreckPer</c>), docked by the intact rule in any room the chain left the dock's box standing in, never the
/// arrival room; born awake on the floor at their open docks, hunting a hero aboard wherever he is.
/// </summary>
public class WreckGuardsTests
{
    private const int U = CreaturesModule.Unit, M = Content.Vessels.Margin;

    /// <summary>A class, a seed and a chain: none for the seed's own (every seed a wreck), else forced, one to four steps of any kind and age.</summary>
    private static Arbitrary<(VesselClass Class, uint Seed, WreckStep[]? Chain)> Wrecks()
    {
        var step = Gen.Zip(Gen.Elements(Enum.GetValues<WreckKind>()), ArbMap.Default.GeneratorFor<bool>()).Select(s => new WreckStep(s.Item1, s.Item2));
        var chain = Gen.Frequency((1, Gen.Constant<WreckStep[]?>(null)), (2, Gen.Choose(1, 4).SelectMany(n => step.ArrayOf(n)).Select(a => (WreckStep[]?)a)));
        return Gen.Zip(Gen.Elements(Content.Vessels.Classes.ToArray()), ArbMap.Default.GeneratorFor<uint>())
            .SelectMany(cs => chain.Select(c => (cs.Item1, cs.Item2, c))).ToArbitrary();
    }

    /// <summary>A wreck of <paramref name="cls"/> from <paramref name="seed"/>: its seed's own chain, every seed a wreck.</summary>
    private static Built Wreck(VesselClass cls, uint seed) => Shipyard.Build(cls, seed, null, 100);

    /// <summary>
    /// The cells of the way (every ladder piece, the two over each hatch), those with a back wall, those with a structure (a floor's) and
    /// those a wreck carved.
    /// </summary>
    private static (HashSet<(int, int)> Way, HashSet<(int, int)> Backed, HashSet<(int, int)> Built, HashSet<(int, int)> Carved) Read(Built b)
    {
        var way = new HashSet<(int, int)>();
        var backed = new HashSet<(int, int)>();
        var built = new HashSet<(int, int)>();
        foreach (var p in b.Parts)
        {
            if (p.Layer == "back") backed.Add((p.X, p.Y));
            else if (p.Kind == "ladder") way.UnionWith(Shipyard.Covers(p));
            else if (p.Kind == Builds.Hatch) way.UnionWith([(p.X, p.Y - 1), (p.X + 1, p.Y - 1)]);
            if (p.Layer == "structure") built.Add((p.X, p.Y));
        }
        int w = b.Vessel.Width;
        var carved = b.Wreck!.Events.SelectMany(e => e.Carved).Select(i => (i % w, i / w)).ToHashSet();
        return (way, backed, built, carved);
    }

    private static IEnumerable<(int X, int Y)> Box(int x, int y, int w, int h) =>
        Enumerable.Range(y, h).SelectMany(cy => Enumerable.Range(x, w).Select(cx => (cx, cy)));

    /// <summary>
    /// Whether <paramref name="rule"/>'s box stands whole on room <paramref name="c"/>'s floor at column <paramref name="x"/>: backed, not
    /// carved, off the way, the floor under it whole.
    /// </summary>
    private static bool Stands((HashSet<(int, int)> Way, HashSet<(int, int)> Backed, HashSet<(int, int)> Built, HashSet<(int, int)> Carved) r, Vessel.Compartment c, Aboard.Rule rule, int x) =>
        c.Floor - rule.Height >= c.Top && x >= c.Left && x + rule.Width <= c.Left + c.Width
        && Box(x, c.Floor - rule.Height, rule.Width, rule.Height).All(p => r.Backed.Contains(p) && !r.Carved.Contains(p) && !r.Way.Contains(p))
        && Box(x, c.Floor, rule.Width, 1).All(p => r.Built.Contains(p) && !r.Carved.Contains(p));

    /// <summary>The room <paramref name="room"/> was before a break cut it: its stern half's index for a bow half, else its own.</summary>
    private static int Origin(Vessel v, int room) => v.Compartments[room].Of is int of and >= 0 ? of : room;

    private static bool In(VesselPart p, Shipyard.Dock d) => Shipyard.Covers(p).Any(c => c.X >= d.X && c.X < d.X + d.Width && c.Y >= d.Y && c.Y < d.Y + d.Height);

    [Property(MaxTest = 150)]
    public Property AWreckDocksItsGuardsByItsOwnCountOnlyWhereTheirBoxesStandNeverTheArrivalRoom() => Prop.ForAll(Wrecks(), t =>
    {
        var b = t.Chain == null ? Wreck(t.Class, t.Seed) : Shipyard.Build(t.Class, t.Seed, t.Chain);
        if (b.Wreck == null) return;
        var v = b.Vessel;
        var docks = Shipyard.Guards(b);
        var read = Read(b);
        var (ax, ay) = Shipyard.Door(b);
        string at = $"{t.Class.Id} {t.Seed} {(t.Chain == null ? "drawn" : string.Join<WreckStep>(",", t.Chain))}";
        Assert.Equal(docks, Shipyard.Guards(b));
        Assert.Equal(docks.Count, docks.Select(d => Origin(v, d.Room)).Distinct().Count());
        Assert.InRange(docks.Count, 0, Spec.Max - 1);
        foreach (var rule in Aboard.Rules)
        {
            var sized = Enumerable.Range(0, v.Compartments.Count).Where(i => v.Compartments[i].Decks > 1 == rule.Big).ToList();
            int rooms = sized.Count(i => v.Compartments[i].Of < 0);
            int candidates = sized.Where(i => !v.Compartments[i].Holds(ax, ay)
                && Enumerable.Range(v.Compartments[i].Left, v.Compartments[i].Width).Any(x => Stands(read, v.Compartments[i], rule, x))).Select(i => Origin(v, i)).Distinct().Count();
            var mine = docks.Where(d => d.Rule == rule).ToList();
            Assert.True(Math.Min((rooms + rule.WreckPer - 1) / rule.WreckPer, candidates) == mine.Count, $"{rule.Species}: {mine.Count} of {rooms} rooms, {candidates} candidates on {at}");
            foreach (var d in mine)
            {
                var c = v.Compartments[d.Room];
                Assert.Contains(d.Room, sized);
                Assert.False(c.Holds(ax, ay), $"{d} in the arrival room on {at}");
                Assert.Equal(c.Floor, d.Y + d.Height);
                Assert.True(Stands(read, c, rule, d.X), $"{d} over what the wreck took on {at}");
            }
        }
        // What a capsule covers left out, the rest laid: what a stand stands over too.
        var laid = Shipyard.Docked(b.Parts, docks);
        var replacing = docks.Where(d => d.Rule.Crowded == Aboard.Crowded.Replace).ToList();
        Assert.Equal(b.Parts.Where(p => p.Layer == "back" || p.Kind == "wall" || !replacing.Exists(d => In(p, d))), laid);
    });

    /// <summary>A tanker wreck whose spider's dock stood over a hole in its room's floor: every dock now stands on a whole floor.</summary>
    [Theory]
    [InlineData("tanker", 87u)]
    public void ADockNeverStandsOverAHoleInItsFloor(string cls, uint seed)
    {
        var b = Wreck(Content.Vessels.Class(cls)!, seed);
        var read = Read(b);
        Assert.All(Shipyard.Guards(b), d => Assert.True(Stands(read, b.Vessel.Compartments[d.Room], d.Rule, d.X), $"{d} over a hole"));
    }

    /// <summary>Wrecks whose break cut a room in two, a dock drawn in each half: one guard to a room as it was, the other half passed over.</summary>
    [Theory]
    [InlineData("freighter", 139u)]
    [InlineData("tanker", 56u)]
    public void BothHalvesOfACutRoomNeverHoldADockEach(string cls, uint seed)
    {
        var b = Wreck(Content.Vessels.Class(cls)!, seed);
        var origins = Shipyard.Guards(b).Select(d => Origin(b.Vessel, d.Room)).ToList();
        Assert.Equal(origins.Distinct().Count(), origins.Count);
    }

    /// <summary>Docks stand in rooms a fire burnt, the cold froze and the air left: only the box's cells count, not what befell its room.</summary>
    [Fact]
    public void ABurntAFrozenAndAVentedRoomMayHoldADock()
    {
        bool burnt = false, frozen = false, vented = false;
        foreach (var cls in new[] { "freighter", "liner" }.Select(id => Content.Vessels.Class(id)!))
            for (uint seed = 1; seed <= 200 && !(burnt && frozen && vented); seed++)
            {
                var b = Wreck(cls, seed);
                var rooms = Shipyard.Guards(b).Select(d => d.Room).ToList();
                var events = b.Wreck!.Events;
                burnt |= events.Any(e => e.Kind == WreckKind.Fire && e.Compartments.Intersect(rooms).Any());
                frozen |= events.Any(e => e.Kind is WreckKind.Icing or WreckKind.Asteroid && e.Compartments.Intersect(rooms).Any());
                vented |= b.Wreck.Vented.Intersect(rooms).Any();
            }
        Assert.True(burnt && frozen && vented, $"burnt {burnt}, frozen {frozen}, vented {vented}");
    }

    /// <summary>A wreck of <paramref name="cls"/> with a dock of <paramref name="species"/> found and jumped to by the new game's hero.</summary>
    internal static (Scenario W, Registry V, Built Built) Boarded(string cls, string species)
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        for (int i = 0; i < 20; i++)
        {
            w.Do(new Find(cls, Wrecked: true)).Run();
            var contact = w.M.Places.Contacts[^1];
            var built = Shipyard.Build(Content.Vessels.Class(cls)!, contact.Seed, contact.Wreck);
            if (built.Wreck == null || !Shipyard.Guards(built).Any(d => d.Species == species)) continue;
            w.Do(new Jump(contact.Id)).Run();
            Assert.Empty(w.Refusals);
            return (w, w.On(w.World.SurfaceOf(0)), built);
        }
        throw new InvalidOperationException($"no {cls} wreck with a {species} dock");
    }

    [Theory]
    [InlineData("freighter", "spider")]
    [InlineData("liner", "escort")]
    public void AWreckOpensWithEveryGuardAwakeOnTheFloorOfItsDockAndNeverDocked(string cls, string species)
    {
        var (w, v, built) = Boarded(cls, species);
        var docks = Shipyard.Guards(built);
        Assert.Equal(docks, v.Creatures.Docks);
        var all = v.Creatures.Creatures.ToList();
        Assert.Equal(docks.Count, all.Count);
        for (int i = 0; i < docks.Count; i++)
        {
            var (c, d) = (all[i], docks[i]);
            Assert.Equal((d.Species, i, 1), (c.Species, CreaturesModule.DockOf(c), c.Recall(DockDrive.Woke)));
            // Born in the middle of its box, a tick on it is still over its dock: it hunts from its first.
            Assert.InRange(c.X, (M + d.X) * U, (M + d.X + d.Width) * U);
            Assert.Equal((M + d.Y + d.Height) * U, c.Y);
        }
        for (int tick = 0; tick < 200; tick++)
        {
            Assert.All(v.Creatures.Creatures, c => Assert.True(c.Posture is not (Posture.Docked or Posture.Waking), $"{c.Species} {c.Id} {c.Posture} at {tick}"));
            w.Run();
        }
    }

    /// <summary>
    /// Its hero aboard but out of its room, a wreck's guard goes at once: the escort hunts him, the spider hunts or patrols; passed out,
    /// none is aboard to hunt, and the escort stands while the spider patrols.
    /// </summary>
    [Theory]
    [InlineData("freighter", "spider", true)]
    [InlineData("liner", "escort", false)]
    public void AWrecksGuardWithNoHeroAboardStandsOrPatrolsAndGoesOutOfItsRoomForOne(string cls, string species, bool patrols)
    {
        var (w, v, _) = Boarded(cls, species);
        var guard = v.Creatures.Creatures.First(c => c.Species == species);
        var hero = v.Crew.HeroOf(0)!;
        Assert.False(v.Creatures.HeroInCabin(guard));
        hero.Out = 1000;
        w.Run(20);
        var (x, y) = (guard.X, guard.Y);
        w.Run(60);
        Assert.Equal(patrols, (x, y) != (guard.X, guard.Y));
        (x, y) = (guard.X, guard.Y);
        hero.Out = 0;
        w.Run(60);
        Assert.False(v.Creatures.HeroInCabin(guard));
        Assert.NotEqual((x, y), (guard.X, guard.Y));
        Assert.Contains(guard, v.Creatures.Creatures);
    }

    /// <summary>
    /// A wreck's guards in the intact rule's tests' stead: every machine the wreck left laid where it stands but what a capsule covers.
    /// </summary>
    [Theory]
    [InlineData("freighter", false)]
    [InlineData("liner", false)]
    [InlineData("freighter", true)]
    public void AWreckLaysEveryMachineItLeftButThoseUnderACapsule(string cls, bool forced)
    {
        var c = Content.Vessels.Class(cls)!;
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(forced ? new Find(cls, [new(WreckKind.Icing, false)]) : new Find(cls, Wrecked: true)).Run();
        var contact = w.M.Places.Contacts[^1];
        w.Do(new Jump(contact.Id)).Run();
        Assert.Empty(w.Refusals);
        var v = w.On(w.World.SurfaceOf(0));
        var built = Shipyard.Build(c, contact.Seed, contact.Wreck);
        Assert.NotNull(built.Wreck);
        var docks = Shipyard.Guards(built);
        Assert.Equal(docks, v.Creatures.Docks);
        Assert.Equal(docks.Count, v.Creatures.Creatures.Count);
        var capsules = docks.Where(d => d.Rule.Crowded == Aboard.Crowded.Replace).ToList();
        var machines = built.Parts.Where(p => p.Layer == "machine").ToList();
        Assert.NotEmpty(machines);
        Assert.All(machines, p =>
        {
            int cell = (M + p.Y) * v.Grid.Width + M + p.X;
            if (capsules.Exists(d => In(p, d))) Assert.Null(v.Grid.MachineAt(cell));
            else Assert.Equal(p.Kind, v.Grid.MachineAt(cell)?.Kind);
        });
    }

    /// <summary>
    /// Two worlds fed the same commands aboard a wrecked liner, alike every tick while its guards hunt their hero from the start; then a peer
    /// comes aboard and a third world loads the first's save, played on beside it.
    /// </summary>
    [Fact]
    public void TwoWorldsStayAlikeAboardAWreckedLinerThroughAHuntAndAJoin()
    {
        var (a, b) = (new Registry(), new Registry());
        var first = World.Create(1, Registry.Wreck, a.All);
        var second = World.Create(1, Registry.Wreck, b.All);
        void Both(params Command[] commands)
        {
            first.Step(commands);
            second.Step(commands);
            Assert.Equal(first.Hash(), second.Hash());
        }
        Both(new Enter(a.Crew.Spawn));
        Both(new Find("liner", Wrecked: true));
        var contact = a.Places.Contacts[^1];
        Both(new Jump(contact.Id));
        int surface = first.SurfaceOf(0);
        var v = Registry.On(first, surface);
        Assert.NotEmpty(v.Creatures.Creatures);
        var start = v.Creatures.Creatures.Select(c => (c.X, c.Y)).ToList();
        for (int i = 0; i < 120; i++) Both();
        Both(new Enter(a.Crew.Spawn) { From = 1 });
        Both(new Transfer(surface) { From = 1 });
        for (int i = 0; i < 60; i++) Both();
        var joined = World.Load(first.Save(), new Registry().All);
        Assert.Equal(first.Hash(), joined.Hash());
        for (int i = 0; i < 300; i++)
        {
            first.Step([]);
            joined.Step([]);
            Assert.Equal(first.Hash(), joined.Hash());
        }
        var guards = Registry.On(first, surface).Creatures.Creatures;
        Assert.True(guards.Any(c => !start.Contains((c.X, c.Y))), "no guard hunted");
        Assert.All(guards, c => Assert.Equal(1, c.Recall(DockDrive.Woke)));
    }
}
