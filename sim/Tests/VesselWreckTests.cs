using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Places;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>Wrecked vessels (vessel-wrecks, W-1): the chain's table, each event's geometry on the parts, the arrival room kept.</summary>
public class VesselWreckTests(ITestOutputHelper output)
{
    private static VesselClass C(string id) => Content.Vessels.Class(id)!;

    private static List<WreckStep> Chain(params WreckKind[] kinds) => [.. kinds.Select(k => new WreckStep(k))];

    private static List<WreckKind> Kinds(Built b) => b.Wreck?.Events.Select(e => e.Kind).ToList() ?? [];

    /// <summary>Parts as text, every value of each, what a crate holds too: what two buildings compare by.</summary>
    private static List<string> Keys(IEnumerable<VesselPart> parts) =>
        [.. parts.Select(p => $"{p.X},{p.Y},{p.Layer},{p.Kind},{p.Material},{p.Open},{string.Join("+", p.Holds ?? [])}")];

    private static bool Breaches(WreckKind k) => k is WreckKind.Explosion or WreckKind.Asteroid or WreckKind.Break;

    /// <summary>
    /// The rules of the plan's table that hold of any chain built: at most the class's count, the once-only events once, the asteroids'
    /// count, no break on a shuttle, icing first or on an unfrozen breach, groups and ages, a break's cause.
    /// </summary>
    private static void KeepsTheTable(VesselClass cls, Built b, bool drawn)
    {
        var events = b.Wreck!.Events;
        var kinds = events.Select(e => e.Kind).ToList();
        Assert.InRange(kinds.Count, 1, cls.Events.Max);
        foreach (var once in (WreckKind[])[WreckKind.Explosion, WreckKind.Icing, WreckKind.Break])
            Assert.True(kinds.Count(k => k == once) <= 1, $"{once} twice");
        bool big = cls.Layout != VesselLayout.Rooms;
        Assert.True(kinds.Count(k => k == WreckKind.Asteroid) <= (big ? Wrecks.RocksBig : Wrecks.Rocks));
        if (cls.Id == "shuttle") Assert.DoesNotContain(WreckKind.Break, kinds);
        for (int i = 0; i < kinds.Count; i++)
        {
            var before = kinds.Take(i).Where(Breaches).ToList();
            if (kinds[i] == WreckKind.Icing)
                Assert.True(i == 0 || before.Count > 0 && before[^1] != WreckKind.Asteroid, $"icing at {i} of {string.Join(",", kinds)}");
            if (kinds[i] == WreckKind.Break)
            {
                var causes = kinds.Take(i).Where(k => k is WreckKind.Explosion or WreckKind.Asteroid).ToList();
                Assert.Equal(causes.Count == 0 ? null : causes[^1], events[i].Cause);
            }
            bool joins = i > 0 && Shipyard.Follows(kinds[i - 1], kinds[i]);
            Assert.Equal(i == 0 ? 0 : events[i - 1].Group + (joins ? 0 : 1), events[i].Group);
            if (joins) Assert.Equal(events[i - 1].Old, events[i].Old);
            if (drawn && events[i].Group != events[^1].Group) Assert.True(events[i].Old, "an earlier group is old");
        }
        Assert.Equal(events[0].Old, b.Wreck.Scavenged);
    }

    /// <summary>
    /// The arrival room and the ring round it as the intact vessel stood, moved with its half: every part in it as it was, the spawn
    /// just inside the same door.
    /// </summary>
    private static void TheArrivalRoomStands(VesselClass cls, uint seed, Built b)
    {
        var intact = Shipyard.Build(cls, seed, []);
        var (ix, iy) = Shipyard.Door(intact);
        var (bx, by) = Shipyard.Door(b);
        int dx = bx - ix, dy = by - iy;
        var door = intact.Vessel.Doors[intact.Entry];
        Assert.Equal(door with { X = door.X + dx, Y = door.Y + dy }, b.Vessel.Doors[b.Entry]);
        var room = intact.Vessel.Compartments.First(c => c.Holds(ix, iy));
        int ring = Wrecks.ProtectRing;
        int x0 = room.Left - ring, y0 = room.Top - ring, x1 = room.Left + room.Width - 1 + ring, y1 = room.Floor + ring;
        bool Inside(VesselPart p, int sx, int sy) => Shipyard.Covers(p).All(c => c.X - sx >= x0 && c.X - sx <= x1 && c.Y - sy >= y0 && c.Y - sy <= y1);
        string Key(VesselPart p, int sx, int sy) => $"{p.X - sx},{p.Y - sy},{p.Layer},{p.Kind},{p.Material},{p.Open},{string.Join("+", p.Holds ?? [])}";
        var was = intact.Parts.Where(p => Inside(p, 0, 0)).Select(p => Key(p, 0, 0)).Order(StringComparer.Ordinal).ToList();
        var now = b.Parts.Where(p => Inside(p, dx, dy)).Select(p => Key(p, dx, dy)).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(was, now);
    }

