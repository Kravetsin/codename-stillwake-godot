using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Generated vessels described, stamped and wrecked (generated-vessels): pure, so every peer makes the same one.</summary>
public class VesselTests
{
    /// <summary>A class and a seed, every class drawn.</summary>
    private static Arbitrary<(VesselClass Class, uint Seed)> Vessels() =>
        Gen.Zip(Gen.Elements(Content.Vessels.Classes.ToArray()), ArbMap.Default.GeneratorFor<uint>()).ToArbitrary();

    /// <summary>The parts as text, every value of each in order: what two makings compare by.</summary>
    private static string Key(IReadOnlyList<VesselPart> parts) =>
        string.Join(";", parts.Select(p => $"{p.X},{p.Y},{p.Layer},{p.Kind},{p.Material},{string.Join("+", p.Holds ?? [])}"));

    [Property]
    public Property TheSameClassAndSeedMakeTheSameVesselIntactAndWrecked() => Prop.ForAll(Vessels(), v =>
    {
        foreach (var cause in (VesselCause[])[VesselCause.None, VesselCause.Breach])
            Assert.Equal(Key(Shipyard.Make(v.Class, v.Seed, cause)), Key(Shipyard.Make(v.Class, v.Seed, cause)));
    });

    [Fact]
    public void SeedsMakeVesselsThatDiffer()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var made = Enumerable.Range(1, 8).Select(s => Key(Shipyard.Make(freighter, (uint)s, VesselCause.None))).Distinct().Count();
        Assert.True(made > 4, $"{made} kinds of 8");
    }

    [Property]
    public Property AVesselFitsItsClassFrameAndEveryPartItsBoxOnceASlot() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var frame = v.Class.Frame;
        Assert.InRange(vessel.Width, 1, frame.Width);
        Assert.InRange(vessel.Height, 1, frame.Height);
        var taken = new HashSet<(int, int, bool)>();
        foreach (var part in Shipyard.Stamp(vessel))
            foreach (var (x, y) in Shipyard.Covers(part))
            {
                Assert.InRange(x, 0, vessel.Width - 1);
                Assert.InRange(y, 0, vessel.Height - 1);
                Assert.True(taken.Add((x, y, part.Layer == "back")), $"{part} over another at ({x}, {y})");
            }
    });

    [Property]
    public Property EveryRoomIsReachedFromTheWayInAndEveryHatchHasALadderUnderIt() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        Assert.Null(Unreached(vessel, Shipyard.Stamp(vessel)));
    });

    [Fact]
    public void ADeckWhoseHatchIsGoneIsUnreached()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var vessel = Shipyard.Describe(freighter, 7);
        Assert.Null(Unreached(vessel, Shipyard.Stamp(vessel)));
        var cut = vessel with { Hatches = vessel.Hatches.Skip(1).ToList() };
        Assert.NotNull(Unreached(cut, Shipyard.Stamp(cut)));
    }

    /// <summary>
    /// Why a hero could not reach every room of <paramref name="v"/> from its way in, by construction: a room no walk through open
    /// cells, doors and hatches from the stern door comes to, a hatch with no ladder under it, a machine standing on one; null
    /// when it could.
    /// </summary>
    private static string? Unreached(Vessel v, IReadOnlyList<VesselPart> parts)
    {
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        var inside = parts.Where(p => p.Layer == "back").Select(p => (p.X, p.Y)).ToHashSet();
        var seen = new HashSet<(int X, int Y)> { (v.Doors[0].X, v.Doors[0].Y) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.TryDequeue(out var c))
            foreach (var n in (ReadOnlySpan<(int, int)>)[(c.X + 1, c.Y), (c.X - 1, c.Y), (c.X, c.Y + 1), (c.X, c.Y - 1)])
                if (inside.Contains(n) && !walls.Contains(n) && seen.Add(n)) queue.Enqueue(n);
        foreach (var room in v.Compartments)
            if (!seen.Contains((room.Left, room.Top + Content.Vessels.Open - 1))) return $"{room.Role} on deck {room.Deck} at {room.Left} unreached";
        var machines = parts.Where(p => p.Layer == "machine").SelectMany(Shipyard.Covers).ToHashSet();
        foreach (var hatch in v.Hatches)
        {
            if (!parts.Any(p => p.Kind == "ladder" && p.X == hatch.X && p.Y == hatch.Y + 1)) return $"no ladder under the hatch at ({hatch.X}, {hatch.Y})";
            if (machines.Contains((hatch.X, hatch.Y - 1)) || machines.Contains((hatch.X + 1, hatch.Y - 1))) return $"a machine on the hatch at ({hatch.X}, {hatch.Y})";
        }
        return null;
    }

    [Property]
    public Property EveryVesselHasItsRoomsTheMainEngineByItsBellAndADoorBetweenEachTwoRooms() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        foreach (var role in (VesselRole[])[VesselRole.Engine, VesselRole.Bridge, VesselRole.Hold, VesselRole.Quarters])
            Assert.Contains(vessel.Compartments, c => c.Role == role);
        Assert.InRange(vessel.Decks.Count, 2, v.Class.Decks.Max);
        // One main engine, against the stern armour of the bottom deck, facing its bell.
        int start = v.Class.Bells + v.Class.ArmourDepth;
        var engine = Assert.Single(vessel.Fittings, f => Content.Vessels.SalvageOf(f.Kind)?.Kind == "salvageMainEngine");
        Assert.Equal((start, vessel.Decks[^1].Top), (engine.X, engine.Y));
        // A six-row door between each two rooms of a deck.
        var doors = parts.Where(p => p.Kind == "door").Select(p => (p.X, p.Y)).ToHashSet();
        foreach (var deck in vessel.Compartments.Where(c => c.Deck >= 0).GroupBy(c => c.Deck))
            foreach (var room in deck.Skip(1))
                for (int y = room.Top; y < room.Top + Content.Vessels.Open; y += 2) Assert.Contains((room.Left - 2, y), doors);
        Assert.InRange(vessel.Width - start - v.Class.Bow, 1, v.Class.Length);
    });

    [Property]
    public Property GlassIsOnlyTheBridgesOuterWallAndTouchesSpace() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        var glass = parts.Where(p => p.Material == Content.Vessels.Glass).ToList();
        Assert.NotEmpty(glass);
        var inside = parts.Where(p => p.Layer == "back").Select(p => (p.X, p.Y)).ToHashSet();
        var where = v.Class.BridgeKind == VesselBridge.Raised
            ? vessel.Sections.Single(s => s.Kind == Vessel.SectionKind.Bridge)
            : vessel.Sections.Single(s => s.Kind == Vessel.SectionKind.Bow);
        int a = v.Class.ArmourDepth;
        foreach (var g in glass)
        {
            Assert.InRange(g.X, where.Left, where.Left + where.Width - 1);
            Assert.False(inside.Contains((g.X, g.Y)), $"a back wall behind the glass at ({g.X}, {g.Y})");
            bool space = false;
            for (int k = 1; k <= a; k++)
                foreach (var (x, y) in (ReadOnlySpan<(int, int)>)[(g.X + k, g.Y), (g.X - k, g.Y), (g.X, g.Y + k), (g.X, g.Y - k)])
                    space |= !parts.Any(p => p.X == x && p.Y == y);
            Assert.True(space, $"glass at ({g.X}, {g.Y}) not on the outside");
        }
    });

    [Property]
    public Property ABreachTakesThingsWholeAndAddsOnlyWhatToreTheHull() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var intact = Shipyard.Stamp(vessel);
        var wrecked = Shipyard.Damage(intact, vessel, VesselCause.Breach);
        Assert.True(wrecked.Count < intact.Count);
        // Only what tore the hull is new: a torn salvage object, lying where nothing else is.
        var added = wrecked.Where(p => !intact.Contains(p)).ToList();
        Assert.All(added, p => Assert.True(Content.Vessels.SalvageOf(p.Kind) is { Torn: true }, p.Kind));
        Assert.InRange(added.Count, 0, 2);
        var taken = new HashSet<(int, int)>();
        foreach (var p in wrecked.Where(p => p.Layer != "back"))
            foreach (var c in Shipyard.Covers(p)) Assert.True(taken.Add(c), $"{p} over another at {c}");
    });

    // Content.

    [Fact]
    public void EveryInteriorIsSixRowsOfOpenCellsFloorsAndWhitelistedMachinesItsFirstTwoColumnsOpen()
    {
        foreach (var interior in Content.Vessels.Interiors)
        {
            Assert.Equal(Content.Vessels.Open, interior.Rows.Count);
            Assert.All(interior.Rows, r => Assert.Equal(interior.Width, r.Length));
            if (!interior.Main) Assert.All(interior.Rows, r => Assert.Equal("..", r[..2]));
            else Assert.Equal("salvageMainEngine", Assert.Single(Layout.Parse(interior.Rows).Machines(), m => m.Cell == 0).Kind);
            if (interior.Role == VesselRole.Bridge) Assert.Contains(Layout.Parse(interior.Rows).Machines(), m => m.Kind == "salvageConsole");
            foreach (char c in string.Concat(interior.Rows))
                Assert.True(c is '.' or '=' || Legend.MachineOf(c) is { } kind && Content.Vessels.Machines.Contains(kind), $"{interior.Id}: '{c}'");
            Assert.All(Layout.Parse(interior.Rows).Machines(), m => Assert.Contains(m.Kind, Content.Vessels.Machines));
        }
    }

    [Fact]
    public void AVesselsMachinesKeepNoStateElsewhereAndAreNeitherTheShipsOwnNorItsDearest()
    {
        foreach (var kind in Content.Vessels.Machines)
        {
            var spec = Machines.Get(kind);
            Assert.False(spec.Fixed, kind);
            Assert.Null(spec.Store);
            Assert.Null(spec.Makes);
        }
    }

    [Fact]
    public void EveryClassFurnishesEveryCompartmentAndFitsBesideAFullZone()
    {
        int beside = (Content.World.Width - Content.World.ZoneWidth) / 2;
        foreach (var cls in Content.Vessels.Classes)
        {
            Assert.True(cls.ArmourDepth >= 2, $"{cls.Id}: a door is two cells wide");
            Assert.True(cls.Frame.Width + Content.Vessels.Gap + Content.Vessels.EdgeClear <= beside, cls.Id);
            Assert.True(cls.Frame.Height + 2 * Content.Vessels.EdgeClear <= Content.World.Height, cls.Id);
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                Assert.Single(vessel.Compartments, c => c.Role == VesselRole.Bridge);
                Assert.Equal(cls.BridgeKind == VesselBridge.Raised, vessel.Compartments.Any(c => c.Deck < 0));
            }
        }
    }

    [Fact]
    public void NoDroneBayOfTheShipReachesAVessel() => Assert.True(Content.Vessels.Gap > Content.Logistics.Reach);

    [Fact]
    public void SalvageGivesTimesItsPiecesMeanLootAndKeepsTheCircuitRare()
    {
        Assert.Equal([("canister", 12), ("scrapChunk", 5)], Machines.Get("salvageTanks").Cost);
        Assert.Equal([("planks", 50), ("scrapChunk", 7), ("gear", 1), ("wire", 3)], Machines.Get("salvageContainer").Cost);
        Assert.Equal([("wire", 40), ("scrapChunk", 7), ("circuit", 1)], Machines.Get("salvageRack").Cost);
        foreach (var s in Content.Vessels.Salvage)
        {
            var spec = Machines.Get(s.Kind);
            foreach (var e in Loot.Of(s.From)!)
            {
                int odds = e.OneIn ?? 1;
                int want = Content.Vessels.Unmultiplied.Contains(e.Item) ? (e.Min + e.Max + 2 * odds - 1) / (2 * odds) : s.Times * (e.Min + e.Max) / (2 * odds);
                Assert.Equal(want, spec.Cost.Where(c => c.Item == e.Item).Sum(c => c.Count));
            }
            Assert.Equal(s.Hp ?? Wreckage.HpOf(s.From), spec.Hp);
            if (s.Size == null) Assert.Contains((s.From, spec.Width, spec.Height), Wreckage.Sizes);
            Assert.InRange(spec.Height, 1, Content.Vessels.Open);
            Assert.Null(spec.Menu);
            Assert.Contains(s.Kind, Content.Vessels.Machines);
        }
    }

    [Fact]
    public void ASalvageOfSeveralLooksWearsEachOnSomeVesselAndEveryLookIsAlike()
    {
        foreach (var s in Content.Vessels.Salvage.Where(s => s.Looks > 1))
        {
            var first = Machines.Get(s.Kind);
            var looks = Enumerable.Range(0, s.Looks).Select(look => Content.Vessels.LookKind(s, look)).ToList();
            foreach (var kind in looks)
            {
                var spec = Machines.Get(kind);
                Assert.Equal((first.Width, first.Height, first.Hp, first.Cost), (spec.Width, spec.Height, spec.Hp, spec.Cost));
                Assert.Same(s, Content.Vessels.SalvageOf(kind));
            }
            var worn = new HashSet<string>();
            foreach (var cls in Content.Vessels.Classes)
                for (uint seed = 1; seed <= 40; seed++)
                    foreach (var f in Shipyard.Describe(cls, seed).Fittings) worn.Add(f.Kind);
            Assert.All(looks, kind => Assert.Contains(kind, worn));
        }
    }

    [Fact]
    public void EveryRoleHoldsSalvageOfItsOwnAndEverySalvageOfARoleStandsInSomeInterior()
    {
        var drawn = Content.Vessels.Interiors.SelectMany(i => Layout.Parse(i.Rows).Machines().Select(m => (i.Role, m.Kind))).ToList();
        foreach (var s in Content.Vessels.Salvage.Where(s => s.Roles.Count > 0))
            Assert.Contains(drawn, d => d.Kind == s.Kind && s.Roles.Contains(d.Role));
        foreach (var role in Enum.GetValues<VesselRole>())
            Assert.Contains(drawn, d => d.Role == role && Content.Vessels.SalvageOf(d.Kind) != null);
    }

    [Fact]
    public void EachClassGivesWhatItsBandSaysTakenApartWhole()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
                Assert.InRange(Yield(Shipyard.Make(cls, seed, VesselCause.None)), cls.Yield.Min, cls.Yield.Max);
    }

    /// <summary>Whole items <paramref name="parts"/> give taken apart, every kind counted alike: walls, back walls, pieces, machines, what crates hold.</summary>
    private static int Yield(IReadOnlyList<VesselPart> parts) => parts.Sum(p =>
        p.Layer == "machine" ? Machines.Get(p.Kind).Cost.Sum(c => c.Count) + (p.Holds?.Sum(h => h.Count) ?? 0)
        : p.Kind is "wall" or "backWall" ? Materials.Get(p.Material!).Cost.Sum(c => c.Count)
        : Builds.Get(p.Kind).Cost.Sum(c => c.Count));
}
