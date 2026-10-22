namespace Stillwake.Sim.Fauna;

public sealed partial class FaunaModule
{
    /// <summary>
    /// A strike with any weapon at a creature that is out most of the time, else one gone or never spawned, three times in four;
    /// else a release of a species at any cell, the grid's edge and past it too, so every refusal comes: aboard, full, blocked.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var all = State.Creatures;
        if (rng.Below(4) != 0)
        {
            int id = all.Count > 0 && rng.Below(4) != 0 ? all[(int)rng.Below((uint)all.Count)].Id : (int)rng.Below((uint)State.NextId + 2);
            var weapons = Content.Weapons.All;
            return new Strike(id, weapons[(int)rng.Below((uint)weapons.Count)].Item) { From = from };
        }
        var species = Content.Fauna.All[(int)rng.Below((uint)Content.Fauna.All.Count)];
        int cells = _grid.Width * _grid.Height;
        return new Release(species.Id, (int)rng.Below((uint)cells + 2) - 1) { From = from };
    }
}