    /// <summary>No two parts on one cell: two structures, two machines, or a machine on a structure.</summary>
    private static void NoTwoPartsOverlap(IReadOnlyList<VesselPart> parts)
    {
        var structures = new HashSet<(int, int)>();
        var machines = new HashSet<(int, int)>();
        foreach (var p in parts)
        {
            if (p.Layer == "back") continue;
            foreach (var c in Shipyard.Covers(p)) Assert.True((p.Layer == "machine" ? machines : structures).Add(c), $"{p} over another at {c}");
        }
        foreach (var p in parts.Where(p => p.Layer == "machine"))
            foreach (var c in Shipyard.Covers(p)) Assert.False(structures.Contains(c), $"{p} on a structure at {c}");
    }

    [Theory]
    [InlineData("shuttle")]
    [InlineData("freighter")]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void DrawnChainsOverFiveHundredSeedsKeepTheTableTheArrivalRoomAndNoOverlap(string id)
    {
        var cls = C(id);
        var counts = new int[cls.Events.Max + 1];
        var seen = new HashSet<WreckKind>();
        for (uint seed = 1; seed <= 500; seed++)
        {
            var b = Shipyard.Build(cls, seed, null, 100);
            Assert.NotNull(b.Wreck);
            counts[b.Wreck!.Events.Count]++;
            seen.UnionWith(Kinds(b));
            KeepsTheTable(cls, b, drawn: true);
            TheArrivalRoomStands(cls, seed, b);
            NoTwoPartsOverlap(b.Parts);
        }
        output.WriteLine($"{id}: chains by length {string.Join(", ", counts.Select((n, k) => $"{k}: {n}"))}; kinds {string.Join(",", seen.Order())}");
        // Each count of the class's comes; a chain ends short of its count only now and then.
        for (int n = cls.Events.Min; n <= cls.Events.Max; n++) Assert.True(counts[n] > 0, $"no chain of {n}");
        Assert.Equal(id == "shuttle" ? 4 : 5, seen.Count);
    }

    private static Arbitrary<(uint Seed, (WreckKind Kind, bool Old)[] Steps)> Threes() =>
        Gen.Zip(ArbMap.Default.GeneratorFor<uint>(),
            Gen.Zip(Gen.Elements(Enum.GetValues<WreckKind>()), ArbMap.Default.GeneratorFor<bool>()).ArrayOf(3)).ToArbitrary();

    [Property(MaxTest = 60)]
    public Property ForcedChainsOfThreeOnTheLinerAndTheTankerKeepTheTableTheArrivalRoomAndNoOverlap() => Prop.ForAll(Threes(), t =>
    {
        foreach (var cls in (VesselClass[])[C("liner"), C("tanker")])
        {
            var asked = t.Steps.Select(s => new WreckStep(s.Kind, s.Old)).ToList();
            var b = Shipyard.Build(cls, t.Seed, asked);
            if (b.Wreck == null) continue;
            KeepsTheTable(cls, b, drawn: false);
            // What was built is the steps asked, in order, those not allowed at their turn left out.
            var kinds = Kinds(b);
            int at = 0;
            foreach (var step in asked)
                if (at < kinds.Count && kinds[at] == step.Kind) at++;
            Assert.Equal(kinds.Count, at);
            TheArrivalRoomStands(cls, t.Seed, b);
            NoTwoPartsOverlap(b.Parts);
        }
    });

    [Fact]
    public void AContactIsAWreckWreckPercentTimesInAHundredItsChainFromItsSeed()
    {
        foreach (var cls in Content.Vessels.Classes)
        {
            int wrecks = Enumerable.Range(1, 200).Count(s => Shipyard.Build(cls, (uint)s).Wreck != null);
            Assert.InRange(wrecks * 100 / 200, Wrecks.WreckPercent - 12, Wrecks.WreckPercent + 12);
        }
    }

