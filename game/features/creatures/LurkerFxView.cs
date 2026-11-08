using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The capback's attacks (lurker-attacks), as <see cref="LurkerFx"/> draws them, every one off the sim: its gas over every gas
/// patch, aged by the sim's ticks, over the creatures and the heroes in it, heard hissing when laid; its glob along its lob
/// (<see cref="GlobFlight"/>) while the sim's arc flies, heard hissing as it leaves; its puddle over every slime patch, on the floor
/// under the heroes, heard splatting when laid. Lit parts lit as the world, light masks over them unlit, and their lights.
/// </summary>
public partial class LurkerFxView : Node2D, ILightSource
{
    private const float Cell = ShipCamera.Cell;
    private const string Folder = "creatures/inkLurkerFx/";
    private static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly Registry _m;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private readonly Layer _low = new(Layers.Creatures), _high = new(Layers.Gas);
    private readonly Voices _voices = new(S.World, voices: 4);
    private readonly List<LurkerArt.Light> _glows = [];
    private readonly List<LightSource> _lights = [];

    /// <summary>The patches heard laid, by id; the lobs heard leaving, by id.</summary>
    private readonly HashSet<int> _heardLaid = [], _heardLobbed = [];
    private int _seenTick = -1;

    /// <summary>A depth it draws at: its sprites this frame, its lit sprites and its light masks over them, each from a pool.</summary>
    private sealed class Layer(int z)
    {
        public readonly List<LurkerFx.Sprite> Sprites = [];
        public readonly Node2D Lit = new() { ZIndex = z }, Shines = new() { ZIndex = z };
        public readonly List<Sprite2D> LitPool = [], ShinePool = [];
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public LurkerFxView(Registry m, Func<double> progress, Func<int> tick)
    {
        (_m, _progress, _tick) = (m, progress, tick);
        foreach (var layer in new[] { _low, _high })
        {
            AddChild(layer.Lit);
            AddChild(layer.Shines);
        }
        AddChild(_voices);
    }

    public override void _Process(double delta)
    {
        var creatures = _m.Creatures;
        int tick = _tick();
        if (tick != _seenTick)
        {
            _seenTick = tick;
            Heard(creatures);
        }
        _low.Sprites.Clear();
        _high.Sprites.Clear();
        _glows.Clear();
        double progress = Math.Clamp(_progress(), 0, 1);
        foreach (var patch in creatures.Patches)
        {
            var kind = Spec.PatchOf(patch.Kind);
            double ageS = (kind.Lasts - patch.Ticks + progress) / World.TicksPerSecond, going = LurkerFx.Going((patch.Ticks - progress) / World.TicksPerSecond);
            double x = (patch.Left + patch.Width / 2.0) * Cell;
            if (kind == Spec.Gas)
            {
                // Its feet a cell under the box's middle, its caps a cell and a half over them: the box is laid round its middle cell.
                double feet = (patch.Top + patch.Height / 2.0) * Cell + Cell;
                LurkerFx.Gas(_high.Sprites, _glows, ageS, going, x, feet, x, feet - 1.5 * Cell);
            }
            else LurkerFx.Puddle(_low.Sprites, _glows, ageS, going, x, (patch.Top + patch.Height) * Cell);
        }
        float k = Cell / 1000f;
        foreach (var arc in creatures.Arcs)
        {
            if (arc.Leaves == null || arc.Lingers <= 0) continue;
            double t = (arc.Lingers - arc.Left + progress) / arc.Lingers;
            var (gx, gy, heading) = GlobFlight.At(arc.X0 * k, arc.Y0 * k, arc.X1 * k, arc.Y1 * k, t);
            LurkerFx.Glob(_high.Sprites, _glows, gx, gy, heading, t * arc.Lingers / World.TicksPerSecond);
        }
        Place(_low);
        Place(_high);
        Glow();
    }

    /// <summary>A tick seen: each patch new since heard laid, gas hissing, slime splatting; each lob new since heard leaving.</summary>
    private void Heard(CreaturesModule creatures)
    {
        _heardLaid.RemoveWhere(id => !Lies(creatures, id));
        foreach (var patch in creatures.Patches)
            if (patch.Ticks >= Spec.PatchOf(patch.Kind).Lasts - 1 && _heardLaid.Add(patch.Id))
                _voices.Play(patch.Kind == Spec.Gas.Id ? S.CapbackHiss : S.CapbackSplat,
                    new Vector2((patch.Left + patch.Width / 2f) * Cell, (patch.Top + patch.Height / 2f) * Cell));
        var arcs = creatures.Arcs;
        _heardLobbed.RemoveWhere(id => !Flies(arcs, id));
        foreach (var arc in arcs)
            if (arc.Leaves != null && _heardLobbed.Add(arc.Id))
                _voices.Play(S.CapbackHiss, new Vector2(arc.X0, arc.Y0) * (Cell / 1000f), 1.25f, -4);
    }

    private static bool Lies(CreaturesModule creatures, int id)
    {
        foreach (var patch in creatures.Patches)
            if (patch.Id == id) return true;
        return false;
    }

    private static bool Flies(IReadOnlyList<Arc> arcs, int id)
    {
        foreach (var arc in arcs)
            if (arc.Id == id) return true;
        return false;
    }

    /// <summary>A layer's sprites this frame placed, from its pools, the rest hidden.</summary>
    private static void Place(Layer layer)
    {
        int lit = 0, shines = 0;
        foreach (var s in layer.Sprites)
        {
            if (s.A <= 0.005) continue;
            var sprite = s.Shine != null ? Take(layer.ShinePool, layer.Shines, ref shines, true) : Take(layer.LitPool, layer.Lit, ref lit, false);
            var part = CreatureArt.LurkerFx[s.Part];
            sprite.Texture = PixelArt.Picture(Folder + s.Part);
            sprite.Offset = new Vector2(-part.PivotX, -part.PivotY);
            (sprite.Position, sprite.Rotation, sprite.Scale) = (new Vector2((float)s.X, (float)s.Y), (float)s.R, new Vector2((float)s.Sx, (float)s.Sy));
            sprite.Modulate = s.Shine is { } tint ? new Color((float)tint.R / 255, (float)tint.G / 255, (float)tint.B / 255, (float)s.A) : new Color(1, 1, 1, (float)s.A);
        }
        for (int i = lit; i < layer.LitPool.Count; i++) layer.LitPool[i].Visible = false;
        for (int i = shines; i < layer.ShinePool.Count; i++) layer.ShinePool[i].Visible = false;
    }

    /// <summary>This frame's lights, for the light feature.</summary>
    private void Glow()
    {
        _lights.Clear();
        foreach (var l in _glows)
            if (l.K > 0.01)
                _lights.Add(new LightSource(l.X, l.Y, ((float)l.Colour.R / 255, (float)l.Colour.G / 255, (float)l.Colour.B / 255), CreatureLooks.LurkerLight * (float)l.K,
                    (int)Math.Ceiling(l.R / Cell) + 2, CreatureLooks.LightKeep, 1));
    }

    private static Sprite2D Take(List<Sprite2D> pool, Node2D parent, ref int used, bool shines)
    {
        if (used == pool.Count)
        {
            var made = new Sprite2D { Centered = false };
            if (shines) made.Material = Unshaded;
            else Lit.Set(made);
            parent.AddChild(made);
            pool.Add(made);
        }
        var sprite = pool[used++];
        sprite.Visible = true;
        return sprite;
    }
}
