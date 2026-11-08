using System.Text.Json.Serialization;
using Stillwake.Sim.Crew;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// An area a creature laid that lasts (lurker-attacks): the capback's gas round it, its slime's puddle on the floor. Of a kind
/// (<see cref="Content.Creatures.PatchKind"/>), a box of cells from its top left (<see cref="Left"/>, <see cref="Top"/>) on the grid,
/// <see cref="Width"/> by <see cref="Height"/>, and the ticks it has left.
/// </summary>
public sealed class Patch
{
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>Its kind, by its id in <c>Creatures.Patches</c>.</summary>
    [JsonInclude] public string Kind { get; internal set; } = "";
    [JsonInclude] public int Left { get; internal set; }
    [JsonInclude] public int Top { get; internal set; }
    [JsonInclude] public int Width { get; internal set; }
    [JsonInclude] public int Height { get; internal set; }
    [JsonInclude] public int Ticks { get; internal set; }

    /// <summary>Whether cell (<paramref name="x"/>, <paramref name="y"/>) is in it.</summary>
    public bool Holds(int x, int y) => x >= Left && x < Left + Width && y >= Top && y < Top + Height;
}

/// <summary>Patches (lurker-attacks): laid by creatures, aging, and every breath paid for by the awake heroes in them.</summary>
public sealed partial class CreaturesModule
{
    /// <summary>The patches lying now, in id order; live and read-only.</summary>
    public IReadOnlyList<Patch> Patches => State.Patches;

    /// <summary>
    /// A patch of <paramref name="kind"/> laid with its middle on cell (<paramref name="x"/>, <paramref name="y"/>), or, <paramref name="floor"/>,
    /// with its bottom row there; cut to the grid.
    /// </summary>
    internal void LayPatch(string kind, int x, int y, bool floor)
    {
        var k = Spec.PatchOf(kind);
        int left = x - (k.Width - 1) / 2, top = floor ? y - k.Height + 1 : y - (k.Height - 1) / 2;
        int right = Math.Min(_grid.Width, left + k.Width), bottom = Math.Min(_grid.Height, top + k.Height);
        (left, top) = (Math.Max(0, left), Math.Max(0, top));
        State.Fired += 1;
        State.Patches.Add(new Patch { Id = State.Fired, Kind = kind, Left = left, Top = top, Width = right - left, Height = bottom - top, Ticks = k.Lasts });
    }

    /// <summary>Whether a hero in cell (<paramref name="x"/>, <paramref name="y"/>) pays a patch each breath: one holds the cell and sees it. What the client shows as stinging.</summary>
    public bool Stings(int x, int y)
    {
        foreach (var p in State.Patches)
            if (p.Holds(x, y) && Sees(p, x, y)) return true;
        return false;
    }

    /// <summary>The open row on the floor under cell (<paramref name="x"/>, <paramref name="y"/>): the last open one falling from it, at most a patch's fall.</summary>
    internal int FloorUnder(int x, int y)
    {
        for (int fall = 0; fall < Spec.PatchFall && !Solid(x, y + 1); fall++) y++;
        return y;
    }

    /// <summary>
    /// Each patch a tick older, those run out gone; then, every breath, every awake hero, in id order, pays each patch it stands in that
    /// sees it (nothing solid on the line from the patch's middle): poison is not spared by a cape, as lava is not.
    /// </summary>
    private void Fume()
    {
        var patches = State.Patches;
        if (patches.Count == 0) return;
        foreach (var p in patches) p.Ticks -= 1;
        patches.RemoveAll(p => p.Ticks <= 0);
        if (State.Clock % Content.Crew.BreathEvery != 0) return;
        var heroes = _crew.Heroes;
        int width = _grid.Width, cells = width * _grid.Height;
        for (int h = 0; h < heroes.Count; h++)
        {
            var hero = heroes[h];
            if (hero.Out != 0 || hero.Cell < 0 || hero.Cell >= cells) continue;
            int hx = hero.Cell % width, hy = hero.Cell / width;
            foreach (var p in patches)
                if (p.Holds(hx, hy) && Sees(p, hx, hy)) _crewOps.Shock(hero.Id, Spec.PatchOf(p.Kind).Damage, Harm.Poison);
        }
    }

    /// <summary>Whether nothing solid lies between <paramref name="p"/>'s middle and cell (<paramref name="x"/>, <paramref name="y"/>): gas does not pass a wall.</summary>
    private bool Sees(Patch p, int x, int y)
    {
        int mx = p.Left * Unit + p.Width * Unit / 2, my = p.Top * Unit + p.Height * Unit / 2;
        bool clear = true;
        Rays.Walk(_grid.Width, _grid.Height, mx, my, Middle(x) - mx, Middle(y) - my, cell =>
        {
            if (!Solid(cell)) return false;
            clear = false;
            return true;
        });
        return clear;
    }
}