    [Fact]
    public void IsWreckSaysWhatTheBuildMakesWithoutBuilding()
    {
        foreach (var cls in Content.Vessels.Classes)
        {
            for (uint seed = 1; seed <= 120; seed++)
                Assert.Equal(Shipyard.Build(cls, seed).Wreck != null, Shipyard.IsWreck(cls, seed));
            Assert.False(Shipyard.IsWreck(cls, 1, []));
            Assert.True(Shipyard.IsWreck(cls, 1, [new WreckStep(WreckKind.Icing)]));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFindAskedWreckedOrIntactGetsAContactThatArrivesSo(bool wrecked)
    {
        var w = new Scenario(Registry.Wreck);
        foreach (var cls in Content.Vessels.Classes)
            for (int n = 0; n < 2; n++)
            {
                w.Do(new Find(cls.Id, null, wrecked)).Run();
                var c = w.M.Places.Contacts[^1];
                Assert.Equal(wrecked, Shipyard.IsWreck(cls, c.Seed, c.Wreck));
                Assert.Equal(wrecked, Shipyard.Build(cls, c.Seed, c.Wreck).Wreck != null);
                w.Do(new Forget(c.Id)).Run();
            }
    }

    [Fact]
    public void ASeedThatDrawsNoWreckStampsTheIntactVessel()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 8; seed++)
            {
                var b = Shipyard.Build(cls, seed, null, 0);
                Assert.Null(b.Wreck);
                Assert.Equal(Keys(Shipyard.Stamp(Shipyard.Describe(cls, seed))), Keys(b.Parts));
                Assert.Equal(Keys(b.Parts), Keys(Shipyard.Build(cls, seed, []).Parts));
            }
    }

    [Fact]
    public void AnEventThatComesOnceIsSkippedTheSecondTimeAndAShuttleTakesOneAndNeverBreaks()
    {
        Assert.Equal([WreckKind.Explosion], Kinds(Shipyard.Build(C("liner"), 3, Chain(WreckKind.Explosion, WreckKind.Explosion))));
        Assert.Equal([WreckKind.Break], Kinds(Shipyard.Build(C("liner"), 3, Chain(WreckKind.Break, WreckKind.Break))));
        Assert.Equal([WreckKind.Icing], Kinds(Shipyard.Build(C("liner"), 3, Chain(WreckKind.Icing, WreckKind.Icing))));
        Assert.Equal([WreckKind.Explosion], Kinds(Shipyard.Build(C("shuttle"), 3, Chain(WreckKind.Explosion, WreckKind.Fire))));
        Assert.Equal([WreckKind.Asteroid, WreckKind.Explosion], Kinds(Shipyard.Build(C("freighter"), 3, Chain(WreckKind.Asteroid, WreckKind.Explosion, WreckKind.Fire))));
        int broken = 0;
        for (uint seed = 1; seed <= 20; seed++)
        {
            Assert.Null(Shipyard.Build(C("shuttle"), seed, Chain(WreckKind.Break)).Wreck);
            // A freighter breaks where a rib stands clear of its ends and the arrival room.
            if (Kinds(Shipyard.Build(C("freighter"), seed, Chain(WreckKind.Break))).SequenceEqual([WreckKind.Break])) broken++;
        }
        Assert.True(broken > 10, $"{broken} of 20 freighters broke");
    }

    [Fact]
    public void IcingFirstIsThePowerLostEveryCompartmentFrozenAndNoCellTouched()
    {
        foreach (var cls in Content.Vessels.Classes)
        {
            var b = Shipyard.Build(cls, 5, Chain(WreckKind.Icing));
            var ice = Assert.Single(b.Wreck!.Events);
            var (sx, sy) = Shipyard.Door(b);
            int arrival = b.Vessel.Compartments.ToList().FindIndex(c => c.Holds(sx, sy));
            Assert.Equal(Enumerable.Range(0, b.Vessel.Compartments.Count).Where(i => i != arrival), ice.Compartments);
            Assert.Empty(ice.Carved);
            Assert.Equal(Keys(Shipyard.Build(cls, 5, []).Parts), Keys(b.Parts));
        }
    }

