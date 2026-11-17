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
        foreach (var chain in (IReadOnlyList<WreckStep>?[])[null, [new(WreckKind.Explosion), new(WreckKind.Fire), new(WreckKind.Asteroid)], [new(WreckKind.Break, true)]])
            Assert.Equal(Key(Shipyard.Build(v.Class, v.Seed, chain).Parts), Key(Shipyard.Build(v.Class, v.Seed, chain).Parts));
    });

    [Fact]
    public void SeedsMakeVesselsThatDiffer()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var made = Enumerable.Range(1, 8).Select(s => Key(Shipyard.Build(freighter, (uint)s).Parts)).Distinct().Count();
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
    internal static string? Unreached(Vessel v, IReadOnlyList<VesselPart> parts)
    {
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        var inside = parts.Where(p => p.Layer == "back").Select(p => (p.X, p.Y)).ToHashSet();
        var seen = new HashSet<(int X, int Y)> { (v.Doors[0].X, v.Doors[0].Y) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.TryDequeue(out var c))
            foreach (var n in (ReadOnlySpan<(int, int)>)[(c.X + 1, c.Y), (c.X - 1, c.Y), (c.X, c.Y + 1), (c.X, c.Y - 1)])
                if (inside.Contains(n) && !walls.Contains(n) && seen.Add(n)) queue.Enqueue(n);
        foreach (var room in v.Compartments)
            if (!seen.Contains((room.Left, room.Floor - 1))) return $"{room.Role} on deck {room.Deck} at {room.Left} unreached";
        var machines = parts.Where(p => p.Layer == "machine").SelectMany(Shipyard.Covers).ToHashSet();
        foreach (var hatch in v.Hatches)
        {
            if (!parts.Any(p => p.Kind == "ladder" && p.X == hatch.X && p.Y == hatch.Y + 1)) return $"no ladder under the hatch at ({hatch.X}, {hatch.Y})";
            if (machines.Contains((hatch.X, hatch.Y - 1)) || machines.Contains((hatch.X + 1, hatch.Y - 1))) return $"a machine on the hatch at ({hatch.X}, {hatch.Y})";
        }
        // Nor on a ladder (big-vessels): the way down stays clear.
        foreach (var ladder in parts.Where(p => p.Kind == "ladder"))
            foreach (var c in Shipyard.Covers(ladder))
                if (machines.Contains(c)) return $"a machine on the ladder at {c}";
        return null;
    }

    [Property]
    public Property EveryVesselHasItsRoomsAMainEngineOnEveryLowerDeckAndADoorBetweenEachTwoRooms() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        foreach (var role in (VesselRole[])[VesselRole.Engine, VesselRole.Bridge, VesselRole.Hold, VesselRole.Quarters])
            Assert.Contains(vessel.Compartments, c => c.Role == role);
        Assert.InRange(vessel.Decks.Count, 2, v.Class.Decks.Max);
        // A main engine on every deck below the first, against the stern armour, by its nozzle; a liner's over its bottom band.
        int start = Section(vessel, Vessel.SectionKind.Body).Left + v.Class.ArmourDepth;
        var engines = vessel.Fittings.Where(f => Content.Vessels.SalvageOf(f.Kind)?.Kind == "salvageMainEngine").Select(f => (f.X, f.Y));
        int low = v.Class.Layout == VesselLayout.Liner ? Content.Vessels.Liner.LowDecks : 0;
        Assert.Equal(vessel.Decks.Skip(1).SkipLast(low).Select(d => (start, d.Top)), engines);
        if (v.Class.Layout != VesselLayout.Rooms) return;
        // A six-row door between each two rooms of a deck.
        var doors = parts.Where(p => p.Kind == "door").Select(p => (p.X, p.Y)).ToHashSet();
        foreach (var deck in vessel.Compartments.Where(c => c.Deck >= 0).GroupBy(c => c.Deck))
            foreach (var room in deck.Skip(1))
                for (int y = room.Top; y < room.Top + Content.Vessels.Open; y += 2) Assert.Contains((room.Left - 2, y), doors);
        Assert.InRange(vessel.Width - start - Section(vessel, Vessel.SectionKind.Bow).Width, 1, v.Class.Length);
    });

    [Property]
    public Property GlassIsOnlyTheBridgesOuterWallAndTouchesSpace() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        var glass = parts.Where(p => p.Material == Content.Vessels.Glass).ToList();
        Assert.NotEmpty(glass);
        var inside = parts.Where(p => p.Layer == "back").Select(p => (p.X, p.Y)).ToHashSet();
        // A raised bridge's, a liner's promenade's and dome's too (big-vessels); else the cockpit's in the bow.
        var where = v.Class.BridgeKind == VesselBridge.Raised
            ? vessel.Sections.Where(s => s.Kind is Vessel.SectionKind.Bridge or Vessel.SectionKind.Tier).ToList()
            : [vessel.Sections.Single(s => s.Kind == Vessel.SectionKind.Bow)];
        int a = v.Class.ArmourDepth;
        foreach (var g in glass)
        {
            Assert.Contains(where, s => g.X >= s.Left && g.X < s.Left + s.Width && g.Y >= s.Top && g.Y <= s.Bottom);
            Assert.False(inside.Contains((g.X, g.Y)), $"a back wall behind the glass at ({g.X}, {g.Y})");
            bool space = false;
            for (int k = 1; k <= a; k++)
                foreach (var (x, y) in (ReadOnlySpan<(int, int)>)[(g.X + k, g.Y), (g.X - k, g.Y), (g.X, g.Y + k), (g.X, g.Y - k)])
                    space |= !parts.Any(p => p.X == x && p.Y == y);
            Assert.True(space, $"glass at ({g.X}, {g.Y}) not on the outside");
        }
    });

    // Shape (derelict-looks).

    /// <summary>The one section of <paramref name="kind"/>.</summary>
    private static Vessel.Section Section(Vessel v, Vessel.SectionKind kind) => v.Sections.Single(s => s.Kind == kind);

    /// <summary>The sections of <paramref name="kind"/>, stern to bow.</summary>
    private static List<Vessel.Section> All(Vessel v, Vessel.SectionKind kind) => [.. v.Sections.Where(s => s.Kind == kind).OrderBy(s => s.Left)];

    /// <summary>The cells of <paramref name="parts"/> of <paramref name="layer"/>, or of every layer but the back for null.</summary>
    private static HashSet<(int X, int Y)> CellsOf(IReadOnlyList<VesselPart> parts, string? layer = null) =>
        parts.Where(p => layer == null ? p.Layer != "back" : p.Layer == layer).SelectMany(Shipyard.Covers).ToHashSet();

    [Property]
    public Property NothingStandsLeftOfTheSternButANozzleADeckBelowTheFirstAgainstItsArmourAndCentredOnItsDeck() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        int stern = Section(vessel, Vessel.SectionKind.Body).Left;
        var size = Content.Vessels.NozzleSize;
        // No bell: left of the stern armour only the nozzles' cells.
        Assert.All(parts.Where(p => p.X < stern), p => Assert.Equal(("machine", Content.Vessels.Nozzle), (p.Layer, p.Kind)));
        var nozzles = parts.Where(p => p.Kind == Content.Vessels.Nozzle).ToList();
        // One by each engine room: a deck below the first, a liner's over its bottom band.
        var engineDecks = vessel.Decks.Skip(1).SkipLast(v.Class.Layout == VesselLayout.Liner ? Content.Vessels.Liner.LowDecks : 0).ToList();
        Assert.Equal(engineDecks.Count, nozzles.Count);
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        foreach (var deck in engineDecks)
        {
            var nozzle = Assert.Single(nozzles, n => n.Y == deck.Top);
            Assert.Equal(stern, nozzle.X + size.W);
            // Centred on its deck: its open rows and its floor, the stern's armour beside every row.
            Assert.Equal(deck.Floor, nozzle.Y + size.H - 1);
            for (int y = nozzle.Y; y < nozzle.Y + size.H; y++) Assert.Contains((stern, y), walls);
        }
    });

    [Property]
    public Property EveryOutsidePartStandsInSpaceAgainstTheHull() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        Assert.NotEmpty(vessel.Outside);
        Assert.Null(Unheld(vessel));
    });

    [Fact]
    public void AManifoldWithItsStrutsGoneOrAPylonOffItsColumnHoldsNothing()
    {
        var vessel = Shipyard.Describe(Content.Vessels.Class("tanker")!, 5);
        Assert.Null(Unheld(vessel));
        var bare = vessel with { Outside = [.. vessel.Outside.Where(f => Content.Vessels.FirstLook(f.Kind) is not (Content.Vessels.Pylon or Content.Vessels.Riser))] };
        Assert.NotNull(Unheld(bare));
        var pylon = vessel.Outside.First(f => f.Kind == Content.Vessels.Pylon);
        var lifted = vessel with { Outside = [.. vessel.Outside.Select(f => f == pylon ? f with { Y = f.Y - 1 } : f)] };
        Assert.NotNull(Unheld(lifted));
    }

    [Fact]
    public void ASphereHungUnderTheDeepestHullOverItsWidthWithAStrutShortOfTheHullHoldsNothing()
    {
        // The old placement: under the hull's lowest row over all its columns. Where a deeper module stands over its middle, a strut's
        // column ends rows higher and the strut hangs short of it.
        int floating = 0;
        var tanker = Content.Vessels.Class("tanker")!;
        var w = Machines.Get(Content.Vessels.Sphere).Width;
        for (uint seed = 1; seed <= 64; seed++)
        {
            var vessel = Shipyard.Describe(tanker, seed);
            var sphere = vessel.Outside.FirstOrDefault(f => Content.Vessels.FirstLook(f.Kind) == Content.Vessels.Sphere);
            if (sphere == null) continue;
            Assert.Null(Unheld(vessel));
            var walls = Shipyard.Stamp(vessel).Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
            int Bottom(int x) => walls.Where(c => c.X == x).Max(c => c.Y);
            var bay = vessel.Compartments.First(c => c.Role == VesselRole.TankBay && c.Left <= sphere.X && sphere.X < c.Left + c.Width);
            int mid = bay.Left + (bay.Width - w) / 2, y = Enumerable.Range(mid, w).Max(Bottom) + 1;
            if (Content.Vessels.SpherePosts.All(p => Bottom(mid + p) == y - 1)) continue;
            floating++;
            var old = vessel with { Outside = [.. vessel.Outside.Select(f => f == sphere ? f with { X = mid, Y = y } : f)] };
            Assert.NotNull(Unheld(old));
        }
        Assert.True(floating > 0, "no sphere where the old placement left a strut short");
    }

    /// <summary>
    /// Why a part outside <paramref name="v"/> is not held, null when every one is: each stands in space; the truss in one line, the
    /// manifold in another (big-vessels); a strut, a column of pylons or of risers, runs from the hull down to a truss segment's post or up
    /// to a manifold segment; each manifold segment on a strut, each valve on the manifold over its riser; a container wholly under its
    /// truss segment; every other part against the hull or a strut.
    /// </summary>
    private static string? Unheld(Vessel v)
    {
        var parts = Shipyard.Stamp(v);
        var hull = CellsOf(parts, "structure");
        hull.UnionWith(CellsOf(parts, "back"));
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        var trusses = v.Outside.Where(f => f.Kind == Content.Vessels.Truss).ToList();
        var manifolds = v.Outside.Where(f => f.Kind == Content.Vessels.Manifold).ToList();
        var (truss, manifold) = (Machines.Get(Content.Vessels.Truss), Machines.Get(Content.Vessels.Manifold));
        string valve = Content.Vessels.LookKind(Content.Vessels.Riser, 1);
        if (trusses.Select(t => t.Y).Distinct().Count() > 1) return "a stepped truss";
        if (manifolds.Select(t => t.Y).Distinct().Count() > 1) return "a stepped manifold";
        var struts = v.Outside.Where(f => f.Kind is Content.Vessels.Pylon or Content.Vessels.Riser).ToList();
        var strutCells = struts.Select(f => (f.X, f.Y)).ToHashSet();
        foreach (var s in struts)
        {
            int top = s.Y, bottom = s.Y;
            while (strutCells.Contains((s.X, top - 1))) top--;
            while (strutCells.Contains((s.X, bottom + 1))) bottom++;
            bool hangs = walls.Contains((s.X, top - 1)) && trusses.Exists(t => t.Y == bottom + 1 && Content.Vessels.TrussPosts.Contains(s.X - t.X));
            bool stands = walls.Contains((s.X, bottom + 1)) && manifolds.Exists(m => m.Y + manifold.Height == top && s.X >= m.X && s.X < m.X + manifold.Width);
            if (!hangs && !stands) return $"a strut at ({s.X}, {s.Y}) holds nothing";
        }
        foreach (var m in manifolds)
            if (!struts.Exists(s => s.Y == m.Y + manifold.Height && s.X >= m.X && s.X < m.X + manifold.Width)) return $"a manifold at ({m.X}, {m.Y}) on no strut";
        bool Touches((int X, int Y) c) =>
            ((IEnumerable<(int, int)>)[(c.X + 1, c.Y), (c.X - 1, c.Y), (c.X, c.Y + 1), (c.X, c.Y - 1)]).Any(n => walls.Contains(n) || strutCells.Contains(n));
        foreach (var f in v.Outside)
        {
            var cells = Shipyard.Covers(new VesselPart(f.X, f.Y, "machine", f.Kind)).ToList();
            if (cells.Exists(hull.Contains)) return $"{f.Kind} at ({f.X}, {f.Y}) over the hull";
            bool held = Content.Vessels.FirstLook(f.Kind) == Content.Vessels.TrussContainer
                ? trusses.Exists(t => t.Y + truss.Height == f.Y && t.X <= f.X && f.X + Machines.Get(f.Kind).Width <= t.X + truss.Width)
                : f.Kind == valve
                ? manifolds.Exists(m => m.Y == f.Y + 1 && f.X >= m.X && f.X < m.X + manifold.Width) && strutCells.Contains((f.X, f.Y + 1 + manifold.Height))
                : Content.Vessels.FirstLook(f.Kind) == Content.Vessels.Sphere
                ? Content.Vessels.SpherePosts.All(p => walls.Contains((f.X + p, f.Y - 1)))
                : f.Kind == Content.Vessels.Manifold || cells.Exists(Touches);
            if (!held) return $"{f.Kind} at ({f.X}, {f.Y}) holds to nothing";
        }
        return null;
    }

    [Property]
    public Property EachClassCarriesItsOutsideParts() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        int Count(string kind) => vessel.Outside.Count(f => Content.Vessels.FirstLook(f.Kind) == kind);
        Assert.Equal(4, Count(Content.Vessels.Rcs));
        Assert.Equal(1, Count(Content.Vessels.Dish));
        if (v.Class.Id == "shuttle")
        {
            Assert.Equal((1, 0, 1, 1), (Count(Content.Vessels.Mast), Count(Content.Vessels.MastTall), Count(Content.Vessels.Solar), Count(Content.Vessels.Tank)));
            Assert.Equal(0, Count(Content.Vessels.Radiator) + Count(Content.Vessels.Collar) + Count(Content.Vessels.Truss) + Count(Content.Vessels.Pylon) + Count(Content.Vessels.Boom));
            return;
        }
        Assert.Equal((0, 1, 0, 0), (Count(Content.Vessels.Mast), Count(Content.Vessels.MastTall), Count(Content.Vessels.Solar), Count(Content.Vessels.Tank)));
        int liner = Count(Content.Vessels.Fin) + Count(Content.Vessels.Pod) + Count(Content.Vessels.Cradle);
        int tanker = Count(Content.Vessels.Manifold) + Count(Content.Vessels.Riser) + Count(Content.Vessels.Dome) + Count(Content.Vessels.Sphere);
        int freighter = Count(Content.Vessels.Collar) + Count(Content.Vessels.Truss) + Count(Content.Vessels.TrussContainer);
        if (v.Class.Layout == VesselLayout.Liner)
        {
            // Its fin on the service block, its deck's second room; a radiator aft; pods or their cradles; no boom, the strobe at the bow's tip.
            Assert.Equal((1, 1, 0, 0, 0), (Count(Content.Vessels.Fin), Count(Content.Vessels.Radiator), tanker, freighter, Count(Content.Vessels.Boom)));
            var service = vessel.Compartments.Where(c => c.Deck == 0).OrderBy(c => c.Left).ElementAt(1);
            Assert.Equal(service.Left + Content.Vessels.Fits.FinInto, vessel.Outside.Single(f => f.Kind == Content.Vessels.Fin).X);
            Assert.True(Count(Content.Vessels.Pod) + Count(Content.Vessels.Cradle) > 0, "no pods");
            // None on or by a raised room or the housing.
            var raised = vessel.Sections.Where(s => s.Kind is Vessel.SectionKind.Bridge or Vessel.SectionKind.Tier or Vessel.SectionKind.Housing).ToList();
            Assert.All(vessel.Outside.Where(f => f.Kind is Content.Vessels.Pod or Content.Vessels.Cradle), f =>
                Assert.DoesNotContain(raised, s => f.X < s.Left + s.Width + Content.Vessels.PartGap && f.X + Machines.Get(f.Kind).Width + Content.Vessels.PartGap > s.Left));
        }
        else if (v.Class.Layout == VesselLayout.Tanker)
        {
            // A radiator, the manifold, a dome on each tank bay, a sphere under each or none, the boom.
            int bays = vessel.Compartments.Where(c => c.Role == VesselRole.TankBay).Select(c => c.Left).Distinct().Count();
            Assert.Equal((1, 1, 0, 0), (Count(Content.Vessels.Radiator), Count(Content.Vessels.Boom), liner, freighter));
            Assert.True(Count(Content.Vessels.Manifold) > 0);
            Assert.Equal(bays, Count(Content.Vessels.Dome));
            Assert.InRange(Count(Content.Vessels.Sphere), 0, bays);
        }
        else
        {
            Assert.Equal((0, 0), (liner, tanker));
            Assert.InRange(Count(Content.Vessels.Radiator), Content.Vessels.Radiators.Min, Content.Vessels.Radiators.Max);
            Assert.Equal((1, 1), (Count(Content.Vessels.Collar), Count(Content.Vessels.Boom)));
            Assert.True(Count(Content.Vessels.Truss) > 0);
            Assert.InRange(Count(Content.Vessels.TrussContainer), 0, Count(Content.Vessels.Truss));
        }
        // The mast and the dish on the bridge's roof; the boom from the bow's tip, its middle rows.
        var bridge = Section(vessel, Vessel.SectionKind.Bridge);
        Assert.All(vessel.Outside.Where(f => f.Kind is Content.Vessels.MastTall or Content.Vessels.Dish), f =>
        {
            Assert.Equal(bridge.Top, f.Y + Machines.Get(f.Kind).Height);
            Assert.InRange(f.X, bridge.Left, bridge.Left + bridge.Width - Machines.Get(f.Kind).Width);
        });
        if (v.Class.Layout == VesselLayout.Liner) return;
        var bow = Section(vessel, Vessel.SectionKind.Bow);
        var boom = vessel.Outside.Single(f => f.Kind == Content.Vessels.Boom);
        var (top, bottom) = bow.RowsAt(bow.Left + bow.Width - 1);
        Assert.Equal((bow.Left + bow.Width, top + bottom + 1), (boom.X, 2 * boom.Y + Machines.Get(Content.Vessels.Boom).Height));
    });

    [Fact]
    public void SomeTrussSegmentsHoldNoContainerAndMostDo()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        int segments = 0, held = 0;
        for (uint seed = 1; seed <= 40; seed++)
        {
            var vessel = Shipyard.Describe(freighter, seed);
            segments += vessel.Outside.Count(f => f.Kind == Content.Vessels.Truss);
            held += vessel.Outside.Count(f => Content.Vessels.FirstLook(f.Kind) == Content.Vessels.TrussContainer);
        }
        // Four in five, roughly.
        Assert.InRange(held * Content.Vessels.TrussEmptyOneIn, segments * 3, segments * Content.Vessels.TrussEmptyOneIn - 1);
    }

    [Fact]
    public void UnderTheKeelHangOnlyCargoContainersEveryColourOnSomeAndTheGreenOneStandsOnlyInside()
    {
        var colours = new HashSet<string>();
        var looks = Enumerable.Range(0, Content.Vessels.ContainerColours).Select(k => Content.Vessels.LookKind(Content.Vessels.Container, k)).ToList();
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                Assert.DoesNotContain(vessel.Outside, f => f.Kind == "salvageContainer");
                foreach (var f in vessel.Outside.Where(f => Content.Vessels.FirstLook(f.Kind) == Content.Vessels.Container))
                {
                    Assert.Contains(f.Kind, looks);
                    colours.Add(f.Kind);
                }
            }
        Assert.Equal(looks.Order(), colours.Order());
        Assert.Equal(Content.Vessels.Container, Content.Vessels.TrussContainer);
        Assert.Contains(Content.Vessels.Interiors, i => Layout.Parse(i.Rows).Machines().Any(m => m.Kind == "salvageContainer"));
        // A colour is a look: its row alike but for its kind.
        var first = Machines.Get(Content.Vessels.Container);
        Assert.All(looks, k => Assert.Equal((first.Width, first.Height, first.Hp, first.Cost), (Machines.Get(k).Width, Machines.Get(k).Height, Machines.Get(k).Hp, Machines.Get(k).Cost)));
    }

    [Fact]
    public void NoSoilClodStandsOnAnyVessel()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 200; seed++)
                foreach (var chain in (IReadOnlyList<WreckStep>?[])[null, [new(WreckKind.Explosion), new(WreckKind.Asteroid)]])
                    Assert.DoesNotContain(Shipyard.Build(cls, seed, chain).Parts, p => p.Layer == "machine" && Content.Vessels.SalvageOf(p.Kind)?.From == "soilClod");
        Assert.DoesNotContain(string.Concat(Content.Vessels.Interiors.SelectMany(i => i.Rows)), c => c == 'z');
    }

    [Fact]
    public void EveryRoomHasTheDerelictsLampOnItsCeilingAndNoneIsTheShipsLamp()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                var machines = Shipyard.Stamp(vessel).Where(p => p.Layer == "machine").ToList();
                Assert.DoesNotContain(machines, p => p.Kind is "lamp" or "emergencyLamp" or "arcLamp");
                // A liner's restaurant hangs chandeliers (big-vessels), a lamp of the vessel's.
                string[] lamps = [Content.Vessels.Lamp, Content.Vessels.Chandelier];
                Assert.All(machines.Where(p => Machines.Is(p.Kind, MachineTrait.Hangs)), p => Assert.Contains(p.Kind, lamps));
                foreach (var c in vessel.Compartments)
                    Assert.Contains(machines, p => lamps.Contains(p.Kind) && p.Y == c.Top && p.X >= c.Left && p.X < c.Left + c.Width);
            }
        var lamp = Machines.Get(Content.Vessels.Lamp);
        Assert.Equal((2, 1, false), (lamp.Width, lamp.Height, lamp.Powered));
        Assert.True(lamp.Is(MachineTrait.Glow));
    }

    [Fact]
    public void EachPieceOfFurnitureStandsInRoomsOfItsRoleOnSomeVessels()
    {
        var roles = new Dictionary<string, VesselRole[]>
        {
            [Content.Vessels.Bunk] = [VesselRole.Quarters],
            [Content.Vessels.Locker] = [VesselRole.Quarters],
            [Content.Vessels.Table] = [VesselRole.Quarters],
            [Content.Vessels.LifeTank] = [VesselRole.Life],
            [Content.Vessels.Machine] = [VesselRole.Engine],
            [Content.Vessels.Rack] = [VesselRole.Engine, VesselRole.Pipeway],
            [Content.Vessels.Console] = [VesselRole.Bridge],
            [Content.Vessels.Seat] = [VesselRole.Bridge],
            [Content.Vessels.Cargo] = [VesselRole.Hold, VesselRole.Hangar],
            // A liner's (big-vessels).
            [Content.Vessels.Bar] = [VesselRole.Restaurant],
            [Content.Vessels.Piano] = [VesselRole.Restaurant],
            [Content.Vessels.Chandelier] = [VesselRole.Restaurant],
            [Content.Vessels.RoundTable] = [VesselRole.Restaurant, VesselRole.Lounge],
            [Content.Vessels.Chair] = [VesselRole.Restaurant, VesselRole.Lounge],
            [Content.Vessels.Lounger] = [VesselRole.Lounge, VesselRole.Pool],
            [Content.Vessels.Pool] = [VesselRole.Pool],
            [Content.Vessels.Diving] = [VesselRole.Pool],
            [Content.Vessels.Stove] = [VesselRole.Galley],
            [Content.Vessels.Counter] = [VesselRole.Galley],
            [Content.Vessels.Bed] = [VesselRole.Infirmary],
            [Content.Vessels.MedCab] = [VesselRole.Infirmary],
            [Content.Vessels.FuelCart] = [VesselRole.Hangar],
            [Content.Vessels.Capsule] = [VesselRole.FuelBay, VesselRole.TankBay],
            // A tanker's.
            [Content.Vessels.Pump] = [VesselRole.PumpRoom],
            [Content.Vessels.Valves] = [VesselRole.PumpRoom, VesselRole.Pipeway],
            [Content.Vessels.TankTall] = [VesselRole.TankBay],
            [Content.Vessels.FuelColumn3] = [VesselRole.FuelBay],
            [Content.Vessels.FuelColumn4] = [VesselRole.FuelBay],
            [Content.Vessels.ShuttleGull] = [VesselRole.Hangar],
            [Content.Vessels.ShuttleMule] = [VesselRole.Hangar],
            [Content.Vessels.ShuttleBeetle] = [VesselRole.Hangar],
            [Content.Vessels.ShuttleWhale] = [VesselRole.Hangar],
            [Content.Vessels.ShuttleHeron] = [VesselRole.Hangar],
        };
        // A body in a suit lies only where a wreck left it (vessel-wrecks), in no room of its own.
        Assert.Equal(Content.Vessels.Furniture.Select(r => Content.Vessels.FirstLook(r.Kind)).Distinct().Where(k => k != Content.Vessels.Lamp && k != Content.Vessels.Body).Order(), roles.Keys.Order());
        var seen = new HashSet<string>();
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                foreach (var f in vessel.Fittings)
                {
                    string kind = Content.Vessels.FirstLook(f.Kind);
                    if (!roles.TryGetValue(kind, out var allowed)) continue;
                    var room = vessel.Compartments.Single(c => c.Holds(f.X, f.Y));
                    Assert.Contains(room.Role, allowed);
                    seen.Add(f.Kind);
                }
            }
        // Every piece, and every colour of the hold's cargo container.
        Assert.All(roles.Keys, k => Assert.Contains(k, seen));
        Assert.All(Enumerable.Range(0, Content.Vessels.ContainerColours), k => Assert.Contains(Content.Vessels.LookKind(Content.Vessels.Cargo, k), seen));
    }

    [Fact]
    public void ARadiatorCountFromOneToThree()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var counts = Enumerable.Range(1, 40).Select(s => Shipyard.Describe(freighter, (uint)s).Outside.Count(f => f.Kind == Content.Vessels.Radiator)).ToHashSet();
        Assert.Equal([1, 2, 3], counts.Order());
    }

    [Property]
    public Property ModulesBulgeUpToTwoRowsTheSternsAtLeastOneNoneUpUnderTheBridgeAndARibAtEachBoundary() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var body = Section(vessel, Vessel.SectionKind.Body);
        var bow = Section(vessel, Vessel.SectionKind.Bow);
        var modules = All(vessel, Vessel.SectionKind.Module);
        // Modules run from the stern to the bow, edge to edge.
        Assert.Equal(body.Left, modules[0].Left);
        Assert.Equal(bow.Left, modules[^1].Left + modules[^1].Width);
        for (int i = 1; i < modules.Count; i++) Assert.Equal(modules[i - 1].Left + modules[i - 1].Width, modules[i].Left);
        var bridge = vessel.Sections.SingleOrDefault(s => s.Kind == Vessel.SectionKind.Bridge);
        var housing = vessel.Sections.SingleOrDefault(s => s.Kind == Vessel.SectionKind.Housing);
        int calm = housing?.Left ?? bridge?.Left ?? 0, calmEnd = bridge == null ? 0 : bridge.Left + bridge.Width + v.Class.ArmourDepth;
        for (int i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            int up = body.Top - m.Top, down = m.Bottom - body.Bottom;
            var range = i == 0 ? Content.Vessels.SternBulge : Content.Vessels.Bulge;
            Assert.InRange(down, range.Min, range.Max);
            bool under = bridge != null && m.Left < calmEnd && m.Left + m.Width > calm;
            if (under) Assert.Equal(0, up);
            else Assert.InRange(up, range.Min, range.Max);
        }
        var ribs = All(vessel, Vessel.SectionKind.Rib);
        Assert.Equal(modules.Count - 1, ribs.Count);
        for (int i = 1; i < modules.Count; i++)
        {
            var (before, after, rib) = (modules[i - 1], modules[i], ribs[i - 1]);
            Assert.Equal((after.Left - 1, Content.Vessels.RibWidth), (rib.Left, rib.Width));
            Assert.Equal(Math.Min(before.Top, after.Top) - 1, rib.Top);
            Assert.Equal(Math.Max(before.Bottom, after.Bottom) + 1, rib.Bottom);
        }
        // The boundaries: past the aft rooms, then every so many columns, none nearer the bow than its stop.
        if (modules.Count > 1)
        {
            int aft = vessel.Compartments.Where(c => c.Deck >= 0 && c.Left == body.Left + v.Class.ArmourDepth).Min(c => c.Width);
            Assert.Equal(body.Left + v.Class.ArmourDepth + aft + 1, modules[1].Left);
        }
        foreach (var m in modules.Skip(1).SkipLast(1)) Assert.InRange(m.Width, Content.Vessels.ModuleEvery.Min, Content.Vessels.ModuleEvery.Max);
        Assert.All(modules.Skip(1), m => Assert.True(m.Left <= bow.Left - Content.Vessels.ModuleStop, $"a boundary at {m.Left}"));
    });

    [Property]
    public Property ModulesRibsAndTheHousingAreSolidHullAndLeaveEveryRoomAsItWas() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        var backs = CellsOf(parts, "back");
        var rooms = vessel.Sections.Where(s => s.Kind is Vessel.SectionKind.Body or Vessel.SectionKind.Bow or Vessel.SectionKind.Bridge or Vessel.SectionKind.Tier).ToList();
        bool Rooms(int x, int y) => rooms.Any(s => x >= s.Left && x < s.Left + s.Width && y >= s.RowsAt(x).Top && y <= s.RowsAt(x).Bottom);
        foreach (var s in vessel.Sections.Where(s => s.Kind is Vessel.SectionKind.Module or Vessel.SectionKind.Rib or Vessel.SectionKind.Housing))
            for (int y = s.Top; y <= s.Bottom; y++)
                for (int x = s.Left; x < s.Left + s.Width; x++)
                {
                    Assert.Contains((x, y), backs);
                    if (!Rooms(x, y)) Assert.True(walls.Contains((x, y)), $"{s.Kind} at ({x}, {y}) not solid");
                }
        // Every room's open rows: a back wall, no wall of the hull in any of them.
        foreach (var c in vessel.Compartments)
            for (int y = c.Top; y < c.Floor; y++)
                for (int x = c.Left; x < c.Left + c.Width; x++)
                {
                    Assert.Contains((x, y), backs);
                    Assert.DoesNotContain((x, y), walls);
                }
    });

    [Fact]
    public void AFreightersHousingStandsBehindItsBridgeDownToTheBodyUnlessItWouldReachTheAftRooms()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        int housed = 0;
        for (uint seed = 1; seed <= 40; seed++)
        {
            var vessel = Shipyard.Describe(freighter, seed);
            var bridge = Section(vessel, Vessel.SectionKind.Bridge);
            var body = Section(vessel, Vessel.SectionKind.Body);
            int aftEnd = body.Left + freighter.ArmourDepth + vessel.Compartments.Where(c => c.Deck >= 0 && c.Left == body.Left + freighter.ArmourDepth).Min(c => c.Width);
            var housing = vessel.Sections.SingleOrDefault(s => s.Kind == Vessel.SectionKind.Housing);
            Assert.Equal(bridge.Left - Content.Vessels.HousingWidth >= aftEnd, housing != null);
            if (housing == null) continue;
            housed++;
            Assert.Equal((bridge.Left - Content.Vessels.HousingWidth, Content.Vessels.HousingWidth), (housing.Left, housing.Width));
            Assert.Equal((bridge.Top + Content.Vessels.HousingDrop, body.Top - 1), (housing.Top, housing.Bottom));
        }
        Assert.True(housed > 20, $"{housed} of 40 housed");
        Assert.All(Content.Vessels.Classes.Where(c => c.BridgeKind == VesselBridge.Cockpit), c =>
            Assert.DoesNotContain(Shipyard.Describe(c, 1).Sections, s => s.Kind == Vessel.SectionKind.Housing));
    }

    [Property]
    public Property TheBowKeepsItsHeightThenStepsARowEveryRunToItsTip() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var parts = Shipyard.Stamp(vessel);
        var bow = Section(vessel, Vessel.SectionKind.Bow);
        var backs = CellsOf(parts, "back");
        backs.UnionWith(parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)));
        Assert.InRange(bow.Run, v.Class.BowRun.Min, v.Class.BowRun.Max);
        // The bow ends the hull: past it only a boom.
        int tip = bow.Left + bow.Width;
        Assert.Equal(vessel.Width, tip + vessel.Outside.Where(f => f.Kind == Content.Vessels.Boom).Sum(f => Machines.Get(f.Kind).Width));
        for (int x = bow.Left; x < bow.Left + bow.Width; x++)
        {
            int j = x - bow.Left, steps = j < v.Class.BowFlat ? 0 : (j - v.Class.BowFlat) / bow.Run + 1;
            var (top, bottom) = bow.RowsAt(x);
            Assert.Equal((bow.Top + Math.Min(steps, bow.Fall), bow.Bottom - Math.Min(steps, bow.Rise)), (top, bottom));
            // The hull's cells in the column, glass too, are just those rows, but for a raised bridge's armour over its first columns.
            var bridge = vessel.Sections.SingleOrDefault(s => s.Kind == Vessel.SectionKind.Bridge);
            bool Bridge((int X, int Y) c) => bridge != null && c.X < bridge.Left + bridge.Width && c.Y <= bridge.Bottom;
            Assert.Equal(Enumerable.Range(top, bottom - top + 1), backs.Where(c => c.X == x && !Bridge(c)).Select(c => c.Y).Order());
        }
        Assert.Equal(v.Class.BowTip, bow.RowsAt(tip - 1).Bottom - bow.RowsAt(tip - 1).Top + 1);
        // The last step is a whole run: the tip's rows reached only in the last Run columns.
        Assert.NotEqual(v.Class.BowTip, bow.RowsAt(tip - 1 - bow.Run).Bottom - bow.RowsAt(tip - 1 - bow.Run).Top + 1);
    });

    [Fact]
    public void TheWayInIsAlwaysShutAndAnInnerDoorStandsOpenNowAndThenItsPiecesLaidOpen()
    {
        int open = 0, inner = 0;
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                var parts = Shipyard.Stamp(vessel);
                Assert.False(vessel.Doors[0].Open);
                foreach (var door in vessel.Doors)
                {
                    var pieces = parts.Where(p => p.Kind == "door" && p.X == door.X && p.Y >= door.Y && p.Y < door.Y + Content.Vessels.Open).ToList();
                    Assert.Equal(Content.Vessels.Open / 2, pieces.Count);
                    Assert.All(pieces, p => Assert.Equal(door.Open, p.Open));
                }
                inner += vessel.Doors.Count - 1;
                open += vessel.Doors.Count(d => d.Open);
            }
        // One in five, roughly.
        Assert.InRange(open * Content.Vessels.DoorOpenOneIn, inner / 2, inner * 2);
    }

    [Fact]
    public void WithTheLargestSternBulgeTheSpawnIsStillJustInsideTheFirstDoor()
    {
        foreach (var cls in Content.Vessels.Classes)
        {
            var built = Enumerable.Range(1, 400).Select(s => Shipyard.Build(cls, (uint)s)).First(b =>
            {
                var stern = All(b.Vessel, Vessel.SectionKind.Module)[0];
                var body = Section(b.Vessel, Vessel.SectionKind.Body);
                int down = cls.Layout == VesselLayout.Liner ? Content.Vessels.Liner.BulgeDown : Content.Vessels.SternBulge.Max;
                // A tanker's bridge stands over its stern module, which bulges up under nothing.
                int up = cls.Layout == VesselLayout.Tanker ? 0 : Content.Vessels.SternBulge.Max;
                return body.Top - stern.Top == up && stern.Bottom - body.Bottom == down;
            });
            var (vessel, parts) = (built.Vessel, built.Parts);
            var blocks = CellsOf(parts);
            var backs = CellsOf(parts, "back");
            var (x, y) = Shipyard.Door(built);
            Assert.Equal((vessel.Doors[0].X + 2, vessel.Doors[0].Y + Content.Vessels.Open - 1), (x, y));
            for (int up = 0; up < Content.Vessels.Open; up++)
                for (int across = 0; across < 2; across++)
                {
                    Assert.DoesNotContain((x + across, y - up), blocks);
                    Assert.Contains((x + across, y - up), backs);
                }
            Assert.Contains(parts, p => p.Kind == "wall" && p.X == x && p.Y == y + 1);
            Assert.Contains(parts, p => p.Kind == "door" && p.X == x - 2 && p.Y == y - 1);
        }
    }

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
    public void EveryClassFurnishesEveryCompartment()
    {
        foreach (var cls in Content.Vessels.Classes)
        {
            Assert.True(cls.ArmourDepth >= 2, $"{cls.Id}: a door is two cells wide");
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                Assert.Single(vessel.Compartments, c => c.Role == VesselRole.Bridge);
                Assert.Equal(cls.BridgeKind == VesselBridge.Raised, vessel.Compartments.Any(c => c.Deck < 0));
            }
        }
    }

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
        foreach (var role in Content.Vessels.Interiors.Select(i => i.Role).Distinct())
            Assert.Contains(drawn, d => d.Role == role && Content.Vessels.SalvageOf(d.Kind) != null);
    }

    [Fact]
    public void EachClassGivesWhatItsBandSaysTakenApartWhole()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
                Assert.InRange(Yield(Shipyard.Build(cls, seed).Parts), cls.Yield.Min, cls.Yield.Max);
    }

    /// <summary>Whole items <paramref name="parts"/> give taken apart, every kind counted alike: walls, back walls, pieces, machines, what crates hold.</summary>
    private static int Yield(IReadOnlyList<VesselPart> parts) => parts.Sum(p =>
        p.Layer == "machine" ? Machines.Get(p.Kind).Cost.Sum(c => c.Count) + (p.Holds?.Sum(h => h.Count) ?? 0)
        : p.Kind is "wall" or "backWall" ? Materials.Get(p.Material!).Cost.Sum(c => c.Count)
        : Builds.Get(p.Kind).Cost.Sum(c => c.Count));
}
