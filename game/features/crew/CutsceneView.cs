using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cutscene played for this player, its drone's meeting (drone-meeting): a grim, muted grade over all the scene shows, eased in
/// and out as the scene says, and everything read on the screen moved off it meanwhile (the HUD, the bars, the tablet's edge and its
/// keys, the words by the pointer), put back as it ends. One full-screen quad reading the screen under it; hidden with no cutscene.
/// </summary>
public partial class CutsceneView : CanvasLayer
{
    private static readonly StringName Grading = "grade";

    private const string Shader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear;
        uniform float grade = 0.0;
        const vec3 tint = vec3(212.0, 222.0, 228.0) / 255.0;
        const vec3 shade = vec3(3.0, 4.0, 7.0) / 255.0;

        void fragment() {
            vec3 c = texture(screen, SCREEN_UV).rgb;
            // The pick's filter, in its order: less colour, more contrast, a little darker, a cold tint, a dark vignette.
            c = mix(vec3(dot(c, vec3(0.2126, 0.7152, 0.0722))), c, 1.0 - 0.45 * grade);
            c = (c - 0.5) * (1.0 + 0.08 * grade) + 0.5;
            c *= (1.0 - 0.04 * grade) * mix(vec3(1.0), tint, grade);
            vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
            float far = 0.58 * length(size), d = length(FRAGCOORD.xy - size * 0.5);
            c = mix(c, shade, clamp((d - 0.35 * far) / (0.65 * far), 0.0, 1.0) * 0.5 * grade);
            COLOR = vec4(c, 1.0);
        }
        """;

    private readonly Func<(double Grade, double Hud)> _read;
    private readonly ColorRect _screen = new();
    private readonly ShaderMaterial _material = new() { Shader = new Shader { Code = Shader } };
    private readonly Dictionary<CanvasLayer, Vector2> _moved = [];
    private double _sinceLook;

    /// <summary>Where what is read goes while the scene plays: far off the screen; how often new layers are looked for, seconds.</summary>
    private static readonly Vector2 Away = new(100000, 100000);
    private const double LookEveryS = 0.25;

    /// <param name="read">This player's cutscene's grade, 0..1, and how much of its HUD shows, 0..1; (0, 1) with none.</param>
    public CutsceneView(Func<(double Grade, double Hud)> read)
    {
        _read = read;
        Layer = ScreenLayers.Grade;
        _screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _screen.MouseFilter = Control.MouseFilterEnum.Ignore;
        _screen.Material = _material;
        _screen.Visible = false;
        AddChild(_screen);
    }

    public override void _Process(double delta)
    {
        var (grade, hud) = _read();
        _screen.Visible = grade > 0.002;
        if (_screen.Visible) _material.SetShaderParameter(Grading, (float)grade);
        // The HUD goes as the pick fades it out, and comes back as it fades in: at half.
        if (hud < 0.5)
        {
            _sinceLook -= delta;
            // Looked for again now and then: a panel made meanwhile goes too.
            if (_sinceLook <= 0)
            {
                MoveAway(GetTree().Root);
                _sinceLook = LookEveryS;
            }
            return;
        }
        foreach (var (layer, offset) in _moved)
            if (IsInstanceValid(layer)) layer.Offset = offset;
        _moved.Clear();
        _sinceLook = 0;
    }

    /// <summary>
    /// Moves every layer that is read off the screen, under <paramref name="node"/>: offset, not hidden, since many set their own
    /// visibility each frame; what each was offset by is kept to put it back.
    /// </summary>
    private void MoveAway(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is CanvasLayer layer && layer != this && Read(layer.Layer) && !_moved.ContainsKey(layer))
            {
                _moved[layer] = layer.Offset;
                layer.Offset = Away;
            }
            MoveAway(child);
        }
    }

    /// <summary>Whether a canvas layer at <paramref name="layer"/> is read rather than the scene: the HUD, the bars, a window or the drone's frame, the tablet's edge and its keys, the words by the pointer.</summary>
    private static bool Read(int layer) => layer is ScreenLayers.Hud or ScreenLayers.Bars or ScreenLayers.Handbook or ScreenLayers.TabletEdge or ScreenLayers.Tips;
}