    [Fact]
    public void IcingAfterOnlyAFireIsSkippedAfterAnAsteroidRefusedAndAfterAnExplosionJoinsItsGroup()
    {
        Assert.Equal([WreckKind.Fire], Kinds(Shipyard.Build(C("liner"), 4, Chain(WreckKind.Fire, WreckKind.Icing))));
        var rock = Shipyard.Build(C("liner"), 4, Chain(WreckKind.Asteroid, WreckKind.Icing));
        Assert.Equal([WreckKind.Asteroid], Kinds(rock));
        // The strike froze its own breach: its compartments are those the cold reached.
        Assert.NotEmpty(rock.Wreck!.Events[0].Compartments);
        var blast = Shipyard.Build(C("liner"), 4, [new(WreckKind.Explosion, Old: true), new(WreckKind.Icing)]);
        Assert.Equal([WreckKind.Explosion, WreckKind.Icing], Kinds(blast));
        Assert.Equal(blast.Wreck!.Events[0].Group, blast.Wreck.Events[1].Group);
        Assert.True(blast.Wreck.Events[1].Old, "icing takes its explosion's age");
    }

    [Fact]
    public void ScavengersAfterAnOldFirstGroupTakeSkinAndKeepTheEnginesAndEveryRoomsThings()
    {
        foreach (var id in (string[])["freighter", "liner", "tanker"])
            for (uint seed = 1; seed <= 6; seed++)
            {
                var intact = Shipyard.Build(C(id), seed, []);
                var b = Shipyard.Build(C(id), seed, [new(WreckKind.Icing, Old: true)]);
                Assert.True(b.Wreck!.Scavenged);
                Assert.Equal(Keys(intact.Parts.Where(p => p.Layer == "machine")), Keys(b.Parts.Where(p => p.Layer == "machine")));
                // Only hull walls and their back walls went, none came.
                var gone = Keys(intact.Parts).Except(Keys(b.Parts)).ToList();
                Assert.NotEmpty(gone);
                Assert.All(gone, p => Assert.True(p.Contains(",wall,hull,") || p.Contains(",backWall,hull,"), p));
                Assert.Empty(Keys(b.Parts).Except(Keys(intact.Parts)));
                var fresh = Shipyard.Build(C(id), seed, [new(WreckKind.Icing)]);
                Assert.False(fresh.Wreck!.Scavenged);
                Assert.Equal(Keys(intact.Parts), Keys(fresh.Parts));
            }
    }

    [Theory]
    [InlineData("freighter")]
    [InlineData("liner")]
    public void FireIsAllowedWhileACompartmentIsSealedAndRefusedOnceEveryOneHasBurnt(string id)
    {
        var cls = C(id);
        var v = Shipyard.Describe(cls, 9);
        int entry = v.Doors.ToList().IndexOf(v.Doors.OrderBy(d => d.Y).ThenBy(d => d.X).First());
        var hulk = new Hulk(cls, v, Shipyard.Stamp(v), entry);
        int fires = 0;
        while (hulk.Allowed([]).Contains(WreckKind.Fire))
        {
            hulk.Apply(WreckKind.Fire, fires == 0 ? [] : [WreckKind.Fire], new Rng(new RngState { S = (uint)fires + 1 }));
            Assert.True(++fires < 200, "a fire burns some compartment each time");
        }
        Assert.True(fires >= 1);
        var b = hulk.Laid(entry, [.. Enumerable.Range(0, fires).Select(i => (false, i))], false, [], new Rng(new RngState { S = 9 }));
        var burnt = b.Wreck!.Events.SelectMany(e => e.Compartments).ToHashSet();
        var (sx, sy) = Shipyard.Door(b);
        for (int c = 0; c < v.Compartments.Count; c++)
            if (v.Compartments[c].Width >= 4 && !v.Compartments[c].Holds(sx, sy)) Assert.Contains(c, burnt);
    }

