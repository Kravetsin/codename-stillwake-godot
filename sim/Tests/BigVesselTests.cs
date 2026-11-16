using Stillwake.Content;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>The liner and the tanker (big-vessels): rooms several decks tall, doors where floors meet, hatches with ladders, raised rooms, their interiors.</summary>
public class BigVesselTests(ITestOutputHelper output)
{
    /// <summary>FNV-1a over the text, 64 bits.</summary>
    private static ulong Fnv(string text)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in text)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }

    /// <summary>Everything <paramref name="cls"/> makes over seeds 1 to <paramref name="seeds"/>, described and stamped, as one hash.</summary>
    private static ulong Fingerprint(string cls, uint seeds = 64)
    {
        var c = Content.Vessels.Class(cls)!;
        var text = new System.Text.StringBuilder();
        for (uint seed = 1; seed <= seeds; seed++)
        {
            var v = Shipyard.Describe(c, seed);
            text.Append($"{v.Width},{v.Height}|");
            foreach (var s in v.Sections) text.Append(s).Append('|');
            foreach (var d in v.Decks) text.Append(d.Floor).Append('|');
            foreach (var s in v.Shafts) text.Append(s).Append('|');
            foreach (var d in v.Doors) text.Append(d).Append('|');
            foreach (var h in v.Hatches) text.Append(h).Append('|');
            foreach (var r in v.Compartments) text.Append($"{r.Deck},{r.Left},{r.Width},{r.Top},{r.Role},{r.Interior}|");
            foreach (var f in v.Fittings.Concat(v.Outside)) text.Append($"{f.Kind},{f.X},{f.Y},{string.Join("+", f.Loot)}|");
            foreach (var cause in (VesselCause[])[VesselCause.None, VesselCause.Breach])
                foreach (var p in Shipyard.Make(c, seed, cause))
                    text.Append($"{p.X},{p.Y},{p.Layer},{p.Kind},{p.Material},{p.Open},{string.Join("+", p.Holds ?? [])};");
        }
        return Fnv(text.ToString());
    }

    [Theory]
    [InlineData("shuttle", 0x649E21D68D96DC46UL)]
    [InlineData("freighter", 0xFE3B6FEDE7D90D02UL)]
    public void TheShuttleAndTheFreighterAreMadeAsBeforeTheLiner(string cls, ulong pinned)
    {
        ulong made = Fingerprint(cls);
        output.WriteLine($"{cls}: 0x{made:X16}UL");
        Assert.Equal(pinned, made);
    }

    [Fact]
    public void TheLinerIsMadeAsBeforeTheTanker()
    {
        // Re-recorded once as its parts outside came (big-vessels stage 3): its rooms and shape as they were.
        ulong made = Fingerprint("liner", 16);
        output.WriteLine($"liner: 0x{made:X16}UL");
        Assert.Equal(0xA8D3B12CEA9B870DUL, made);
    }

    private static readonly VesselClass Liner = Content.Vessels.Class("liner")!;

    private static Vessel Of(uint seed, string cls = "liner") => Shipyard.Describe(Content.Vessels.Class(cls)!, seed);

    /// <summary>The room holding cell (<paramref name="x"/>, <paramref name="y"/>); null for none.</summary>
    private static Vessel.Compartment? At(Vessel v, int x, int y) => v.Compartments.FirstOrDefault(c => c.Holds(x, y));

    [Fact]
    public void FiveHundredAndTwelveLinersAreDescribedAndStampedWhole()
    {
        for (uint seed = 1; seed <= 512; seed++)
        {
            var v = Of(seed);
            Assert.NotEmpty(Shipyard.Stamp(v));
            Assert.InRange(v.Decks.Count, Liner.Decks.Min, Liner.Decks.Max);
        }
    }

    [Theory]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void ADoorStandsOnlyWhereBothFloorsMeetAndTheGapsOtherDecksAreHull(string cls)
    {
        int faced = 0;
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed, cls);
            var parts = Shipyard.Stamp(v);
            var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
            var doors = v.Doors.Skip(1).Select(d => (d.X, d.Y)).ToHashSet();
            var rooms = v.Compartments.Where(c => c.Deck >= 0).ToList();
            foreach (var p in rooms)
                foreach (var q in rooms.Where(q => q.Left == p.Left + p.Width + 2))
                    for (int d = Math.Max(p.Deck, q.Deck); d < Math.Min(p.Deck + p.Decks, q.Deck + q.Decks); d++)
                    {
                        int gap = p.Left + p.Width, top = v.Decks[d].Top;
                        bool meet = d == p.Deck + p.Decks - 1 && d == q.Deck + q.Decks - 1;
                        Assert.Equal(meet, doors.Remove((gap, top)));
                        if (meet) continue;
                        faced++;
                        for (int y = top; y < top + Content.Vessels.Open; y++)
                            for (int x = gap; x < gap + 2; x++) Assert.Contains((x, y), walls);
                    }
            // Every inner door stands between two such rooms.
            Assert.Empty(doors);
        }
        Assert.True(faced > 0, "no tall room faced hull");
    }

    [Fact]
    public void TheWellRunsDownTheServiceBlocksLeftAndARepairHatchReachesTheRoomsTallRoomsCutOff()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed);
            var aft = v.Compartments.Single(c => c.Deck == 0 && c.Left == v.Doors[0].X + 2);
            int well = aft.Left + aft.Width + 2;
            for (int d = 0; d + 1 < v.Decks.Count; d++)
            {
                var (upper, lower) = (At(v, well, v.Decks[d].Top)!, At(v, well, v.Decks[d + 1].Top)!);
                bool differ = upper != lower && upper.Floor + 1 == lower.Top;
                Assert.Equal(differ, v.Hatches.Contains(new(well, v.Decks[d].Floor)));
            }
            // A room no door nor the well reaches, a tall room beside it on its deck: a hatch from a room over it, two columns in from
            // the right end of the columns they share.
            Assert.Contains(v.Hatches, h => At(v, h.X, h.Y - 1) is { } over && At(v, h.X, h.Y + 1) is { } under
                && h.X == Math.Min(over.Left + over.Width, under.Left + under.Width) - 2 && h.X != well);
        }
    }

    [Fact]
    public void ALadderRunsDownATallRoomInWholePiecesToARowOverItsFloor()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed);
            var parts = Shipyard.Stamp(v);
            var tall = v.Shafts.Select(s => (Shaft: s, Room: At(v, s.Column, s.Top))).Where(t => t.Room is { Decks: > 1 }).Select(t => (t.Shaft, Room: t.Room!)).ToList();
            Assert.NotEmpty(tall);
            foreach (var (shaft, room) in tall)
            {
                Assert.Equal((room.Top, room.Floor - 2), (shaft.Top, shaft.Bottom));
                Assert.Equal(room.Height / 2, parts.Count(p => p.Kind == "ladder" && p.X == shaft.Column && p.Y >= room.Top && p.Y < room.Floor));
            }
        }
    }

    [Fact]
    public void EachRoomOfALinerIsFurnishedByItsRoleAndEveryHangarHoldsAShuttle()
    {
        var models = new HashSet<string>();
        for (uint seed = 1; seed <= 40; seed++)
        {
            var v = Of(seed);
            foreach (var role in (VesselRole[])[VesselRole.Pool, VesselRole.Restaurant, VesselRole.Lounge, VesselRole.Galley, VesselRole.Infirmary,
                         VesselRole.Hold, VesselRole.FuelBay, VesselRole.Hangar])
                Assert.Contains(v.Compartments, c => c.Role == role);
            Assert.Single(v.Compartments, c => c.Role == VesselRole.Hangar);
            foreach (var c in v.Compartments)
            {
                var inside = v.Fittings.Where(f => c.Holds(f.X, f.Y)).ToList();
                int Count(string kind) => inside.Count(f => Content.Vessels.FirstLook(f.Kind) == kind);
                switch (c.Role)
                {
                    case VesselRole.Pool:
                        Assert.Equal((2, 1, 1), (c.Decks, Count(Content.Vessels.Pool), Count(Content.Vessels.Diving)));
                        break;
                    case VesselRole.Restaurant:
                        Assert.Equal((2, 1, 1, 2), (c.Decks, Count(Content.Vessels.Bar), Count(Content.Vessels.Piano), Count(Content.Vessels.Chandelier)));
                        Assert.True(Count(Content.Vessels.RoundTable) > 0 && Count(Content.Vessels.Chair) == 2 * Count(Content.Vessels.RoundTable));
                        break;
                    case VesselRole.Galley:
                        Assert.Equal((1, 2, 2), (Count(Content.Vessels.Stove), Count(Content.Vessels.Counter), Count("salvageVending")));
                        break;
                    case VesselRole.Infirmary:
                        Assert.Equal((2, 1), (Count(Content.Vessels.Bed), Count(Content.Vessels.MedCab)));
                        break;
                    case VesselRole.Lounge:
                        Assert.True(Count(Content.Vessels.Lounger) > 0, $"a lounge on deck {c.Deck} with no lounger");
                        break;
                    case VesselRole.Hold when c.Decks > 1:
                        Assert.True(Count(Content.Vessels.Cargo) + Count("salvageCrate") > 0);
                        break;
                    case VesselRole.FuelBay:
                        // Its look fuel, the capsule's first.
                        Assert.Single(inside, f => f.Kind == Content.Vessels.Capsule);
                        break;
                    case VesselRole.Hangar:
                        var shuttle = Assert.Single(inside, f => Content.Vessels.Shuttles.Contains(f.Kind));
                        models.Add(shuttle.Kind);
                        Assert.Equal(c.Floor, shuttle.Y + Machines.Get(shuttle.Kind).Height);
                        Assert.Equal(1, Count(Content.Vessels.FuelCart));
                        break;
                }
            }
            Assert.False(v.Doors[0].Open, "the way in stands open");
        }
        Assert.Equal(Content.Vessels.Shuttles.Order(), models.Order());
    }

    [Fact]
    public void ALinerWithItsRepairHatchGoneIsUnreached()
    {
        var v = Of(5);
        Assert.Null(VesselTests.Unreached(v, Shipyard.Stamp(v)));
        int last = v.Hatches.Count - 1;
        var cut = v with { Hatches = [.. v.Hatches.Take(last)], Shafts = [.. v.Shafts.Take(last)] };
        Assert.NotNull(VesselTests.Unreached(cut, Shipyard.Stamp(cut)));
    }

    // The tanker.

    private static readonly VesselClass TankerClass = Content.Vessels.Class("tanker")!;

    private static Vessel Tanker(uint seed) => Of(seed, "tanker");

    [Fact]
    public void FiveHundredAndTwelveTankersAreDescribedAndStampedWhole()
    {
        var decks = new HashSet<int>();
        for (uint seed = 1; seed <= 512; seed++)
        {
            var v = Tanker(seed);
            Assert.NotEmpty(Shipyard.Stamp(v));
            Assert.InRange(v.Decks.Count, TankerClass.Decks.Min, TankerClass.Decks.Max);
            decks.Add(v.Decks.Count);
        }
        Assert.Equal([4, 5], decks.Order());
    }

    [Fact]
    public void ATankersBridgeStandsOverItsSternGlazedForeAndAft()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Tanker(seed);
            var bridge = v.Sections.Single(s => s.Kind == Vessel.SectionKind.Bridge);
            var airlock = v.Compartments.Single(c => c.Deck == 0 && c.Left == v.Doors[0].X + 2);
            var room = v.Compartments.Single(c => c.Role == VesselRole.Bridge);
            // Its hatch over the airlock, past its walkway.
            Assert.InRange(room.Left, airlock.Left + Content.Vessels.Tanker.BridgeFrom, airlock.Left + airlock.Width - 2);
            Assert.Contains(new Vessel.Hatch(room.Left, airlock.Top - TankerClass.ArmourDepth), v.Hatches);
            var glass = Shipyard.Stamp(v).Where(p => p.Material == Content.Vessels.Glass).Select(p => p.X).ToHashSet();
            Assert.Contains(bridge.Left, glass);
            Assert.Contains(bridge.Left + bridge.Width - 1, glass);
        }
    }

    [Fact]
    public void TheFuelBaySpansTheSecondDeckToTheLastPastTheEngineRoomsItsColumnAsTallAndAPipewayOverIt()
    {
        for (uint seed = 1; seed <= 40; seed++)
        {
            var v = Tanker(seed);
            var aft = v.Compartments.Single(c => c.Deck == 0 && c.Left == v.Doors[0].X + 2);
            var fuel = Assert.Single(v.Compartments, c => c.Role == VesselRole.FuelBay);
            Assert.Equal((1, v.Decks.Count - 1, aft.Left + aft.Width + 2), (fuel.Deck, fuel.Decks, fuel.Left));
            Assert.Contains(v.Compartments, c => c.Deck == 0 && c.Left == fuel.Left && c.Role == VesselRole.Pipeway);
            var column = Assert.Single(v.Fittings, f => fuel.Holds(f.X, f.Y) && f.Kind != Content.Vessels.Lamp);
            Assert.Equal(Content.Vessels.FuelColumn(fuel.Decks), column.Kind);
            Assert.Equal(fuel.Floor, column.Y + Machines.Get(column.Kind).Height);
        }
    }

    [Fact]
    public void TheWellRunsDownTheServiceBlockPastTheFuelBayAndPumpRoomsBetweenTankBaysHaveRepairHatches()
    {
        int repaired = 0;
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Tanker(seed);
            var fuel = v.Compartments.Single(c => c.Role == VesselRole.FuelBay);
            int well = fuel.Left + fuel.Width + 2;
            for (int d = 0; d + 1 < v.Decks.Count; d++)
                Assert.Contains(new Vessel.Hatch(well, v.Decks[d].Floor), v.Hatches);
            // A pump room between two tank bays has no door: a hatch in its ceiling from the room over it, two columns in from its right.
            foreach (var pump in v.Compartments.Where(c => c.Role == VesselRole.PumpRoom))
            {
                bool shut = At(v, pump.Left - 3, pump.Top) is { Role: VesselRole.TankBay } left && left.Floor > pump.Floor
                    && At(v, pump.Left + pump.Width + 2, pump.Top) is { Role: VesselRole.TankBay } right && right.Floor > pump.Floor;
                if (!shut) continue;
                Assert.Contains(new Vessel.Hatch(pump.Left + pump.Width - 2, pump.Top - 1), v.Hatches);
                repaired++;
            }
        }
        Assert.True(repaired > 0, "no pump room between tank bays");
    }

    [Fact]
    public void ALadderRunsUpEveryTallTankRoomInWholePiecesClearOfItsTanks()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Tanker(seed);
            var parts = Shipyard.Stamp(v);
            var machines = parts.Where(p => p.Layer == "machine").SelectMany(Shipyard.Covers).ToHashSet();
            foreach (var room in v.Compartments.Where(c => c.Role is VesselRole.FuelBay or VesselRole.TankBay))
            {
                var shaft = Assert.Single(v.Shafts, s => room.Holds(s.Column, s.Top));
                Assert.Equal((room.Top, room.Top + room.Height - room.Height % 2 - 1), (shaft.Top, shaft.Bottom));
                Assert.Equal(room.Height / 2, parts.Count(p => p.Kind == "ladder" && p.X == shaft.Column && room.Holds(p.X, p.Y)));
                for (int y = shaft.Top; y <= shaft.Bottom; y++)
                    Assert.False(machines.Contains((shaft.Column, y)) || machines.Contains((shaft.Column + 1, y)), $"a tank on the ladder at ({shaft.Column}, {y})");
            }
        }
    }

    [Fact]
    public void ATankBayHoldsTwoTallTanksOrOneCapsuleNeverOfFuelAndEveryHangarAShuttleAndAFuelCart()
    {
        var models = new HashSet<string>();
        var contents = new HashSet<string>();
        int hangars = 0, without = 0;
        for (uint seed = 1; seed <= 60; seed++)
        {
            var v = Tanker(seed);
            foreach (var role in (VesselRole[])[VesselRole.TankBay, VesselRole.PumpRoom, VesselRole.Pipeway, VesselRole.FuelBay])
                Assert.Contains(v.Compartments, c => c.Role == role);
            Assert.All(v.Compartments.Where(c => c.Role == VesselRole.TankBay), c => Assert.Equal(2, c.Decks));
            int count = v.Compartments.Count(c => c.Role == VesselRole.Hangar);
            Assert.InRange(count, 0, 1);
            if (count == 0) without++;
            foreach (var c in v.Compartments)
            {
                var inside = v.Fittings.Where(f => c.Holds(f.X, f.Y)).ToList();
                int Count(string kind) => inside.Count(f => Content.Vessels.FirstLook(f.Kind) == kind);
                switch (c.Role)
                {
                    case VesselRole.TankBay:
                        var tanks = inside.Where(f => Content.Vessels.FirstLook(f.Kind) is Content.Vessels.Capsule or Content.Vessels.TankTall).ToList();
                        Assert.True(tanks.Count is 1 or 2, $"{tanks.Count} tanks in a bay");
                        Assert.DoesNotContain(tanks, f => f.Kind == Content.Vessels.Capsule);
                        Assert.Single(tanks.Select(f => f.Kind).Distinct());
                        contents.UnionWith(tanks.Select(f => f.Kind));
                        break;
                    case VesselRole.PumpRoom:
                        Assert.Equal(1, Count(Content.Vessels.Pump));
                        break;
                    case VesselRole.Pipeway:
                        Assert.Equal(1, Count(Content.Vessels.Valves));
                        break;
                    case VesselRole.Hangar:
                        hangars++;
                        Assert.Equal((2, v.Decks.Count - 2), (c.Decks, c.Deck));
                        var shuttle = Assert.Single(inside, f => Content.Vessels.Shuttles.Contains(f.Kind));
                        models.Add(shuttle.Kind);
                        Assert.Equal(c.Floor, shuttle.Y + Machines.Get(shuttle.Kind).Height);
                        Assert.Equal(1, Count(Content.Vessels.FuelCart));
                        break;
                }
            }
            Assert.False(v.Doors[0].Open, "the way in stands open");
        }
        Assert.True(hangars > 0 && without > 0, $"{hangars} hangars, {without} tankers without");
        Assert.Equal(Content.Vessels.Shuttles.Order(), models.Order());
        // Every content but fuel, in both kinds of tank.
        var all = Enumerable.Range(1, Content.Vessels.CapsuleContents - 1).Select(k => Content.Vessels.LookKind(Content.Vessels.Capsule, k))
            .Concat(Enumerable.Range(0, Content.Vessels.TankTallContents).Select(k => Content.Vessels.LookKind(Content.Vessels.TankTall, k)));
        Assert.Equal(all.Order(), contents.Order());
    }

    [Fact]
    public void ATankerWithARepairHatchGoneIsUnreached()
    {
        var v = Tanker(3);
        Assert.Null(VesselTests.Unreached(v, Shipyard.Stamp(v)));
        // The last hatch, a repair one, and its ladder: the tanks' ladders come after every hatch's.
        int last = v.Hatches.Count - 1;
        var cut = v with { Hatches = [.. v.Hatches.Take(last)], Shafts = [.. v.Shafts.Where(s => s != v.Shafts[last])] };
        Assert.NotNull(VesselTests.Unreached(cut, Shipyard.Stamp(cut)));
    }

    [Fact]
    public void AboutHalfALinersPodsAreGoneTheirCradlesLeft()
    {
        int pods = 0, cradles = 0;
        for (uint seed = 1; seed <= 64; seed++)
        {
            var v = Of(seed);
            pods += v.Outside.Count(f => f.Kind == Content.Vessels.Pod);
            cradles += v.Outside.Count(f => f.Kind == Content.Vessels.Cradle);
        }
        output.WriteLine($"{pods} pods, {cradles} cradles");
        // PodPercent in a hundred still there, give or take ten.
        Assert.InRange(pods * 100 / (pods + cradles), Content.Vessels.Fits.PodPercent - 10, Content.Vessels.Fits.PodPercent + 10);
        Assert.InRange(Content.Vessels.Fits.PodPercent, 35, 65);
    }

    [Fact]
    public void ATankersManifoldRunsOnOneLevelOnStrutsToTheHullARiserAndValveOverEachTankBayItSpans()
    {
        var manifold = Machines.Get(Content.Vessels.Manifold);
        string valve = Content.Vessels.LookKind(Content.Vessels.Riser, 1);
        for (uint seed = 1; seed <= 64; seed++)
        {
            var v = Tanker(seed);
            var walls = Shipyard.Stamp(v).Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
            var segments = v.Outside.Where(f => f.Kind == Content.Vessels.Manifold).OrderBy(f => f.X).ToList();
            Assert.NotEmpty(segments);
            Assert.Single(segments.Select(s => s.Y).Distinct());
            // Segments end to end, each on at least one strut standing on the hull.
            for (int i = 1; i < segments.Count; i++) Assert.Equal(segments[i - 1].X + manifold.Width, segments[i].X);
            int under = segments[0].Y + manifold.Height;
            var struts = v.Outside.Where(f => f.Kind is Content.Vessels.Pylon or Content.Vessels.Riser).ToList();
            bool Reaches(int x)
            {
                int y = under;
                while (struts.Exists(s => s.X == x && s.Y == y)) y++;
                return y > under && walls.Contains((x, y));
            }
            foreach (var s in segments) Assert.Contains(Enumerable.Range(s.X, manifold.Width), Reaches);
            // A riser down to each tank bay under the run, its valve over the manifold, its column the bay's middle and RiserPast.
            int from = segments[0].X, to = segments[^1].X + manifold.Width;
            foreach (int left in v.Compartments.Where(c => c.Role == VesselRole.TankBay).Select(c => c.Left).Distinct())
            {
                var bay = v.Compartments.First(c => c.Left == left);
                int x = bay.Left + bay.Width / 2 + Content.Vessels.Fits.RiserPast;
                if (x < from || x >= to) continue;
                Assert.True(Reaches(x) && v.Outside.Any(f => f.Kind == Content.Vessels.Riser && f.X == x), $"tanker {seed}: no riser at {x}");
                Assert.Contains(v.Outside, f => f.Kind == valve && (f.X, f.Y) == (x, segments[0].Y - 1));
            }
            Assert.Equal(v.Outside.Count(f => f.Kind == valve), v.Outside.Count(f => f.Kind == Content.Vessels.Riser && f.Y == under));
        }
    }

    [Fact]
    public void SpheresHangOnlyUnderATankBayEachAgainstTheKeelAndOnSomeTankers()
    {
        int with = 0;
        for (uint seed = 1; seed <= 64; seed++)
        {
            var v = Tanker(seed);
            var walls = Shipyard.Stamp(v).Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
            var spheres = v.Outside.Where(f => f.Kind == Content.Vessels.Sphere).ToList();
            if (spheres.Count > 0) with++;
            var sphere = Machines.Get(Content.Vessels.Sphere);
            foreach (var s in spheres)
            {
                Assert.Contains(v.Compartments, c => c.Role == VesselRole.TankBay && c.Left <= s.X && s.X + sphere.Width <= c.Left + c.Width);
                Assert.Contains(Enumerable.Range(s.X, sphere.Width), x => walls.Contains((x, s.Y - 1)));
            }
        }
        // SpherePercent of a hundred, give or take.
        Assert.InRange(with * 100 / 64, Content.Vessels.Fits.SpherePercent - 20, Content.Vessels.Fits.SpherePercent + 20);
        Assert.DoesNotContain(Enumerable.Range(1, 16).Select(s => Of((uint)s)), v => v.Outside.Any(f => f.Kind == Content.Vessels.Sphere));
    }

    [Fact]
    public void TheRadarsWeightsAreEachClassesAndSumAboveNothing()
    {
        Assert.Equal([("shuttle", 1), ("freighter", 3), ("liner", 5), ("tanker", 4)], Content.Vessels.Classes.Select(c => (c.Id, c.Weight)));
        Assert.All(Content.Vessels.Classes, c => Assert.True(c.Weight >= 0, c.Id));
        Assert.True(Content.Vessels.Weights(Content.Vessels.Classes) > 0);
    }

    [Fact]
    public void EachDrawFallsOnAClassAsOftenAsItsWeightAndNeverOnOneOfWeightNothing()
    {
        var c = Content.Vessels.Classes;
        VesselClass[] classes = [c[0] with { Weight = 2 }, c[1] with { Weight = 0 }, c[2] with { Weight = 3 }, c[3] with { Weight = 0 }];
        int sum = Content.Vessels.Weights(classes);
        var drawn = Enumerable.Range(0, sum).Select(d => Content.Vessels.Weighed(classes, d).Id).ToList();
        Assert.Equal(["shuttle", "shuttle", "liner", "liner", "liner"], drawn);
        Assert.Throws<ArgumentOutOfRangeException>(() => Content.Vessels.Weighed(classes, sum));
    }
}
