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
    /// hold's stacks of containers and of crates; the fuel tank; the hangar's shuttle by its right, a fuel cart and stacks by its left.
    /// </summary>
    private static void Fit(Rng rng, Vessel.Compartment c, HashSet<int> kept, List<Vessel.Fitting> into)
    {
        int right = c.Left + c.Width, cx = c.Left;
        bool Free(int x, int w)
        {
            for (int k = x; k < x + w; k++)
                if (kept.Contains(k)) return false;
            return true;
        }
        void Add(string kind, int x, int y) => into.Add(new(kind, x, y, []));
        // A thing on the floor at the first free columns from the cursor, short of limit; null where none is left.
        (int X, int Y)? Put(string kind, int? limit = null)
        {
            var spec = Machines.Get(kind);
            int end = limit ?? right;
            while (cx + spec.Width <= end && !Free(cx, spec.Width)) cx++;
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
                Put(Vessels.Capsule);
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
}