    [Theory]
    [InlineData("freighter")]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void ABreakCutsAtTheRibNearestItsCauseOrOneOfTheTwoNearestTheMiddle(string id)
    {
        var cls = C(id);
        int caused = 0, overloads = 0;
        for (uint seed = 1; seed <= 24; seed++)
        {
            var intact = Shipyard.Build(cls, seed, []);
            var v = intact.Vessel;
            var (sx, sy) = Shipyard.Door(intact);
            var room = v.Compartments.First(c => c.Holds(sx, sy));
            var body = v.Sections.First(s => s.Kind == Vessel.SectionKind.Body);
            int bow = v.Sections.First(s => s.Kind == Vessel.SectionKind.Bow).Left;
            var ribs = v.Sections.Where(s => s.Kind == Vessel.SectionKind.Rib && s.Left > body.Left + Wrecks.BreakFromEnds && s.Left < bow - Wrecks.BreakFromEnds
                && (s.Left + 13 < room.Left - Wrecks.ProtectRing || s.Left - 11 > room.Left + room.Width - 1 + Wrecks.ProtectRing)).Select(s => s.Left).ToList();
            var rock = Shipyard.Build(cls, seed, Chain(WreckKind.Asteroid));
            var broken = Shipyard.Build(cls, seed, Chain(WreckKind.Asteroid, WreckKind.Break));
            if (Kinds(broken).Count == 2)
            {
                var cut = broken.Wreck!.Events[1];
                Assert.Equal(WreckKind.Asteroid, cut.Cause);
                int at = rock.Wreck!.Events[0].X, rib = cut.Break!.Column - 1;
                Assert.Contains(rib, ribs);
                Assert.True(Math.Abs(rib - at) <= ribs.Min(r => Math.Abs(r - at)) + 1, $"seed {seed}: cut at {rib}, the strike at {at}, ribs {string.Join(",", ribs)}");
                caused++;
            }
            if (Shipyard.Build(cls, seed, Chain(WreckKind.Break)).Wreck is { } w)
            {
                Assert.Null(w.Events[0].Cause);
                int mid2 = body.Left + bow, rib = w.Events[0].Break!.Column - 1;
                Assert.Contains(rib, ribs.OrderBy(r => Math.Abs(2 * r - mid2)).ThenBy(r => r).Take(2));
                overloads++;
            }
            else Assert.Empty(ribs);
        }
        Assert.True(caused > 0 && overloads > 0, $"{caused} caused, {overloads} overloads");
    }

    [Theory]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void TwoAsteroidsStrikeThreeCraterRadiiApart(string id)
    {
        var cls = C(id);
        int both = 0;
        for (uint seed = 1; seed <= 30; seed++)
        {
            var b = Shipyard.Build(cls, seed, Chain(WreckKind.Asteroid, WreckKind.Asteroid));
            if (b.Wreck!.Events.Count < 2) continue;
            both++;
            // Each record's centre leans a little along its direction off the struck column.
            int lean = (3 * cls.Rock + 9) / 10;
            Assert.True(Math.Abs(b.Wreck.Events[0].X - b.Wreck.Events[1].X) >= 3 * cls.Rock - 2 * lean - 1, $"seed {seed}");
            Assert.All(b.Wreck.Events, e => Assert.NotEmpty(e.Carved));
        }
        Assert.True(both > 10, $"{both} of 30");
    }

