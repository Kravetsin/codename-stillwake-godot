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
/// The capback's attacks (lurker-attacks), as <see cref="LurkerFx"/> draws them: its gas over every gas patch, aged by the sim's ticks,
/// heard hissing when laid; its glob flown from its mouth to its puddle (<see cref="GlobFlight"/>), heard hissing as it leaves and
/// splatting as it lands; its puddle over every slime patch, from the glob's landing. The sim lays the puddle as it strikes the arc,
/// so the client holds it back while the glob flies. Lit parts lit as the world, light masks over them unlit, and their lights.
/// </summary>
public partial class LurkerFxView : Node2D, ILightSource
{
    private const float Cell = ShipCamera.Cell;
    private const string Folder = "creatures/inkLurkerFx/";
    private static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly Registry _m;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private readonly Node2D _lit = new(), _shines = new();
    private readonly List<Sprite2D> _litPool = [], _shinePool = [];
    private readonly Voices _voices = new(S.World, voices: 4);
    private readonly List<LurkerFx.Sprite> _sprites = [];
    private readonly List<LurkerArt.Light> _glows = [];
    private readonly List<LightSource> _lights = [];

    /// <summary>The gas patches heard hissing, by id; the slime arcs flown, by id.</summary>
    private readonly HashSet<int> _hissed = [], _flown = [];
    /// <summary>The globs flying or landed, each with its puddle's patch, or -1 for none.</summary>
    private readonly List<Glob> _globs = [];
    private double _clock;
    private int _seenTick = -1;

    private sealed record Glob(int Patch, Vector2 From, Vector2 To, double At, double Seconds)
    {
        public bool Landed { get; set; }
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public LurkerFxView(Registry m, Func<double> progress, Func<int> tick)
    {
        (_m, _progress, _tick) = (m, progress, tick);
        AddChild(_lit);
        AddChild(_shines);
        AddChild(_voices);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        var creatures = _m.Creatures;
        int tick = _tick();
        if (tick != _seenTick)
        {
            _seenTick = tick;
            Heard(creatures);
        }
        _sprites.Clear();
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
                LurkerFx.Gas(_sprites, _glows, ageS, going, x, feet, x, feet - 1.5 * Cell);
            }
            else LurkerFx.Puddle(_sprites, _glows, Landed(patch.Id) is { } at ? _clock - at : ageS, going, x, (patch.Top + patch.Height) * Cell);
        }
        foreach (var glob in _globs)
        {
            if (glob.Landed) continue;
            double t = _clock - glob.At;
            if (t >= glob.Seconds)
            {
                glob.Landed = true;
                _voices.Play(S.CapbackSplat, glob.To);
                continue;
            }
            var (gx, gy, heading) = GlobFlight.At(glob.From.X, glob.From.Y, glob.To.X, glob.To.Y, t / glob.Seconds);
            LurkerFx.Glob(_sprites, _glows, gx, gy, heading, t);
        }
        _globs.RemoveAll(g => g.Landed && _clock - g.At > 1 && !Lies(creatures, g.Patch));
        Place();
    }

    /// <summary>A tick seen: each new gas patch heard hissing; each new slime arc flown toward the puddle laid with it, heard leaving.</summary>
    private void Heard(CreaturesModule creatures)
    {
        _hissed.RemoveWhere(id => !Lies(creatures, id));
        foreach (var patch in creatures.Patches)
            if (patch.Kind == Spec.Gas.Id && patch.Ticks >= Spec.Gas.Lasts - 1 && _hissed.Add(patch.Id))
                _voices.Play(S.CapbackHiss, new Vector2((patch.Left + patch.Width / 2f) * Cell, (patch.Top + patch.Height / 2f) * Cell));
        float k = Cell / 1000f;
        var arcs = creatures.Arcs;
        _flown.RemoveWhere(id => !Struck(arcs, id));
        foreach (var arc in arcs)
        {
            if (arc.Projectile != Stillwake.Content.Shots.Glob.Id || !_flown.Add(arc.Id)) continue;
            var from = new Vector2(arc.X0, arc.Y0) * k;
            var (patch, to) = PuddleOf(creatures, new Vector2(arc.X1, arc.Y1) * k);
            _globs.Add(new Glob(patch, from, to, _clock, GlobFlight.Seconds(from.X, from.Y, to.X, to.Y)));
            _voices.Play(S.CapbackHiss, from, 1.25f, -4);
        }
    }

    /// <summary>The slime patch just laid that no glob flies to yet, and where on its floor the glob lands; none, where the arc ended.</summary>
    private (int Patch, Vector2 To) PuddleOf(CreaturesModule creatures, Vector2 end)
    {
        Patch? found = null;
        foreach (var patch in creatures.Patches)
            if (patch.Kind == Spec.Slime.Id && patch.Ticks >= Spec.Slime.Lasts - 1 && !Flies(patch.Id)) found = patch;
        return found == null ? (-1, end) : (found.Id, new Vector2((found.Left + found.Width / 2f) * Cell, (found.Top + found.Height) * Cell - 1));
    }

    /// <summary>When the glob flying to puddle <paramref name="patch"/> lands, by this view's clock; null for a puddle with none (one lying before it was seen).</summary>
    private double? Landed(int patch)
    {
        foreach (var glob in _globs)
            if (glob.Patch == patch) return glob.At + glob.Seconds;
        return null;
    }

    private bool Flies(int patch)
    {
        foreach (var glob in _globs)
            if (glob.Patch == patch) return true;
        return false;
    }

    private static bool Lies(CreaturesModule creatures, int id)
    {
        foreach (var patch in creatures.Patches)
            if (patch.Id == id) return true;
        return false;
    }

    private static bool Struck(IReadOnlyList<Arc> arcs, int id)
    {
        foreach (var arc in arcs)
            if (arc.Id == id) return true;
        return false;
    }

    /// <summary>This frame's sprites placed, from the pools, the rest hidden; and their lights.</summary>
    private void Place()
    {
        int lit = 0, shines = 0;
        foreach (var s in _sprites)
        {
            if (s.A <= 0.005) continue;
            var sprite = s.Shine is { } c ? Take(_shinePool, _shines, ref shines, true) : Take(_litPool, _lit, ref lit, false);
            var part = CreatureArt.LurkerFx[s.Part];
            sprite.Texture = PixelArt.Picture(Folder + s.Part);
            sprite.Offset = new Vector2(-part.PivotX, -part.PivotY);
            (sprite.Position, sprite.Rotation, sprite.Scale) = (new Vector2((float)s.X, (float)s.Y), (float)s.R, new Vector2((float)s.Sx, (float)s.Sy));
            sprite.Modulate = s.Shine is { } tint ? new Color((float)tint.R / 255, (float)tint.G / 255, (float)tint.B / 255, (float)s.A) : new Color(1, 1, 1, (float)s.A);
        }
        for (int i = lit; i < _litPool.Count; i++) _litPool[i].Visible = false;
        for (int i = shines; i < _shinePool.Count; i++) _shinePool[i].Visible = false;
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
