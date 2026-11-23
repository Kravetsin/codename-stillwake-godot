using Stillwake.Content;
using F = Stillwake.Content.Vessels.Fits;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A room furnished by its role, by code (big-vessels, the mockup's furnish): its ceiling lamps (a restaurant's chandeliers), then what
    /// stands on its floor from its left, a column between each two, none in <paramref name="kept"/>, none past its right. A lounge's
    /// loungers and tables, a promenade's and a dome's with chairs; a galley's stove, counters and fridges; an infirmary's beds and
    /// cabinet; the pool's basin, diving board and a lounger; the restaurant's bar, tables between chairs and the piano by its right; a
    /// hold's stacks of containers and of crates; the fuel tank, a tanker's fuel column as tall as its bay; a tanker's bay's two tall tanks or
    /// capsule, its pump room's pump and valves, its pipeway's valves and rack; the hangar's shuttle by its right, a fuel cart and stacks by its left.
    /// </summary>
    private static void Fit(Rng rng, Vessel.Compartment c, HashSet<int> kept, List<Vessel.Fitting> into)
    {
        int right = c.Left + c.Width, cx = c.Left, mine = into.Count;
        bool Free(int x, int w)
        {
            for (int k = x; k < x + w; k++)
                if (kept.Contains(k)) return false;
            return true;
        }
        // Whether a thing h tall on the floor from column x would cross one this room already has: a panel floor to ceiling, a lamp.
        bool Crosses(int x, int w, int h)
        {
            for (int i = mine; i < into.Count; i++)
            {
                var f = into[i];
                var s = Machines.Get(f.Kind);
                if (x < f.X + s.Width && f.X < x + w && c.Floor - h < f.Y + s.Height && f.Y < c.Floor) return true;
            }
            return false;
        }
        void Add(string kind, int x, int y) => into.Add(new(kind, x, y, []));
        // A thing on the floor at the first free columns from the cursor, short of limit, crossing nothing; null where none is left.
        (int X, int Y)? Put(string kind, int? limit = null)
        {
            var spec = Machines.Get(kind);
            int end = limit ?? right;
            while (cx + spec.Width <= end && (!Free(cx, spec.Width) || Crosses(cx, spec.Width, spec.Height))) cx++;
            if (cx + spec.Width > end) return null;
            var at = (cx, c.Floor - spec.Height);
            Add(kind, at.Item1, at.Item2);
            cx += spec.Width + 1;
            return at;
        }
        string Look(string kind) => Vessels.LookKind(kind, (int)rng.Below((uint)Vessels.LooksOf(kind)));

        string lamp = c.Role == VesselRole.Restaurant ? Vessels.Chandelier : Vessels.Lamp;
        int lampWidth = Machines.Get(lamp).Width;
        var at = c.Decks > 1 || c.Width > F.TwoLampsPast ? (int[])[c.Width / 3 - 1, 2 * c.Width / 3 - 1] : [c.Width / 2 - 1];
        foreach (int from in at)
        {
            int x = c.Left + from;
            while (x + lampWidth < right && !Free(x, lampWidth)) x++;
            if (Free(x, lampWidth) && x + lampWidth <= right) Add(lamp, x, c.Top);
        }

        switch (c.Role)
        {
            case VesselRole.Lounge when c.Deck == -1:
                for (int g = 0; g < F.PromenadeGroups; g++)
                {
                    if (Put(Vessels.Lounger) == null || Put(Vessels.Chair) == null || Put(Vessels.RoundTable) == null) break;
                    if (Put(Vessels.LookKind(Vessels.Chair, 1)) == null) break;
                    cx++;
                }
                break;
            case VesselRole.Lounge when c.Deck == -2:
                _ = Put(Vessels.Chair) != null && Put(Vessels.RoundTable) != null && Put(Vessels.LookKind(Vessels.Chair, 1)) != null;
                cx++;
                _ = Put(Vessels.Lounger) != null && Put(Vessels.Lounger) != null;
                break;
            case VesselRole.Lounge:
                for (int g = 0; g < F.LoungeGroups; g++)
                    if (Put(Vessels.Lounger) == null || Put(Vessels.RoundTable) == null) break;
                break;
            case VesselRole.Galley:
                foreach (var kind in (string[])[Vessels.Stove, Vessels.Counter, F.Fridge, Vessels.Counter, F.Fridge])
                    if (Put(kind == F.Fridge ? Look(kind) : kind) == null) break;
                break;
            case VesselRole.Infirmary:
                _ = Put(Vessels.Bed) != null && Put(Vessels.Bed) != null && Put(Vessels.MedCab) != null;
                break;
            case VesselRole.Pool:
                // The diving board over the basin's far end, a lounger past it.
                if (Put(Vessels.Pool) is { } basin)
                {
                    cx = basin.X + Machines.Get(Vessels.Pool).Width;
                    if (Put(Vessels.Diving) != null) Put(Vessels.Lounger);
                }
                break;
            case VesselRole.Restaurant:
                {
                    if (Put(Vessels.Bar) == null) break;
                    // Each table between two chairs facing it, the three set whole.
                    int piano = right - F.PianoFromRight, set = 2 * (1 + 1) + Machines.Get(Vessels.RoundTable).Width;
                    for (int g = 0; g < F.RestaurantTables && cx + set <= piano;)
                    {
                        if (!Free(cx, set))
                        {
                            cx++;
                            continue;
                        }
                        Put(Vessels.Chair);
                        Put(Vessels.RoundTable);
                        Put(Vessels.LookKind(Vessels.Chair, 1));
                        g++;
                    }
                    cx = Math.Max(cx, piano);
                    Put(Vessels.Piano);
                    break;
                }
            case VesselRole.Hold:
                for (int g = 0; g < F.HoldStacks; g++)
                {
                    if (rng.Below(100) < F.ContainerPercent)
                    {
                        if (Put(Look(Vessels.Cargo)) is not { } box) break;
                        int h = Machines.Get(Vessels.Cargo).Height;
                        for (int k = 1, n = Between(rng, F.ContainersOver); k <= n; k++) Add(Look(Vessels.Cargo), box.X, box.Y - h * k);
                    }
                    else
                    {
                        if (Put(F.Crate) is not { } crate) break;
                        int h = Machines.Get(F.Crate).Height;
                        for (int k = 1, n = Between(rng, F.CratesOver); k <= n; k++) Add(F.Crate, crate.X, crate.Y - h * k);
                    }
                }
                break;
            case VesselRole.FuelBay:
                Put(c.Decks > 2 ? Vessels.FuelColumn(c.Decks) : Vessels.Capsule);
                break;
            case VesselRole.TankBay:
                // Two tall tanks alike, or one capsule: fuel FuelCapsulePercent in a hundred, else water or oxygen alike.
                if (rng.Below(100) < F.TallTanksPercent)
                {
                    string tall = Look(Vessels.TankTall);
                    if (Put(tall) != null) Put(tall);
                }
                else
                {
                    int roll = (int)rng.Below(100);
                    Put(Vessels.LookKind(Vessels.Capsule, roll < F.FuelCapsulePercent ? 0 : 1 + roll % (Vessels.CapsuleContents - 1)));
                }
                break;
            case VesselRole.PumpRoom:
                _ = Put(Vessels.Pump) != null && Put(Vessels.Valves) != null;
                break;
            case VesselRole.Pipeway:
                _ = Put(Vessels.Valves) != null && Put(Vessels.Rack) != null;
                break;
            case VesselRole.Hangar:
                {
                    var model = Machines.Get(Vessels.Shuttles[(int)rng.Below((uint)Vessels.Shuttles.Count)]);
                    int sx = right - model.Width - F.ShuttleFromRight;
                    while (sx > c.Left && !Free(sx, model.Width)) sx--;
                    Add(model.Kind, sx, c.Floor - model.Height);
                    cx = c.Left + F.HangarFrom;
                    int limit = sx - F.HangarFrom;
                    Put(Vessels.FuelCart, limit);
                    if (Put(F.Crate, limit) is { } crate) Add(F.Crate, crate.X, crate.Y - Machines.Get(F.Crate).Height);
                    if (Put(Look(Vessels.Cargo), limit) is { } box) Add(Look(Vessels.Cargo), box.X, box.Y - Machines.Get(Vessels.Cargo).Height);
                    break;
                }
        }
    }

    /// <summary>
    /// Each tank's liquid (big-vessels), drawn after everything else on <paramref name="rng"/> so every draw before stays: inside in their
    /// order, then outside; the item its label says (<see cref="Vessels.ContentsOf"/>), as much as its size: a tall tank or a sphere small, a
    /// capsule medium, a fuel bay's tank large. A sphere takes the contents of the tank in the lowest bay over it.
    /// </summary>
    private static void Fill(Rng rng, IReadOnlyList<Vessel.Compartment> rooms, List<Vessel.Fitting> inside, List<Vessel.Fitting> outside)
    {
        for (int i = 0; i < inside.Count; i++)
        {
            var f = inside[i];
            if (Vessels.ContentsOf(f.Kind) is not int contents) continue;
            var room = rooms.First(c => c.Holds(f.X, f.Y));
            var size = Vessels.FirstLook(f.Kind) == Vessels.TankTall ? F.SmallTank
                : f.Kind is Vessels.FuelColumn3 or Vessels.FuelColumn4 ? F.ColumnTank : room.Role == VesselRole.FuelBay ? F.LargeTank : F.MediumTank;
            inside[i] = f with { Loot = [(Vessels.Liquids[contents], Between(rng, size))] };
        }
        int w = Machines.Get(Vessels.Sphere).Width;
        for (int i = 0; i < outside.Count; i++)
        {
            var f = outside[i];
            if (f.Kind != Vessels.Sphere) continue;
            var bay = rooms.Where(c => c.Role == VesselRole.TankBay && c.Left <= f.X && f.X + w <= c.Left + c.Width).MaxBy(c => c.Top)!;
            // A sphere under a fuel bay holds water: no sphere is drawn holding fuel.
            int contents = Math.Max(1, inside.Select(t => bay.Holds(t.X, t.Y) ? Vessels.ContentsOf(t.Kind) : null).First(k => k != null)!.Value);
            outside[i] = f with { Kind = Vessels.LookKind(Vessels.Sphere, contents - 1), Loot = [(Vessels.Liquids[contents], Between(rng, F.SmallTank))] };
        }
    }
}