    [Fact]
    public void AnAsteroidLeavesStonesAnExplosionAVoidAndALaterEventNeverChangesAnEarlier()
    {
        int stones = 0;
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 10; seed++)
            {
                var b = Shipyard.Build(cls, seed, Chain(WreckKind.Asteroid));
                // A short shuttle may have no column a crater clears its arrival room from.
                if (b.Wreck == null && cls.Id == "shuttle") continue;
                var rock = b.Wreck!.Events[0];
                Assert.NotEmpty(rock.Carved);
                foreach (var (x, y) in rock.Stones)
                {
                    Assert.Contains(b.Parts, p => p.Kind == Content.Vessels.Stone && p.X == x && p.Y == y);
                    stones++;
                }
                Assert.NotEmpty(Shipyard.Build(cls, seed, Chain(WreckKind.Explosion)).Wreck!.Events[0].Carved);
                var after = Shipyard.Build(cls, seed, Chain(WreckKind.Asteroid, WreckKind.Fire));
                Assert.Equal(rock.Carved, after.Wreck!.Events[0].Carved);
            }
        Assert.True(stones > 10, $"{stones} stones");
    }

    [Fact]
    public void AFireBurnsHalfOfEachStackInACrateItReachedRoundedDown()
    {
        int burnt = 0, odd = 0;
        foreach (var id in (string[])["shuttle", "freighter", "liner"])
            for (uint seed = 1; seed <= 40; seed++)
            {
                var b = Shipyard.Build(C(id), seed, Chain(WreckKind.Fire));
                if (b.Wreck == null) continue;
                foreach (var crate in Shipyard.Build(C(id), seed, []).Parts.Where(p => p.Layer == "machine" && p.Holds != null))
                {
                    var now = b.Parts.FirstOrDefault(p => p.Layer == "machine" && p.X == crate.X && p.Y == crate.Y && p.Kind == crate.Kind);
                    if (now == null || now.Holds != null && now.Holds.SequenceEqual(crate.Holds!)) continue;
                    var left = crate.Holds!.Select(s => (s.Item, s.Count - s.Count * Wrecks.BurntLoss / 100)).Where(s => s.Item2 > 0).ToList();
                    Assert.Equal(left, now.Holds ?? []);
                    burnt++;
                    odd += crate.Holds!.Count(s => s.Count % 2 == 1);
                }
            }
        Assert.True(burnt > 5 && odd > 0, $"{burnt} crates burnt, {odd} odd stacks");
    }

    [Fact]
    public void AForcedChainReachesTheVesselWorldThroughFindAndJumpAndSurvivesASave()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        var chain = Chain(WreckKind.Asteroid, WreckKind.Break);
        w.Do(new Find("freighter", chain)).Run();
        var contact = w.M.Places.Contacts[^1];
        Assert.Equal(chain, contact.Wreck);
        w.Do(new Jump(contact.Id)).Run();
        Assert.Empty(w.Refusals);
        int id = w.World.SurfaceOf(0);
        Assert.Equal(chain, w.World.Surfaces[id].Wreck);
        var b = Shipyard.Build(C("freighter"), contact.Seed, chain);
        Assert.Equal(2, b.Wreck!.Events.Count);
        var v = w.On(id);
        int width = v.Grid.Width, m = Content.Vessels.Margin;
        foreach (var part in b.Parts.Where(p => p.Layer == "machine"))
            Assert.Equal(part.Kind, v.Grid.MachineAt((m + part.Y) * width + m + part.X)?.Kind);
        var (dx, dy) = Shipyard.Door(b);
        Assert.Equal((m + dy) * width + m + dx, v.Crew.HeroOf(0)!.Cell);
        string save = w.World.Save();
        w.Reload();
        Assert.Equal(save, w.World.Save());
        Assert.Equal(chain, w.World.Surfaces[id].Wreck);
    }

    /// <summary>What holds each machine along each side (0 under, 1 over, 2 left, 3 right): hull, glass or another part's cell.</summary>
    private static int Touching(VesselPart p, int side, Dictionary<(int, int), int> held, int self)
    {
        var spec = Content.Machines.Get(p.Kind);
        int n = 0, along = side < 2 ? spec.Width : spec.Height;
        for (int k = 0; k < along; k++)
        {
            var c = side switch { 0 => (p.X + k, p.Y + spec.Height), 1 => (p.X + k, p.Y - 1), 2 => (p.X - 1, p.Y + k), _ => (p.X + spec.Width, p.Y + k) };
            if (held.TryGetValue(c, out int o) && o != self) n++;
        }
        return n;
    }

    /// <summary>Whether <paramref name="p"/> touches nothing on any side: thrown off, drifting.</summary>
    private static bool Drifts(VesselPart p, Dictionary<(int, int), int> held, int self) =>
        Enumerable.Range(0, 4).All(side => Touching(p, side, held, self) == 0);

    private static Dictionary<(int, int), int> Held(IReadOnlyList<VesselPart> parts)
    {
        var held = new Dictionary<(int, int), int>();
        for (int i = 0; i < parts.Count; i++)
            if (parts[i].Layer != "back")
                foreach (var c in Shipyard.Covers(parts[i])) held[c] = i;
        return held;
    }

    [Theory]
    [InlineData("shuttle")]
    [InlineData("freighter")]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void NothingHangsOverMissingSupportAfterTheEvents(string id)
    {
        var cls = C(id);
        int checkedParts = 0;
        for (uint seed = 1; seed <= 200; seed++)
        {
            var intact = Shipyard.Build(cls, seed, []).Parts;
            var b = Shipyard.Build(cls, seed, null, 100);
            var (was, now) = (Held(intact), Held(b.Parts));
            var rests = new Dictionary<(string, int, int), (int Side, int Count)>();
            var cut = b.Wreck?.Events.Select(e => e.Break).FirstOrDefault(x => x != null);
            for (int i = 0; i < intact.Count; i++)
            {
                var p = intact[i];
                if (p.Layer != "machine") continue;
                var best = (Side: -1, Count: 0);
                for (int side = 0; side < 4; side++)
                    if (Touching(p, side, was, i) > best.Count) best = (side, Touching(p, side, was, i));
                // A break moved each half (WreckBreak): read the intact part where it went, those by the ragged cut left out.
                var (mx, my) = (p.X, p.Y);
                if (cut is { } k)
                {
                    int w = Content.Machines.Get(p.Kind).Width, reach = 8;
                    if (p.X + w > k.Column - reach && p.X < k.Column + reach) continue;
                    (mx, my) = p.X < k.Column ? (p.X - k.Gap / 2, p.Y + k.SternRows) : (p.X + k.Gap - k.Gap / 2, p.Y + k.BowRows);
                }
                rests[(p.Kind, mx, my)] = best;
            }
            for (int i = 0; i < b.Parts.Count; i++)
            {
                var p = b.Parts[i];
                if (p.Layer != "machine") continue;
                if (p.Kind == Content.Vessels.Stone)
                    Assert.Equal(Content.Machines.Get(p.Kind).Width, Touching(p, 0, now, i));
                else if (rests.TryGetValue((p.Kind, p.X, p.Y), out var rest) && rest.Side >= 0 && !Drifts(p, now, i))
                {
                    Assert.True(Touching(p, rest.Side, now, i) >= rest.Count, $"seed {seed}: {p} holds by {Touching(p, rest.Side, now, i)} of {rest.Count}");
                    checkedParts++;
                }
            }
        }
        Assert.True(checkedParts > 1000);
    }

    /// <summary>The machines of <paramref name=b/> the events threw out: in space (no back wall, no wall under any cell), not where the intact vessel had them.</summary>
    private static List<VesselPart> Thrown(VesselClass cls, uint seed, Built b)
    {
        var intact = Shipyard.Build(cls, seed, []).Parts.Where(p => p.Layer == "machine").Select(p => (p.Kind, p.X, p.Y)).ToHashSet();
        var filled = b.Parts.Where(p => p.Layer == "back" || p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        return [.. b.Parts.Where(p => p.Layer == "machine" && p.Kind != Content.Vessels.Stone && !intact.Contains((p.Kind, p.X, p.Y))
            && Shipyard.Covers(p).All(c => !filled.Contains(c)))];
    }

    [Fact]
    public void WhatAnEventThrowsOutLiesInSpaceAsAMachineClearOfTheArrivalRoomAnOldGroupKeepingAThird()
    {
        int fresh = 0, old = 0;
        foreach (var id in (string[])["liner", "tanker"])
            for (uint seed = 1; seed <= 30; seed++)
            {
                var cls = C(id);
                var b = Shipyard.Build(cls, seed, Chain(WreckKind.Explosion, WreckKind.Asteroid));
                var thrown = Thrown(cls, seed, b);
                var held = Held(b.Parts);
                fresh += thrown.Count;
                var (sx, sy) = Shipyard.Door(b);
                var room = b.Vessel.Compartments.First(c => c.Holds(sx, sy));
                int ring = Wrecks.ProtectRing;
                foreach (var p in thrown)
                {
                    Assert.Contains(b.Vessel.Outside, f => f.Kind == p.Kind && f.X == p.X && f.Y == p.Y);
                    Assert.True(Drifts(p, held, b.Parts.ToList().IndexOf(p)), $"{p} touches the hull or a part");
                    Assert.All(Shipyard.Covers(p), c => Assert.False(c.X >= room.Left - ring && c.X <= room.Left + room.Width - 1 + ring
                        && c.Y >= room.Top - ring && c.Y <= room.Floor + ring, $"{p} in the arrival ring"));
                }
                NoTwoPartsOverlap(b.Parts);
                old += Thrown(cls, seed, Shipyard.Build(cls, seed, [new(WreckKind.Explosion, true), new(WreckKind.Asteroid, true)])).Count;
            }
        Assert.True(fresh > 30, $"{fresh} thrown");
        Assert.InRange((double)old / fresh, Wrecks.ThrownOldPercent / 100.0 - 0.15, Wrecks.ThrownOldPercent / 100.0 + 0.15);
    }
}
