using Stillwake.Sim.Debris;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Nests (companion-fights): packs asleep on the far field's wrecks, no creatures till they wake. A nest is drawn with its piece,
/// wakes whole round it into open space, and goes with it, nothing given.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Where a pack wakes round its piece, in thousandths of the half box it stands off by: as many as the largest nest, no two the same.</summary>
    private static readonly (int X, int Y)[] HatchPlaces =
        [(1000, 0), (-1000, 0), (0, -1000), (0, 1000), (1000, -1000), (-1000, 1000), (-1000, -1000), (1000, 1000)];

    /// <summary>How far past its piece's box a pack wakes, in thousandths of a cell: a breaker's body and a cell clear of the picture.</summary>
    private const int HatchGap = 2 * Unit;

    private readonly List<(int X, int Y)> _hatch = [];

    /// <summary>The nests asleep on this surface, ascending by their piece's id; live and read-only.</summary>
    public IReadOnlyList<Nest> Nests => State.Nests;

    /// <summary>The species that sleeps on a piece such as <paramref name="piece"/>; null where none does.</summary>
    public string? SleeperOn(Piece piece) => NestKind(piece)?.Species;

    /// <summary>The nests' kind for <paramref name="piece"/>; null for a piece no pack sleeps on.</summary>
    private Spec.Nest? NestKind(Piece piece)
    {
        // Asked of every nest every tick: by index, for an enumerator is an allocation.
        var all = Rules.Nests;
        for (int i = 0; i < all.Count; i++)
            if (all[i].Zone == piece.Zone)
                for (int k = 0; k < all[i].Kinds.Count; k++)
                    if (string.Equals(all[i].Kinds[k], piece.Item, StringComparison.Ordinal)) return all[i];
        return null;
    }

    /// <summary>
    /// Whether a pack sleeps on <paramref name="piece"/>, just come, and how many: one draw for a piece of a nest's kind not yet
    /// struck, one more for the size of the nest it has. The surface's cap is asked as it wakes, never here.
    /// </summary>
    private void NestOn(Piece piece)
    {
        if (piece.Struck != null || NestKind(piece) is not { } kind || Random.Below(1000) >= kind.PerMille) return;
        State.Nests.Add(new Nest { Piece = piece.Id, Size = kind.Least + (int)Random.Below((uint)(kind.Most - kind.Least + 1)) });
    }

    /// <summary>Each nest in piece order: gone with its piece, spent once woken, else asleep or waiting for room.</summary>
    private void WakeNests()
    {
        var nests = State.Nests;
        int kept = 0;
        for (int i = 0; i < nests.Count; i++)
        {
            var nest = nests[i];
            if (_debris.PieceById(nest.Piece) is not { } piece || NestKind(piece) is not { } kind) continue;
            if (!Woke(nest, piece, kind)) nests[kept++] = nest;
        }
        nests.RemoveRange(kept, nests.Count - kept);
    }

    /// <summary>
    /// Whether <paramref name="nest"/> on <paramref name="piece"/> woke: called by a strike on the piece, whoever's, or by a hero out
    /// within its wake; while there is room for all of it under the surface's cap and round that hero, and any free place round it.
    /// </summary>
    private bool Woke(Nest nest, Piece piece, Spec.Nest kind)
    {
        int x = Down(piece.X, Unit), y = Down(piece.Y, Unit);
        (int X, int Y) hero;
        if (piece.Struck is { } blow && OutAt(blow.By) is { } striker) hero = striker;
        else if (NearestOut(x, y) is { } near && (piece.Struck != null || near.Distance <= kind.Wake)) hero = near.At;
        else return false;
        int alive = 0, round = 0;
        foreach (var c in State.Creatures)
        {
            if (c.Dead) continue;
            alive += 1;
            if (c.Species == kind.Species && Within(Down(c.X, Unit), Down(c.Y - 1, Unit), hero, Spec.Keep)) round += 1;
        }
        if (alive + nest.Size > Spec.Max || round + nest.Size > kind.PerKeep) return false;
        var spec = Spec.Get(kind.Species);
        FreePlaces(piece, spec, nest.Size);
        foreach (var (atX, atY) in _hatch) Add(spec, atX, atY);
        return _hatch.Count > 0;
    }

    /// <summary>The bottom middles round <paramref name="piece"/> where a body of <paramref name="spec"/> lies wholly in open space and in nothing solid, up to <paramref name="most"/>, in <see cref="_hatch"/>.</summary>
    private void FreePlaces(Piece piece, Spec.Species spec, int most)
    {
        _hatch.Clear();
        var (w, h) = Content.Wreckage.SizeOf(piece.Item) ?? (0, 0);
        int offX = w * Unit / 2 + HatchGap, offY = h * Unit / 2 + HatchGap;
        foreach (var place in HatchPlaces)
        {
            if (_hatch.Count == most) return;
            // Round the wreck as it is turned (debris-physics); the body itself hatches upright.
            var (dx, dy) = TurnedBox.Turn(piece.Angle, place.X * offX / Unit, place.Y * offY / Unit);
            int x = piece.X + dx, y = piece.Y + dy + spec.Body.Height * Unit / 2;
            // A platform out there is space to the rooms, and solid to a body (as a lair is asked).
            if (InSpace(spec.Body, x, y) && !Overlaps(spec.Body, x, y)) _hatch.Add((x, y));
        }
    }
}
