using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Material = Stillwake.Content.Material;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// Digging seen (0077): each cracked wall drawn with cracks in three stages as its crack grows toward its strength
/// (<see cref="BuildingModule.Cracks"/>); a hit that deepens one throws chips in its material's colour and a little dust,
/// a wall broken out bursts into dust and chips. Whoever dug: a friend's digs show as one's own. It reads the cracks each
/// frame, a short list, and redraws only when they change.
/// </summary>
public partial class DigView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    /// <summary>The cracks drawn, by cell: how deep, and what the wall was made of, for its dust once it breaks.</summary>
    private readonly Dictionary<int, (int Damage, Material Material)> _drawn = new();
    private readonly List<int> _gone = [];

    /// <summary>The stages of cracks, three, for each shape a wall is cut to: a slope's only over its solid half (0079).</summary>
    private const int Stages = 3;
    private static readonly Dictionary<(int Stage, int Shape), Texture2D> Pictures = new();
    private static readonly Dictionary<string, (Luminous Chip, Luminous Dust, Luminous Cloud)> Looks = new();

    /// <summary>A wall broken out heard where it stood, by its material, whoever broke it (0093).</summary>
    private readonly Voices _breaks = new(Game.Sound.World, voices: 3);

    public DigView(IGridQueries grid, BuildingModule building)
    {
        _grid = grid;
        _building = building;
        ZIndex = Layers.Dug;
        Lit.Set(this, Stillwake.Game.Light.WallIn);
        AddChild(_breaks);
    }

    private static Texture2D Texture(int stage, int shape) =>
        Pictures.TryGetValue((stage, shape), out var made) ? made : Pictures[(stage, shape)] = ImageTexture.CreateFromImage(GroundArt.Crack(stage, shape));

    public override void _Process(double delta)
    {
        bool changed = false;
        var cracks = _building.Cracks;
        foreach (var crack in cracks)
        {
            if (_drawn.TryGetValue(crack.Cell, out var was) && was.Damage == crack.Damage) continue;
            var material = _grid.WallAt(crack.Cell);
            if (material == null) continue;
            if (crack.Damage > was.Damage) Hit(crack.Cell, material);
            _drawn[crack.Cell] = (crack.Damage, material);
            changed = true;
        }
        _gone.Clear();
        foreach (var (cell, _) in _drawn)
            if (!Holds(cracks, cell)) _gone.Add(cell);
        foreach (int cell in _gone)
        {
            // Gone with its wall: broken out. A crack gone with the wall still there was swept: nothing to show.
            if (_grid.WallAt(cell) == null) Break(cell, _drawn[cell].Material);
            _drawn.Remove(cell);
            changed = true;
        }
        if (changed) QueueRedraw();
    }

    private static bool Holds(IReadOnlyList<Crack> cracks, int cell)
    {
        foreach (var c in cracks)
            if (c.Cell == cell) return true;
        return false;
    }

    public override void _Draw()
    {
        int w = _grid.Width;
        foreach (var (cell, (damage, material)) in _drawn)
        {
            int stage = System.Math.Clamp(damage * Stages / System.Math.Max(1, material.Strength), 0, Stages - 1);
            DrawTexture(Texture(stage, _grid.ShapeAt(cell)), new Vector2(cell % w, cell / w) * Cell);
        }
    }

    /// <summary>A hit deepened the crack in <paramref name="cell"/>: chips of it knocked out and a puff of its dust.</summary>
    private void Hit(int cell, Material material)
    {
        var (chip, dust, _) = LooksOf(material.Id);
        var (x, y, floor) = Where(cell);
        var glims = Glims.Shared;
        glims.Spray(chip, x, y, (int)System.Math.Round(GroundLooks.Chips * Fx.Count), -System.Math.PI / 2, (-1.3, 1.3), (30, 70), floor);
        for (int i = 0; i < System.Math.Round(4 * Fx.Count); i++)
            glims.Add(dust, x + glims.Between(-4, 4), y + glims.Between(-4, 4), glims.Between(-12, 12), -glims.Between(2, 14), size: glims.Between(0.4, 0.7));
    }

    /// <summary><paramref name="cell"/> broken out: a cloud of its dust where it stood, and chips falling.</summary>
    private void Break(int cell, Material material)
    {
        var (chip, dust, cloud) = LooksOf(material.Id);
        var (x, y, floor) = Where(cell);
        if (GroundSounds.BreakOf(material) is { } broke) _breaks.Play(broke, new Vector2((float)x, (float)y));
        var glims = Glims.Shared;
        for (int i = 0; i < System.Math.Round(GroundLooks.BreakDust * Fx.Count); i++)
        {
            double a = glims.Between(0, System.Math.Tau), s = glims.Between(4, 24);
            glims.Add(dust, x + glims.Between(-6, 6), y + glims.Between(-6, 6), System.Math.Cos(a) * s, System.Math.Sin(a) * s,
                bright: glims.Between(0.6, 0.9), size: glims.Between(0.45, 0.8));
        }
        for (int i = 0; i < 4; i++)
            glims.Add(cloud, x + glims.Between(-3, 3), y + glims.Between(-3, 3), glims.Between(-6, 6), -glims.Between(1, 6), size: glims.Between(3, 4.5));
        glims.Spray(chip, x, y, (int)System.Math.Round(GroundLooks.BreakChips * Fx.Count), -System.Math.PI / 2, (-1.6, 1.6), (25, 80), floor);
    }

    /// <summary>The middle of <paramref name="cell"/> in pixels, and the top of the first solid cell under it within six, for chips to land on.</summary>
    private (double X, double Y, double? Floor) Where(int cell)
    {
        int w = _grid.Width, cx = cell % w, cy = cell / w;
        double? floor = null;
        for (int row = cy + 1; row <= cy + 6 && row < _grid.Height; row++)
            if (_grid.StructureAt(row * w + cx) is Structure.Wall or Structure.Floor)
            {
                floor = row * Cell;
                break;
            }
        return ((cx + 0.5) * Cell, (cy + 0.5) * Cell, floor);
    }

    /// <summary>A material's chips, falling as matter does on a planet, its dust and its slow cloud, in its colour.</summary>
    private static (Luminous Chip, Luminous Dust, Luminous Cloud) LooksOf(string material)
    {
        if (Looks.TryGetValue(material, out var made)) return made;
        (int R, int G, int B) c = GroundLooks.Of.TryGetValue(material, out var look) ? look.Colour : (120, 120, 120);
        var fleck = look?.Fleck ?? c;
        var light = (System.Math.Min(255, c.R + 50), System.Math.Min(255, c.G + 50), System.Math.Min(255, c.B + 50));
        return Looks[material] = (
            Luminous.Chip with { Ramp = [fleck, c], Gravity = 420, Bounce = 0.25, Size = 1.6, Low = true },
            Luminous.BreakDust with { Ramp = [light], Gravity = 6, Low = true },
            Luminous.Vapour with { Ramp = [light], Matter = true, Low = true });
    }
}
