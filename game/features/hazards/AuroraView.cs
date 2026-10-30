using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Hazards;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// A magnetic flare seen coming and striking (2026-10-30-magnetic-flare): through its warning an aurora's curtains spread over the
/// stars, green over violet, rising toward the strike; at the strike, a white flash over the whole world. Behind the ship the sky
/// shows only through what is open to space. It reads the sim's flare once a tick.
/// </summary>
public partial class AuroraView : Node
{
    private static readonly StringName Strength = "strength", Time = "time";

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

    private readonly HazardsModule _hazards;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private readonly CanvasLayer _sky = new() { Layer = ScreenLayers.Stars }, _over = new() { Layer = ScreenLayers.FlareFlash };
    private readonly ColorRect _curtains = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly ColorRect _flash = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0.92f, 0.96f, 1f, 0) };
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Code } };
    private int _read = -1;
    private bool _coming;
    private double _timeMs, _sinceStrike = double.MaxValue;

    public AuroraView(HazardsModule hazards, Func<double> progress, Func<int> tick)
    {
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
        _curtains.Visible = _flash.Visible = false;
    }

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _sinceStrike += delta * 1000;
        if (_tick() != _read)
        {
            _read = _tick();
            bool coming = _hazards.FlareComing;
            // A warning ends only by its strike.
            if (_coming && !coming) _sinceStrike = 0;
            _coming = coming;
        }
        double near = _coming ? FlareLook.Near(_hazards.FlareLeft, _progress(), _hazards.FlareRules.Warning) : 0;
        // The curtains fade as the flash does, so the sky does not snap clear.
        double strength = Math.Max(_coming ? 0.25 + 0.75 * near : 0, FlareLook.Flash(_sinceStrike));
        _curtains.Visible = strength > 0;
        if (_curtains.Visible)
        {
            _material.SetShaderParameter(Strength, (float)strength);
            _material.SetShaderParameter(Time, (float)(_timeMs / 1000));
        }
        double flash = FlareLook.Flash(_sinceStrike);
        _flash.Visible = flash > 0;
        _flash.Color = _flash.Color with { A = (float)(0.85 * flash) };
    }
}
