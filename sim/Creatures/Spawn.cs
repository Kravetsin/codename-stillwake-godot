using Stillwake.Content;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Spawning (0102): round each hero, on the beat, out of every hero's view, where a habitat of the surface's type is. Its own ring,
/// drawn as the far field's is (0052), not shared with it: one caller each so far.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>
    /// Round each hero in id order with fewer than <c>Creatures.PerKeep</c> in its keep box, while the surface is under its cap: three
    /// draws, always taken — the side, the columns out, the habitat by weight — then the first place down that column, within the
    /// hero's keep box, where the habitat's species stands. None off the grid, without such a place, or in any hero's view.
    /// </summary>
    private void Spawn()
    {
        var all = State.Creatures;
        var habitats = Habitats;
        foreach (var at in _heroes)
        {
            if (all.Count >= Spec.Max) return;
            int near = 0;
            foreach (var c in all)
                if (Within(Down(c.X, Unit), Down(c.Y - 1, Unit), at, Spec.Keep)) near++;
            if (near >= Spec.PerKeep) continue;
            bool right = Random.Below(2) == 0;
            int out_ = Spec.RingFrom + (int)Random.Below(Spec.RingTo - Spec.RingFrom + 1);
            var habitat = Drawn(habitats);
            var spec = Spec.Get(habitat.Species);
            // The body's left column: on the right its left edge is the ring's, on the left its right edge.
            int left = right ? at.X + out_ : at.X - out_ - spec.Body.Width + 1;
            if (left < 0 || left + spec.Body.Width > _grid.Width) continue;
            if (PlaceIn(habitat, spec, left, at.Y) is not int row || Seen(left, row - spec.Body.Height + 1, spec)) continue;
            var (x, y) = Standing(spec, left, row);
            Add(spec, x, y);
        }
    }

    /// <summary>A habitat drawn by weight, one draw, walking them in order.</summary>
    private Spec.Habitat Drawn(IReadOnlyList<Spec.Habitat> habitats)
    {
        int total = 0;
        foreach (var h in habitats) total += h.Weight;
        int roll = (int)Random.Below((uint)total);
        foreach (var h in habitats)
        {
            if (roll < h.Weight) return h;
            roll -= h.Weight;
        }
        throw new InvalidOperationException("creatures: a roll past the total weight");
    }

    /// <summary>
    /// The bottom row of the first place, from the top of a hero at row <paramref name="heroRow"/>'s keep box down, where a body of
    /// <paramref name="spec"/> with its left column at <paramref name="left"/> stands in <paramref name="habitat"/>: every cell of it
    /// open, solid under every one, and each of the habitat's place — no back wall under the open sky, a back wall in its depth band
    /// in a cave. Null for none.
    /// </summary>
    private int? PlaceIn(Spec.Habitat habitat, Spec.Species spec, int left, int heroRow)
    {
        int from = Math.Max(spec.Body.Height - 1, heroRow - Spec.Keep.Y), to = Math.Min(_grid.Height - 2, heroRow + Spec.Keep.Y);
        for (int row = from; row <= to; row++)
            if (Stands(habitat, spec, left, row)) return row;
        return null;
    }

    private bool Stands(Spec.Habitat habitat, Spec.Species spec, int left, int row)
    {
        int width = _grid.Width;
        for (int x = left; x < left + spec.Body.Width; x++)
        {
            if (!Solid(x, row + 1)) return false;
            for (int y = row - spec.Body.Height + 1; y <= row; y++)
            {
                if (Solid(x, y)) return false;
                bool backed = _grid.BackAt(y * width + x) != null;
                if (habitat.Place == Spec.Place.Surface ? backed : !backed || Terrain.BandOf((PlanetType)Rules, y) != habitat.Band) return false;
            }
        }
        return true;
    }

    /// <summary>Whether any hero sees any cell of a body of <paramref name="spec"/> with its top left cell at (<paramref name="left"/>, <paramref name="top"/>).</summary>
    private bool Seen(int left, int top, Spec.Species spec)
    {
        var view = Spec.View;
        foreach (var at in _heroes)
            if (left + spec.Body.Width - 1 >= at.X - view.X && left <= at.X + view.X && top + spec.Body.Height - 1 >= at.Y - view.Y && top <= at.Y + view.Y)
                return true;
        return false;
    }
}
