namespace Stillwake.Sim.Fauna;

public sealed partial class FaunaModule
{
    /// <summary>
    /// A strike with any weapon at a creature that is out most of the time, else one gone or never spawned, five times in eight; a
    /// hit by a charge, of a charging creature half the time, else of any or one gone, so it is refused not charging and gone too;
    /// else a release of a species at any cell, the grid's edge and past it too, so every refusal comes: aboard, full, blocked.
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
            var charging = all.Where(c => c.Mode == Mode.Charge).ToList();
            int id = charging.Count > 0 && rng.Below(2) == 0 ? charging[(int)rng.Below((uint)charging.Count)].Id : Any();
            return new HitBy(id) { From = from };
        }
        var species = Content.Fauna.All[(int)rng.Below((uint)Content.Fauna.All.Count)];
        int cells = _grid.Width * _grid.Height;
        return new Release(species.Id, (int)rng.Below((uint)cells + 2) - 1) { From = from };
    }
}
