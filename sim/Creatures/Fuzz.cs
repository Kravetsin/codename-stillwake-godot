namespace Stillwake.Sim.Creatures;

public sealed partial class CreaturesModule
{
    /// <summary>
    /// A strike with any weapon at a creature that is out most of the time, else one gone or never spawned, five times in eight; a
    /// hit by a charge, of a charging creature half the time, else of any or one gone, so it is refused not charging and gone too;
    /// else a release of a species at any cell, the grid's edge and past it too, so every refusal comes: aboard, full, blocked, no
    /// piece; a clinger's half the time onto a wreck.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var all = State.Creatures;
        uint pick = rng.Below(8);
        int Any() => all.Count > 0 && rng.Below(4) != 0 ? all[(int)rng.Below((uint)all.Count)].Id : (int)rng.Below((uint)State.NextId + 2);
        if (pick < 5)
        {
            int id = Any();
            var weapons = Content.Weapons.All;
            return new Strike(id, weapons[(int)rng.Below((uint)weapons.Count)].Item) { From = from };
        }
        if (pick == 5)
        {
            var charging = all.Where(c => c.Posture == Posture.Charge).ToList();
            int id = charging.Count > 0 && rng.Below(2) == 0 ? charging[(int)rng.Below((uint)charging.Count)].Id : Any();
            return new HitBy(id) { From = from };
        }
        var species = Content.Creatures.All[(int)rng.Below((uint)Content.Creatures.All.Count)];
        // A clinger half the time onto a wreck out, so a live sentry plays in the suites; else anywhere, refused mostly.
        if (species.Body is Content.Creatures.Clinger && rng.Below(2) == 0 && WreckCell(rng) is int onto) return new Release(species.Id, onto) { From = from };
        int cells = _grid.Width * _grid.Height;
        return new Release(species.Id, (int)rng.Below((uint)cells + 2) - 1) { From = from };
    }

    /// <summary>A cell inside the box of a wreck out, drawn: the wreck, then the cell; null with none out.</summary>
    private int? WreckCell(Rng rng)
    {
        var wrecks = _debris.Pieces.Where(p => Content.Wreckage.SizeOf(p.Item) != null).ToList();
        if (wrecks.Count == 0) return null;
        var piece = wrecks[(int)rng.Below((uint)wrecks.Count)];
        var (w, h) = Content.Wreckage.SizeOf(piece.Item)!.Value;
        int x = piece.X / Unit - w / 2 + (int)rng.Below((uint)w), y = piece.Y / Unit - h / 2 + (int)rng.Below((uint)h);
        return x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height ? null : y * _grid.Width + x;
    }
}
