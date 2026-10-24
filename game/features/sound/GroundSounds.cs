using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Grid;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Footing = Stillwake.Game.Sound.Footing;
using Material = Stillwake.Content.Material;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// What a planet's ground sounds like (0093), chosen from what the sim says is there: the boots' step, push-off and landing by
/// what they stand on, an axe's hit by the wall or growth it lands on, a wall's break by its material, a growth's fall by its
/// kind, the drill's strikes by the wall at its bit. The tables are <see cref="S"/>'s; this picks from them.
/// </summary>
public static class GroundSounds
{
    private const int Unit = 1000, Half = Spec.Hero.Width / 2;
    private static readonly int SoilCode = Spec.Materials.Get("soil").Code;

    /// <summary>
    /// What the boots of a hero with its feet at (<paramref name="x"/>, <paramref name="y"/>), thousandths of a cell, stand on: the
    /// cell under the middle of its feet, else under either edge, in the row its feet are in or the two under it (a jump pushing off
    /// is a little way up already); none where nothing holds it up there, in the air.
    /// </summary>
    public static Footing? FootingOf(IGridQueries grid, IReadOnlyList<Plant> plants, int x, int y)
    {
        if (x < 0 || y < 0) return null;
        for (int row = y / Unit; row <= y / Unit + 2 && row < grid.Height; row++)
            foreach (int foot in (ReadOnlySpan<int>)[x, x - Half, x + Half - 1])
            {
                int column = foot / Unit;
                if (foot < 0 || column >= grid.Width) continue;
                if (Under(grid, plants, row * grid.Width + column) is Footing footing) return footing;
            }
        return null;
    }

    private static Footing? Under(IGridQueries grid, IReadOnlyList<Plant> plants, int cell)
    {
        int structure = grid.StructureAt(cell);
        if (structure == Structure.Empty) return null;
        if (structure != Structure.Wall || grid.WallAt(cell) is not { Natural: true } material) return Footing.Deck;
        if (material.Id == "alloy") return Footing.Alloy;
        if (Holds(plants, cell, "moss")) return Footing.Moss;
        if (material.Code != SoilCode) return Footing.Stone;
        int above = cell - grid.Width;
        return above >= 0 && Grass.Grows(structure, material.Code, grid.StructureAt(above), grid.BackLayer[above], SoilCode)
            ? Footing.Grass
            : Footing.Soil;
    }

    /// <summary>Whether a growth of <paramref name="kind"/> stands at <paramref name="cell"/>, the plants ascending by cell as the sim keeps them.</summary>
    public static bool Holds(IReadOnlyList<Plant> plants, int cell, string kind)
    {
        int lo = 0, hi = plants.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (plants[mid].Cell < cell) lo = mid + 1;
            else hi = mid;
        }
        for (int i = lo; i < plants.Count && plants[i].Cell == cell; i++)
            if (plants[i].Kind == kind) return true;
        return false;
    }

    /// <summary>A step on <paramref name="footing"/>.</summary>
    public static S.Cue StepOf(Footing footing) => S.Steps[footing];

    /// <summary>The push-off of a jump from <paramref name="footing"/>: the deck's boots on metal, else the ground's own step.</summary>
    public static S.Cue JumpOf(Footing footing) => footing == Footing.Deck ? S.Jump : StepOf(footing);

    /// <summary>
    /// A landing on <paramref name="footing"/> from a fall at <paramref name="fall"/> thousandths of a cell a step: its cue, and the
    /// decibels and the pitch scale on top of the cue's. On the deck and on alloy their step, louder and lower; on the ground soft
    /// or heavy as the landing pose is, quieter and higher the lighter the fall.
    /// </summary>
    public static (S.Cue Cue, float OffsetDb, float Tune) LandOf(Footing footing, int fall)
    {
        if (footing == Footing.Deck) return (S.Land, 0, 1);
        if (footing == Footing.Alloy) return (S.LandAlloy, 0, 1);
        var cue = fall >= HeroMotion.Landing.Heavy ? S.LandHard : S.LandSoft;
        float share = Math.Clamp((float)(fall - S.LandSoftestFall) / (S.LandHardestFall - S.LandSoftestFall), 0, 1);
        return (cue, S.LandSoftestDb * (1 - share), 1 - (1 - S.LandHardestTune) * share);
    }

    /// <summary>The axe's hit on a natural wall of <paramref name="material"/>; none for one that never breaks.</summary>
    public static S.Cue? HitOf(Material material) => S.Ground.TryGetValue(material.Id, out var sounds) ? sounds.Hit : null;

    /// <summary>A natural wall of <paramref name="material"/> broken out; none for one that never breaks.</summary>
    public static S.Cue? BreakOf(Material material) => S.Ground.TryGetValue(material.Id, out var sounds) ? sounds.Break : null;

    /// <summary>A growth of <paramref name="kind"/> felled; none for one with no sound of its own (<see cref="S.Unheard"/>).</summary>
    public static S.Cue? FallOf(string kind) => S.Growths.TryGetValue(kind, out var sounds) ? sounds.Fall : null;

    /// <summary>
    /// A hit of any tool at <paramref name="cell"/>, taking <paramref name="damage"/> (the sim's for its sender, a module's boost in
    /// it): what is hit decides, the natural wall's there, else the growth's the hit takes. None for a growth any hit fells alone, its
    /// fall saying it; nor for the hit that fells one whose fall sounds as its hits do, a bush's last, which would play it twice.
    /// </summary>
    public static S.Cue? HitAt(IGridQueries grid, BuildingModule building, int cell, int damage)
    {
        if (grid.WallAt(cell) is { Natural: true } wall) return HitOf(wall);
        if (building.PlantAt(cell) is not { } plant || !S.Growths.TryGetValue(plant.Kind, out var sounds) || sounds.Hit is not { } hit)
            return null;
        if (hit != sounds.Fall) return hit;
        int crack = 0;
        foreach (var c in building.Cracks)
            if (c.Cell == plant.Cell) crack = c.Damage;
        return crack + damage >= Spec.Flora.Get(plant.Kind).Strength ? null : hit;
    }

    /// <summary>The decibels a hit of <paramref name="tool"/> plays under the object's hit: a drill's, run together, quieter.</summary>
    public static float HitDbOf(string tool) => HeldLook.IsDrill(tool) ? S.DrillHitDb : 0;
}
