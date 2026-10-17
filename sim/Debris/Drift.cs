using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Debris;

/// <summary>
/// The debris module's system: pieces drift and are gone, and each zone of <c>Debris.Zones</c> spawns on
/// its beat, the stream by the hull and the far field round every hero (0052).
/// </summary>
public sealed partial class DebrisModule
{
    /// <summary>The columns and rows the hull spans, every cell with a structure in it; null for none.</summary>
    private (int Left, int Right, int Top, int Bottom)? _hull;
    /// <summary>The grid's <c>Version</c> <see cref="_hull"/> was found at: found again only when it moves.</summary>
    private int _hullAt = int.MinValue;
    /// <summary>Where each hero on the grid is this tick, by its reported cell, in id order; refilled every tick.</summary>
    private readonly List<(int X, int Y)> _heroes = [];

    /// <summary>The hull is found again after a create and a load: the grid's version starts over then.</summary>
    public override void Rebuild() => _hullAt = int.MinValue;

    private (int Left, int Right, int Top, int Bottom)? Hull()
    {
        if (_grid.Version == _hullAt) return _hull;
        _hull = HullOf();
        _hullAt = _grid.Version;
        return _hull;
    }

    /// <summary>The binary search of <paramref name="id"/> among the pieces, in id order as they spawned; -1 for none.</summary>
    private int IndexOf(int id)
    {
        var pieces = State.Pieces;
        int lo = 0, hi = pieces.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2, at = pieces[mid].Id;
            if (at == id) return mid;
            if (at < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return -1;
    }

    /// <summary>The zone of that id in <c>Debris.Zones</c>; a piece of no zone there is our bug.</summary>
    private static DebrisZone ZoneOf(string id)
    {
        var zones = Content.Debris.Zones;
        for (int i = 0; i < zones.Count; i++)
            if (zones[i].Id == id) return zones[i];
        throw new InvalidOperationException($"debris: a piece of zone '{id}', which is not in Debris.Zones");
    }

    /// <summary>
    /// Moves a reeled piece its share of the way still to go, truncated toward zero, so the last tick
    /// lands it exactly; the line comes free on that tick.
    /// </summary>
    private static void Pull(Piece piece, Reeling reel)
    {
        piece.X += (reel.ToX - piece.X) / reel.Left;
        piece.Y += (reel.ToY - piece.Y) / reel.Left;
        reel.Left -= 1;
        if (reel.Left == 0) piece.Reel = null;
    }

    private void Drift()
    {
        var s = State;
        // A world begins with pieces already on their way, as if the ship had drifted a while.
        if (s.Clock == 0 && s.AtStart > 0 && Hull() is { } first)
        {
            var ship = Content.Debris.Ship;
            for (int i = 0; i < Math.Min(s.AtStart, ship.Max); i++) SpawnByHull(ship, (ByHull)ship.Anchor, first, along: true);
        }
        s.Clock += 1;
        FindHeroes();
        // Moved, then gone past its end or, round the heroes, left behind by them all; the rest kept in order.
        var pieces = s.Pieces;
        int kept = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece.Reel is { } reel) Pull(piece, reel);
            else piece.X -= piece.Speed;
            if (piece.X < piece.End) continue;
            if (ZoneOf(piece.Zone).Anchor is AroundHeroes around && !Kept(piece, around)) continue;
            pieces[kept++] = piece;
        }
        pieces.RemoveRange(kept, pieces.Count - kept);
        var zones = Content.Debris.Zones;
        for (int i = 0; i < zones.Count; i++) Beat(zones[i]);
    }

    /// <summary>Every hero's reported cell, in id order, as a column and row; one off the grid, which only the fuzz and an old save hold, is nowhere.</summary>
    private void FindHeroes()
    {
        _heroes.Clear();
        int width = _grid.Width, cells = width * _grid.Height;
        var heroes = _crew.Heroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            int cell = heroes[i].Cell;
            if (cell >= 0 && cell < cells) _heroes.Add((cell % width, cell / width));
        }
    }

    private static bool Within(int x, int y, (int X, int Y) at, HalfBox box) => Math.Abs(x - at.X) <= box.X && Math.Abs(y - at.Y) <= box.Y;

    /// <summary>
    /// Whether a piece round the heroes stays: in some hero's keep box, seen there this tick; else within
    /// some hero's hard box and seen less than its linger ago.
    /// </summary>
    private bool Kept(Piece piece, AroundHeroes around)
    {
        int x = piece.X / Unit, y = piece.Y / Unit;
        bool near = false;
        foreach (var at in _heroes)
        {
            if (Within(x, y, at, around.Keep))
            {
                piece.SeenAt = State.Clock;
                return true;
            }
            near |= Within(x, y, at, around.Hard);
        }
        return near && State.Clock - piece.SeenAt < around.Linger;
    }

    /// <summary>
    /// On the zone's beat, its next beat set, then a piece by the hull, or one round each hero that may
    /// have one; a zone with no beat yet, new since the world was saved, is given one first.
    /// </summary>
    private void Beat(DebrisZone zone)
    {
        var s = State;
        Beat? beat = null;
        foreach (var b in s.Beats)
            if (b.Zone == zone.Id) beat = b;
        if (beat == null) s.Beats.Add(beat = new Beat { Zone = zone.Id, Next = s.Clock + zone.Every });
        if (s.Clock < beat.Next) return;
        // No jitter, no draw: a beat that spawns nothing takes nothing from the world's Rng.
        beat.Next = s.Clock + zone.Every + (zone.Jitter > 0 ? (int)Random.Below((uint)zone.Jitter + 1) : 0);
        switch (zone.Anchor)
        {
            case ByHull byHull:
                // Found again on every beat, so a ship grown taller gets a taller stream.
                if (CountOf(zone) < zone.Max && Hull() is { } hull) SpawnByHull(zone, byHull, hull, along: false);
                break;
            case AroundHeroes around:
                if (Hull() is { } from) SpawnAround(zone, around, from);
                break;
        }
    }

    private int CountOf(DebrisZone zone)
    {
        int n = 0;
        foreach (var piece in State.Pieces)
            if (piece.Zone == zone.Id) n++;
        return n;
    }

    /// <summary>
    /// A piece by the hull, drawn by the world's Rng: its row, kind and speed, then, <paramref name="along"/>,
    /// how far along its way it already is; otherwise at its start.
    /// </summary>
    private void SpawnByHull(DebrisZone zone, ByHull byHull, (int Left, int Right, int Top, int Bottom) hull, bool along)
    {
        var s = State;
        var (left, right, top, bottom) = hull;
        int width = _grid.Width, height = _grid.Height;
        int from = Math.Max(0, top - byHull.Margin);
        int to = Math.Min(height - 1, bottom + byHull.Margin);
        int row = from + (int)Random.Below((uint)(to - from + 1));
        int column = Math.Min(width - 1, right + byHull.Lead);
        var item = KindOf(zone, column, row, hull);
        int speed = zone.DriftMin + (int)Random.Below((uint)(zone.DriftMax - zone.DriftMin + 1));
        int start = column * Unit + Unit / 2;
        int end = Math.Max(0, left - byHull.Lead) * Unit;
        s.Pieces.Add(new Piece
        {
            Id = s.NextId,
            Zone = zone.Id,
            Item = item,
            X = along ? end + (int)Random.Below((uint)(start - end + 1)) : start,
            Y = row * Unit + Unit / 2,
            Speed = speed,
            Start = start,
            End = end,
        });
        s.NextId += 1;
    }

    /// <summary>
    /// Round each hero in id order, out past every stream's box, that has fewer than its share in its keep
    /// box, while the zone is under its cap: a point on the ring round its reported cell, drawn as its
    /// side, the offset along it and the depth across, then a kind by the tier its distance from the hull
    /// picks and a speed. A point off the grid, in a stream's box or in any hero's view spawns nothing,
    /// its draws taken all the same. It drifts from where it spawns to the grid's left edge.
    /// </summary>
    private void SpawnAround(DebrisZone zone, AroundHeroes around, (int Left, int Right, int Top, int Bottom) hull)
    {
        var s = State;
        int width = _grid.Width, height = _grid.Height;
        int count = CountOf(zone);
        foreach (var at in _heroes)
        {
            if (count >= zone.Max) return;
            if (InStream(at.X, at.Y, hull)) continue;
            int near = 0;
            foreach (var piece in s.Pieces)
                if (piece.Zone == zone.Id && Within(piece.X / Unit, piece.Y / Unit, at, around.Keep)) near++;
            if (near >= around.PerHero) continue;
            var (from, to) = (around.RingFrom, around.RingTo);
            int side = (int)Random.Below(4);
            bool across = side < 2;
            int span = across ? to.Y : to.X;
            int offset = (int)Random.Below((uint)(2 * span + 1)) - span;
            int depth = across
                ? from.X + (int)Random.Below((uint)(to.X - from.X + 1))
                : from.Y + (int)Random.Below((uint)(to.Y - from.Y + 1));
            int x = at.X + (across ? (side == 0 ? depth : -depth) : offset);
            int y = at.Y + (across ? offset : (side == 2 ? -depth : depth));
            var item = KindOf(zone, x, y, hull);
            int speed = zone.DriftMin + (int)Random.Below((uint)(zone.DriftMax - zone.DriftMin + 1));
            if (x < 0 || x >= width || y < 0 || y >= height || InStream(x, y, hull) || InView(x, y, around)) continue;
            int start = x * Unit + Unit / 2;
            s.Pieces.Add(new Piece
            {
                Id = s.NextId,
                Zone = zone.Id,
                Item = item,
                X = start,
                Y = y * Unit + Unit / 2,
                Speed = speed,
                Start = start,
                End = 0,
                SeenAt = s.Clock,
            });
            s.NextId += 1;
            count += 1;
        }
    }

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is in the box of a zone by the hull: the hull grown by its lead across and its margin up and down.</summary>
    private static bool InStream(int x, int y, (int Left, int Right, int Top, int Bottom) hull)
    {
        foreach (var zone in Content.Debris.Zones)
            if (zone.Anchor is ByHull h && x >= hull.Left - h.Lead && x <= hull.Right + h.Lead && y >= hull.Top - h.Margin && y <= hull.Bottom + h.Margin)
                return true;
        return false;
    }

    private bool InView(int x, int y, AroundHeroes around)
    {
        foreach (var at in _heroes)
            if (Within(x, y, at, around.View)) return true;
        return false;
    }

    /// <summary>
    /// The zone's tier for a piece at (<paramref name="x"/>, <paramref name="y"/>): the last that starts at
    /// or within <c>max(dx, 2·dy)</c> cells of the hull's box, <c>dx</c> and <c>dy</c> the cells past it.
    /// </summary>
    internal static DebrisTier TierOf(DebrisZone zone, int x, int y, (int Left, int Right, int Top, int Bottom) hull)
    {
        int dx = Math.Max(0, Math.Max(hull.Left - x, x - hull.Right));
        int dy = Math.Max(0, Math.Max(hull.Top - y, y - hull.Bottom));
        int d = Math.Max(dx, 2 * dy);
        var tier = zone.Tiers[0];
        for (int i = 1; i < zone.Tiers.Count; i++)
            if (zone.Tiers[i].From <= d) tier = zone.Tiers[i];
        return tier;
    }

    /// <summary>A kind drawn by weight, one draw, from <see cref="TierOf"/>'s tier, walking its kinds in order.</summary>
    private string KindOf(DebrisZone zone, int x, int y, (int Left, int Right, int Top, int Bottom) hull)
    {
        var tier = TierOf(zone, x, y, hull);
        int total = 0;
        foreach (var (_, weight) in tier.Kinds) total += weight;
        int roll = (int)Random.Below((uint)total);
        foreach (var (item, weight) in tier.Kinds)
        {
            if (roll < weight) return item;
            roll -= weight;
        }
        throw new InvalidOperationException("debris: a roll past the total weight");
    }

    /// <summary>
    /// The columns and rows the hull spans: every cell with a structure in it. None for no hull. Walked
    /// within the grid's <c>Built</c>, past which nothing stands, so it costs the ship and not the world,
    /// and only when the grid has changed since.
    /// </summary>
    private (int Left, int Right, int Top, int Bottom)? HullOf()
    {
        var structure = _grid.StructureLayer;
        int width = _grid.Width;
        var built = _grid.Built;
        (int Left, int Right, int Top, int Bottom)? hull = null;
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int x = built.Left; x <= built.Right; x++)
            {
                if (structure[y * width + x] == Structure.Empty) continue;
                hull = hull is { } h ? (Math.Min(h.Left, x), Math.Max(h.Right, x), h.Top, y) : (x, x, y, y);
            }
        return hull;
    }
}
