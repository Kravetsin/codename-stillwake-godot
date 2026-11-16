using Stillwake.Content;
using F = Stillwake.Content.Vessels.Fits;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// What <see cref="Outfit"/> lends a big class's parts: its stream, the sections, the parts so far, and its ways to place one: whether
    /// a part's cells are free, put it there, put it on top of or under the hull near a column; the hull's highest and lowest row over columns.
    /// </summary>
    private sealed record Outfitter(
        Rng Shape, IReadOnlyList<Vessel.Section> Sections, List<Vessel.Fitting> Outside,
        Func<string, int, int, bool> Free, Func<string, int, int, bool> Put,
        Func<string, int, int, bool> OnTop, Func<string, int, int, bool> Beneath,
        Func<int, int, int> Over, Func<int, int, int> Under);

    /// <summary>
    /// A big class's parts outside past its mast and dish (big-vessels, the mockup's), in this order. A liner: its fin on the service block
    /// (from <paramref name="aftEnd"/>'s gap), a radiator aft of it, then escape pods along the top past the fin, one every few columns where
    /// no raised room, housing or part is near, each drawn on stream 2 still in its cradle or gone, its cradle left; the bow's strobe is
    /// the client's. A tanker: a radiator past its bridge, then the manifold, the keel truss turned upward: segments in one line some rows
    /// over the hull's highest along it, a dome hatch at each tank bay's middle, a riser down from the manifold past the middle of each tank
    /// bay and the fuel bay under a valve's wheel over it, a strut of pylons down from each segment's posts where its column is clear; one
    /// draw for spheres under the keel at each tank bay's middle; the sensor boom at the bow's tip.
    /// </summary>
    private static void Big(Outfitter o, VesselLayout layout, IReadOnlyList<Vessel.Compartment> rooms, int aftEnd)
    {
        var body = o.Sections.First(s => s.Kind == Vessel.SectionKind.Body);
        var bowSection = o.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
        var bridge = o.Sections.First(s => s.Kind == Vessel.SectionKind.Bridge);
        int stern = body.Left, bow = bowSection.Left, gap = Vessels.PartGap;
        static (int W, int H) Size(string kind) => (Machines.Get(kind).Width, Machines.Get(kind).Height);
        Vessel.Fitting? Placed(string kind) => o.Outside.LastOrDefault(f => f.Kind == kind);

        if (layout == VesselLayout.Liner)
        {
            // The service block is the first past the aft one's gap.
            o.OnTop(Vessels.Fin, aftEnd + 2 + F.FinInto, 0);
            o.OnTop(Vessels.Radiator, stern + 1 + Size(Vessels.Rcs).W + gap, Vessels.PartSlide);
            var raised = o.Sections.Where(s => s.Kind is Vessel.SectionKind.Bridge or Vessel.SectionKind.Tier or Vessel.SectionKind.Housing).ToList();
            bool Near(int x, int w) => raised.Exists(s => x < s.Left + s.Width + gap && x + w + gap > s.Left);
            // A column of space round a pod clear of every part, so no two touch.
            bool Clear(string kind, int x, int y)
            {
                var (w, h) = Size(kind);
                for (int dy = 0; dy < h; dy++)
                    for (int dx = -1; dx <= w; dx++)
                        if (o.Outside.Exists(f => f.X <= x + dx && x + dx < f.X + Size(f.Kind).W && f.Y <= y + dy && y + dy < f.Y + Size(f.Kind).H)) return false;
                return true;
            }
            var fin = Placed(Vessels.Fin);
            int pod = Size(Vessels.Pod).W;
            for (int x = (fin == null ? aftEnd : fin.X + Size(Vessels.Fin).W) + F.PodPast; x + pod <= bow - F.PodShort; x += F.PodEvery)
            {
                if (Near(x, pod)) continue;
                string kind = o.Shape.Below(100) < F.PodPercent ? Vessels.Pod : Vessels.Cradle;
                int y = o.Over(x, pod) - Size(kind).H;
                if (Clear(kind, x, y)) o.Put(kind, x, y);
            }
            return;
        }

        o.OnTop(Vessels.Radiator, bridge.Left + bridge.Width + gap, Vessels.PartSlide);
        var radiator = Placed(Vessels.Radiator);
        int mw = Size(Vessels.Manifold).W;
        var segments = new List<int>();
        for (int t = (radiator == null ? bridge.Left + bridge.Width : radiator.X + Size(Vessels.Radiator).W) + gap; t + mw <= bow - F.ManifoldShort; t += mw) segments.Add(t);
        int level = segments.Count == 0 ? 0 : segments.Min(t => o.Over(t, mw)) - F.ManifoldRise;
        segments = segments.Where(t => o.Put(Vessels.Manifold, t, level)).ToList();
        // Each bay once, by its columns: a tank bay's two rooms stand over one another.
        var bays = rooms.Where(c => c.Role is VesselRole.TankBay or VesselRole.FuelBay).DistinctBy(c => c.Left).ToList();
        foreach (var b in bays.Where(b => b.Role == VesselRole.TankBay))
        {
            int x = b.Left + b.Width / 2 - Size(Vessels.Dome).W;
            o.Put(Vessels.Dome, x, o.Over(x, Size(Vessels.Dome).W) - Size(Vessels.Dome).H);
        }
        // A column of pylons, or of risers, from under the manifold down to the hull, only where every row of it is free.
        bool Down(string kind, int x)
        {
            int bottom = o.Over(x, 1);
            for (int y = level + Size(Vessels.Manifold).H; y < bottom; y++)
                if (!o.Free(kind, x, y)) return false;
            for (int y = level + Size(Vessels.Manifold).H; y < bottom; y++) o.Put(kind, x, y);
            return true;
        }
        foreach (var b in bays)
        {
            int x = b.Left + b.Width / 2 + F.RiserPast;
            if (!segments.Exists(t => x >= t && x < t + mw) || !o.Free(Vessels.Riser, x, level - 1)) continue;
            if (Down(Vessels.Riser, x)) o.Put(Vessels.LookKind(Vessels.Riser, 1), x, level - 1);
        }
        foreach (int t in segments)
            foreach (int post in F.ManifoldPosts) Down(Vessels.Pylon, t + post);

        if (o.Shape.Below(100) < F.SpherePercent)
            foreach (var b in bays.Where(b => b.Role == VesselRole.TankBay))
            {
                int x = b.Left + (b.Width - Size(Vessels.Sphere).W) / 2;
                o.Put(Vessels.Sphere, x, o.Under(x, Size(Vessels.Sphere).W) + 1);
            }
        int tip = bow + bowSection.Width;
        var (tipTop, tipBottom) = bowSection.RowsAt(tip - 1);
        o.Put(Vessels.Boom, tip, tipTop + (tipBottom - tipTop + 1 - Size(Vessels.Boom).H) / 2);
    }
}
