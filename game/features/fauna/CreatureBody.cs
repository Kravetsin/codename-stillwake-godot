using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// One creature's body (0102) in the design its species names, as the user picked it on the loper page: its rig's parts, each a
/// picture recoloured by the planet's palette (<see cref="CreatureParts"/>), drawn in the rig's order, turned and moved about
/// its pivot by <see cref="CreaturePose"/> at the screen's resolution as a hero's parts are, its origin the middle of its feet,
/// facing right. Lit as the world is; its eye in four states; its glowing parts added over it unlit and lighting what is round
/// them (<see cref="Lights"/>); a blow's white flash an unlit copy over it.
/// </summary>
public partial class CreatureBody : Node2D
{
    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    // Unshaded, so the world's darkness does not grey it: a flash is light.
    private static readonly CanvasItemMaterial FlashMaterial = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>A picture drawn of one node: its colour, its flash, and its glow if it has one.</summary>
    private sealed record Drawn(int Node, string Part, Sprite2D Colour, Sprite2D White, Sprite2D? Glow, (Vector2 At, Color Colour)[] Spots);

    private readonly List<Drawn> _drawn = [];
    private readonly Node2D _flash = new();
    private Transform2D[] _mats = [];
    private int[] _parents = [];
    private FaunaArt.Design? _design;
    private string _palette = "";
    private double _white;

    /// <summary>How far through its death it is, 0 to 1, swelling white and fading; null alive.</summary>
    public double? Dying { get; set; }

    /// <summary>The design it is drawn in.</summary>
    public FaunaArt.Design? Design => _design;

    /// <summary>Dressed as a creature of <paramref name="species"/> in <paramref name="palette"/>: its parts made anew for another design.</summary>
    public void Dress(string species, string palette)
    {
        var design = FaunaArt.Designs[Spec.Fauna.Get(species).Look];
        (Dying, Modulate, _white) = (null, Colors.White, 0);
        if (design != _design || palette != _palette) Make(design, palette);
        Fade(1);
    }

    private void Make(FaunaArt.Design design, string palette)
    {
        (_design, _palette) = (design, palette);
        foreach (var child in GetChildren())
            if (child != _flash) child.QueueFree();
        foreach (var child in _flash.GetChildren()) child.QueueFree();
        _drawn.Clear();
        var index = CreaturePose.Indexed(design);
        _parents = new int[design.Rig.Count];
        for (int i = 0; i < design.Rig.Count; i++) _parents[i] = design.Rig[i].Parent is { } p ? index[p] : -1;
        _mats = new Transform2D[design.Rig.Count];
        for (int i = 0; i < design.Rig.Count; i++)
        {
            var node = design.Rig[i];
            if (node.Drive == FaunaArt.Drive.Eye)
                foreach (var part in FaunaArt.EyeParts) Add(i, part);
            else Add(i, node.Part);
        }
        // Glows over the whole body, as the page adds them, then the flash over all.
        foreach (var d in _drawn)
            if (d.Glow != null) AddChild(d.Glow);
        foreach (var d in _drawn) _flash.AddChild(d.White);
        if (_flash.GetParent() == null) AddChild(_flash);
        else MoveChild(_flash, GetChildCount() - 1);
    }

    private void Add(int node, string part)
    {
        var design = _design!;
        var p = design.Parts[part];
        Sprite2D Sprite(CreatureParts.Kind kind) => new()
        {
            Texture = CreatureParts.Of(design, part, _palette, kind),
            Centered = false,
            Offset = new Vector2(-p.PivotX, -p.PivotY),
        };
        var colour = Sprite(CreatureParts.Kind.Colour);
        Lit.Set(colour);
        var white = Sprite(CreatureParts.Kind.White);
        white.Material = FlashMaterial;
        Sprite2D? glow = null;
        if (p.Glow)
        {
            glow = Sprite(CreatureParts.Kind.Glow);
            glow.Material = GlowMaterial;
        }
        AddChild(colour);
        _drawn.Add(new Drawn(node, part, colour, white, glow, CreatureParts.SpotsOf(design, part, _palette)));
    }

    /// <summary>
    /// Posed by <paramref name="pose"/> this frame, <paramref name="white"/> its flash, 0 to 1. Every part's place is its parent's
    /// and its own turn about its pivot; the eye's pictures by its state, the pupil on where it looks, whole pixels.
    /// </summary>
    public void Pose(CreaturePose pose, double white)
    {
        var design = _design!;
        var rig = design.Rig;
        for (int i = 0; i < rig.Count; i++)
        {
            var n = rig[i];
            var j = pose.Joints[i];
            var local = new Transform2D((float)j.A, new Vector2(n.X + (float)j.X, n.Y + (float)j.Y));
            _mats[i] = _parents[i] < 0 ? local : _mats[_parents[i]] * local;
        }
        var eye = pose.Eye;
        bool wide = eye == CreaturePose.EyeState.Wide;
        var travel = wide ? design.Eye.TravelWide : design.Eye.Travel;
        var look = pose.Look;
        var pupil = new Vector2(MathF.Round((float)look.X * travel.X), MathF.Round((float)look.Y * travel.Y + (eye == CreaturePose.EyeState.Half ? 1 : 0)));
        _white = white;
        foreach (var d in _drawn)
        {
            var mat = _mats[d.Node];
            bool shown = d.Part switch
            {
                "eye_open" => !wide,
                "eye_wide" => wide,
                "pupil" => !wide && eye != CreaturePose.EyeState.Blink,
                "pupil_wide" => wide,
                "lid_half" => eye == CreaturePose.EyeState.Half,
                "lid_blink" => eye == CreaturePose.EyeState.Blink,
                _ => true,
            };
            if (d.Part is "pupil" or "pupil_wide") mat *= Transform2D.Identity.Translated(pupil);
            d.Colour.Visible = shown;
            d.Colour.Transform = mat;
            d.White.Visible = shown && white > 0.01;
            d.White.Transform = mat;
            if (d.Glow != null) d.Glow.Transform = mat;
        }
        _flash.Visible = white > 0.01;
        _flash.Modulate = new Color(1, 1, 1, (float)white);
    }

    /// <summary>
    /// Adds the light of its glowing parts, its brightest <see cref="FaunaLooks.MostLights"/> spots, at where they are drawn in the
    /// world through <paramref name="toWorld"/>, faded with it: what its glow lights round it.
    /// </summary>
    public void Lights(List<(Vector2 At, Color Colour)> into, Transform2D toWorld, float alpha)
    {
        int n = 0;
        foreach (var d in _drawn)
        {
            if (d.Glow == null || !d.Colour.Visible) continue;
            foreach (var (at, colour) in d.Spots)
            {
                if (n++ >= FaunaLooks.MostLights) return;
                into.Add((toWorld * (_mats[d.Node] * at), colour with { A = colour.A * alpha }));
            }
        }
    }

    /// <summary>Drawn at <paramref name="alpha"/>, dying: its colours and glows fading, its flash swelling white then gone.</summary>
    public void Fade(float alpha)
    {
        var shade = new Color(1, 1, 1, alpha);
        foreach (var d in _drawn)
        {
            d.Colour.Modulate = shade;
            if (d.Glow != null) d.Glow.Modulate = shade;
        }
        float white = (float)Math.Max(_white, Dying is double t ? Math.Min(1, 1.6 * (1 - t)) * (1 - t) : 0);
        foreach (var d in _drawn) d.White.Visible = d.Colour.Visible && white > 0.01;
        _flash.Visible = white > 0.01;
        _flash.Modulate = new Color(1, 1, 1, white);
    }
}
