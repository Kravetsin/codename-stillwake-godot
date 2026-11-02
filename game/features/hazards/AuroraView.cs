using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Power;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// A magnetic flare seen coming and striking (2026-10-30-magnetic-flare): through its warning an aurora's curtains come in over
/// the stars, green over violet, swaying slowly, growing toward the strike; at the strike, a white flash eased over the whole
/// world and out, the aurora fading after it, and each machine it broke heard bursting where it stands, one after another, a
/// lamp's glass tinkling too. Behind the ship the sky shows only through what is open to space. It reads the sim once a tick.
/// </summary>
public partial class AuroraView : Node
{
    private const int Cell = ShipCamera.Cell;
    private static readonly StringName Strength = "strength", Time = "time";
    /// <summary>How fast the curtains sway and their rays drift, against the shader's own pace: slow, as a sky's light moves.</summary>
    private const double Pace = 0.35;
    /// <summary>Over how long the bursts after a strike are spread, in ms: a run of pops, not one bang.</summary>
    private const double BurstsOverMs = 600;

    private const string Code = """
        shader_type canvas_item;
        uniform float strength = 0.0;
        uniform float time = 0.0;

        // One curtain hanging from base line y0: a sharp lower hem, fading upward, combed into rays that drift along it.
        float curtain(vec2 uv, float y0, float speed, float seed) {
            float x = uv.x * 3.0 + seed;
            float hem = y0 + sin(x * 2.1 + time * 0.3 * speed) * 0.07 + sin(x * 5.3 - time * 0.5 * speed) * 0.025;
            float d = uv.y - hem;
            float body = smoothstep(-0.32, 0.0, d) * (1.0 - smoothstep(0.0, 0.05, d));
            float rays = 0.55 + 0.45 * sin(x * 38.0 + sin(x * 6.0 + time * speed) * 3.0 + time * 0.7 * speed);
            float sheet = 0.55 + 0.45 * sin(x * 1.3 - time * 0.2 * speed + seed);
            return body * rays * sheet;
        }

        void fragment() {
            float a = curtain(UV, 0.34, 1.0, 0.0) + 0.6 * curtain(UV, 0.5, 1.4, 2.7);
            // Green along the hem, violet up its folds.
            float up = clamp((0.42 - UV.y) / 0.3, 0.0, 1.0);
            vec3 colour = mix(vec3(0.35, 1.0, 0.6), vec3(0.62, 0.38, 1.0), up);
            COLOR = vec4(colour, clamp(a, 0.0, 1.0) * strength * 0.6);
        }
        """;

    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly HazardsModule _hazards;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private readonly CanvasLayer _sky = new() { Layer = ScreenLayers.Stars }, _over = new() { Layer = ScreenLayers.FlareFlash };
    private readonly ColorRect _curtains = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly ColorRect _flash = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0.92f, 0.96f, 1f, 0) };
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Code } };
    private readonly Voices _bursts = new(S.World, voices: 6);
    private readonly Random _random = new();
    /// <summary>The machines broken as of the last tick read: those new at a strike are what it broke.</summary>
    private readonly HashSet<int> _broken = [];
    /// <summary>Bursts still to be heard: in how many ms, where, and whether a lamp's glass goes with it.</summary>
    private readonly List<(double InMs, Vector2 At, bool Glass)> _due = [];
    private int _read = -1;
    private bool _coming;
    private double _timeMs, _sinceStrike = double.MaxValue, _struckAt;

    public AuroraView(IGridQueries grid, IPowerQueries power, HazardsModule hazards, Func<double> progress, Func<int> tick)
    {
        _grid = grid;
        _power = power;
        _hazards = hazards;
        _progress = progress;
        _tick = tick;
        _curtains.Material = _material;
        _curtains.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _flash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _sky.AddChild(_curtains);
        _over.AddChild(_flash);
        AddChild(_sky);
        AddChild(_over);
        AddChild(_bursts);
        _curtains.Visible = _flash.Visible = false;
        _broken.UnionWith(power.Broken);
    }

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        _timeMs += ms;
        _sinceStrike += ms;
        if (_tick() != _read)
        {
            _read = _tick();
            bool coming = _hazards.FlareComing;
            // A warning ends only by its strike.
            if (_coming && !coming) Strike();
            _coming = coming;
            _broken.Clear();
            _broken.UnionWith(_power.Broken);
        }
        double near = _coming ? FlareLook.Near(_hazards.FlareLeft, _progress(), _hazards.FlareRules.Warning) : 0;
        double strength = _coming ? FlareLook.Aurora(near) : _struckAt * FlareLook.AuroraLeft(_sinceStrike);
        if (_coming) _struckAt = strength;
        _curtains.Visible = strength > 0.001;
        if (_curtains.Visible)
        {
            _material.SetShaderParameter(Strength, (float)strength);
            _material.SetShaderParameter(Time, (float)(_timeMs / 1000 * Pace));
        }
        double flash = FlareLook.Flash(_sinceStrike);
        _flash.Visible = flash > 0;
        _flash.Color = _flash.Color with { A = (float)(0.85 * flash) };
        Burst(ms);
    }

    /// <summary>The strike: the flash begins, and every machine broken since the last tick is due to burst, each a moment apart.</summary>
    private void Strike()
    {
        _sinceStrike = 0;
        foreach (int id in _power.Broken)
        {
            if (_broken.Contains(id) || _grid.MachineById(id) is not { } machine) continue;
            var spec = Content.Machines.Get(machine.Kind);
            var at = new Vector2((machine.Cell % _grid.Width + spec.Width / 2f) * Cell, (machine.Cell / _grid.Width + spec.Height / 2f) * Cell);
            _due.Add((_random.NextDouble() * BurstsOverMs, at, Content.Machines.Is(machine.Kind, Content.MachineTrait.Glow)));
        }
    }

    /// <summary>
    /// The bursts come due: an electric crack where the machine stands, pitched up and each a little apart, the resonator's
    /// crackle (<see cref="S.Zap"/>); a lamp's glass tinkling high and soft with it (<see cref="S.GlassBreak"/>).
    /// </summary>
    private void Burst(double ms)
    {
        for (int i = _due.Count - 1; i >= 0; i--)
        {
            var (inMs, at, glass) = _due[i];
            if ((inMs -= ms) > 0)
            {
                _due[i] = (inMs, at, glass);
                continue;
            }
            _due.RemoveAt(i);
            _bursts.Play(S.Zap, at, tune: 1.25f + 0.45f * _random.NextSingle(), offsetDb: 3);
            if (glass) _bursts.Play(S.GlassBreak, at, tune: 1.8f + 0.3f * _random.NextSingle(), offsetDb: -17);
        }
    }
}
