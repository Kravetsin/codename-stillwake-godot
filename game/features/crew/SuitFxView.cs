using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit's frost and heat on a figure (<see cref="SuitFx"/>, body-temperature): while either shows, each suit part is drawn with a
/// lit material of its own that greys or chars its picture, the char's rims and embers glowing past the light; and the sparks off it,
/// drawn added over the figure in the world, each left where it rose from. With neither, the parts have their plain material back.
/// </summary>
public sealed partial class SuitFxView : Node2D
{
    private static string F(float v)
    {
        var s = v.ToString(CultureInfo.InvariantCulture);
        return s.Contains('.') ? s : s + ".0";
    }

    private const string Declarations = """

            uniform float suit_frost = 0.0;
            // The char's threshold (SuitFx.Scorch): none at or under 0.
            uniform float suit_scorch = -1.0;
            // Where this part's noise starts, in the world's pixels, and how many frames its picture's strip holds.
            uniform vec2 suit_seed = vec2(0.0);
            uniform float suit_frames = 1.0;
            // How far inside the picture each pixel lies, whole at SuitFx.EdgeReach.
            uniform sampler2D suit_edge : filter_linear, repeat_disable, hint_default_white;

            float suit_hash(vec2 p) {
                vec3 q = fract(vec3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return fract((q.x + q.y) * q.z);
            }

            float suit_noise(vec2 p) {
                vec2 i = floor(p);
                vec2 f = p - i;
                vec2 u = f * f * (3.0 - 2.0 * f);
                return mix(mix(suit_hash(i), suit_hash(i + vec2(1.0, 0.0)), u.x), mix(suit_hash(i + vec2(0.0, 1.0)), suit_hash(i + vec2(1.0, 1.0)), u.x), u.y);
            }

            // Three octaves, spread back out to about 0 to 1.
            float suit_fbm(vec2 p) {
                float v = 0.57 * suit_noise(p) + 0.29 * suit_noise(p * 2.03 + vec2(17.1, 9.2)) + 0.14 * suit_noise(p * 4.1 + vec2(3.3, 41.7));
                return clamp((v - 0.5) * 2.3 + 0.5, 0.0, 1.0);
            }
            """;

    private const string Vertex = """

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }
            """;

    private static readonly string Paint = $$"""
                vec4 suit_glow = vec4(0.0);
                if (suit_frost > 0.0 || suit_scorch > 0.0) {
                    vec2 suit_size = vec2(textureSize(TEXTURE, 0));
                    vec2 suit_px = UV * suit_size;
                    suit_px.x = mod(suit_px.x, suit_size.x / suit_frames);
                    float suit_a = texture(TEXTURE, UV).a;
                    float suit_h1 = suit_hash(floor(suit_px) + suit_seed * 23.0);
                    float suit_h2 = suit_hash(floor(suit_px / 2.0) + suit_seed * 7.0);
                    if (suit_frost > 0.0) {
                        float s = suit_frost;
                        vec3 c = mix(COLOR.rgb, vec3(dot(COLOR.rgb, vec3(0.299, 0.587, 0.114))), {{F(SuitFx.Grey)}} * s);
                        c = c * vec3(1.0 - 0.15 * s, 1.0 - 0.02 * s, 1.0 + 0.12 * s) + vec3(0.13, 0.18, 0.25) * s;
                        if (suit_a >= 0.5 && suit_h2 < {{F(SuitFx.Specks)}} * s) {
                            float twinkle = 0.5 + 0.5 * sin(TIME * {{F(SuitFx.Twinkle)}} + fract(suit_h2 * 137.1) * 40.0);
                            c = vec3(0.92, 0.97, 1.0) * (twinkle > 0.82 ? 1.0 : 0.84);
                        }
                        COLOR.rgb = clamp(c, 0.0, 1.0);
                    }
                    if (suit_scorch > 0.0 && suit_a >= 0.5) {
                        float score = 0.7 * suit_fbm((suit_px / {{F(SuitFx.Scale)}} + suit_seed) * 0.55) + 0.3 * texture(suit_edge, UV).r;
                        if (score < suit_scorch) {
                            COLOR.rgb = COLOR.rgb * vec3(0.36, 0.27, 0.24) * (0.9 + 0.2 * suit_h1) + vec3(0.03, 0.015, 0.01);
                            if (suit_scorch - score < {{F(SuitFx.Rim)}})
                                suit_glow = vec4(vec3(1.0, 0.45, 0.1) * (0.75 + 0.25 * sin(TIME * 5.0 + suit_h2 * 30.0)), 1.0);
                            else if (suit_h2 < {{F(SuitFx.Embers)}} && sin(TIME * 3.0 + suit_h2 * 400.0) > 0.2)
                                suit_glow = vec4(1.0, 0.55, 0.15, 1.0);
                        }
                    }
                }
            """;

    // The smouldering rims and embers give their own light: drawn as they are, whatever light falls there.
    private const string After = """
                if (suit_glow.a > 0.0) COLOR.rgb = suit_glow.rgb;
            """;

    private static ShaderMaterial? _first;
    private static readonly Material Added = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly Color Pale = new("ffe0a0"), Ember = new("ff6020");

    /// <summary>A suit picture's edge field for the shader and the pixels sparks leave from, by the picture; none for one not made by <see cref="SuitArt"/>.</summary>
    private static readonly Dictionary<Texture2D, Field?> Fields = new();

    private sealed record Field(ImageTexture Edge, int[] Inside, int Width);

    private sealed class Slot(Sprite2D sprite, bool hand)
    {
        public readonly Sprite2D Sprite = sprite;
        public readonly bool Hand = hand;
        public Material? Was;
        public ShaderMaterial? Own;
        public Texture2D? Texture;
        public (float Frost, float Scorch) Set = (0, -1);
        public bool On;
    }

    private readonly Slot[] _slots;
    private readonly Func<Sprite2D, Vector2, Vector2> _place;
    private readonly (long Cycle, Vector2 From)?[] _from = new (long, Vector2)?[SuitFx.Sparks];
    private double _seconds;
    private float _heat;

    /// <param name="parts">The suit's parts in a fixed order, each marked if it is a hand, which a bare one leaves as skin.</param>
    /// <param name="place">Where a point of a part's sprite, in its own frame, is in the world as the part is drawn now.</param>
    public SuitFxView(IReadOnlyList<(Sprite2D Sprite, bool Hand)> parts, Func<Sprite2D, Vector2, Vector2> place)
    {
        _slots = new Slot[parts.Count];
        for (int i = 0; i < parts.Count; i++) _slots[i] = new Slot(parts[i].Sprite, parts[i].Hand);
        _place = place;
        (TopLevel, ZIndex, Material, Visible) = (true, 1, Added, false);
        SetProcess(false);
    }

    /// <summary>The suit as <paramref name="frost"/> and <paramref name="heat"/> show it, each 0 to 1; its hands left bare while <paramref name="bareHands"/>.</summary>
    public void Show(float frost, float heat, bool bareHands)
    {
        frost = Math.Clamp(frost, 0, 1);
        heat = Math.Clamp(heat, 0, 1);
        bool on = frost > 0 || heat > 0;
        float scorch = SuitFx.Scorch(heat);
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (on && !(slot.Hand && bareHands)) Wear(slot, i, frost, scorch);
            else if (slot.On) (slot.Sprite.Material, slot.On) = (slot.Was, false);
        }
        _heat = heat;
        bool sparks = heat > 0;
        if (sparks != Visible)
        {
            Visible = sparks;
            SetProcess(sparks);
            Array.Clear(_from);
        }
    }

    private static void Wear(Slot slot, int index, float frost, float scorch)
    {
        var sprite = slot.Sprite;
        if (!slot.On)
        {
            slot.Was = sprite.Material;
            if (slot.Own == null)
            {
                _first ??= Lit.Painted(Declarations, Vertex, Paint, 1f, After);
                slot.Own = Lit.Like(_first, 1f);
                var (x, y) = SuitFx.Seed(index);
                slot.Own.SetShaderParameter("suit_seed", new Vector2(x, y));
            }
            (sprite.Material, slot.On) = (slot.Own, true);
        }
        var own = slot.Own!;
        if (sprite.Texture != slot.Texture)
        {
            slot.Texture = sprite.Texture;
            own.SetShaderParameter("suit_edge", slot.Texture != null && FieldOf(slot.Texture) is { } field ? field.Edge : default(Variant));
            own.SetShaderParameter("suit_frames", (float)Math.Max(1, sprite.Hframes));
        }
        if (slot.Set == (frost, scorch)) return;
        slot.Set = (frost, scorch);
        own.SetShaderParameter("suit_frost", frost);
        own.SetShaderParameter("suit_scorch", scorch);
    }

    private static Field? FieldOf(Texture2D texture)
    {
        if (Fields.TryGetValue(texture, out var known)) return known;
        Field? made = null;
        if (SuitArt.PixelsOf(texture) is var (w, h, data))
        {
            var distances = SuitFx.EdgeDistances(data, w, h);
            made = new Field(ImageTexture.CreateFromImage(Image.CreateFromData(w, h, false, Image.Format.R8, SuitFx.EdgeBytes(distances))), SuitFx.Inside(distances), w);
        }
        return Fields[texture] = made;
    }

    public override void _Process(double delta)
    {
        _seconds += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float facing = GetParent<Node2D>().GlobalTransform.X.X < 0 ? -1 : 1;
        float zoom = Math.Max(GetGlobalTransformWithCanvas().X.Length(), 0.01f);
        float r = Math.Max(SuitFx.SparkRadius, SuitFx.SparkLeast / zoom);
        for (int i = 0; i < SuitFx.Sparks; i++)
        {
            if (SuitFx.SparkAt(i, _seconds, _heat) is not { } spark)
            {
                _from[i] = null;
                continue;
            }
            if (_from[i] is not { } from || from.Cycle != spark.Cycle)
            {
                if (OriginOf(spark.Pick, spark.Spot) is not { } at) continue;
                _from[i] = from = (spark.Cycle, at);
            }
            var p = from.From + new Vector2((float)(-spark.Back * facing), (float)-spark.Up);
            var colour = Pale.Lerp(Ember, (float)spark.Age) with { A = (float)spark.Alpha };
            DrawRect(new Rect2(p - new Vector2(r, r), new Vector2(2 * r, 2 * r)), colour);
        }
    }

    /// <summary>A place on the suit as drawn now, chosen by <paramref name="pick"/> among its parts by their size and by <paramref name="spot"/> on it; none with no suit showing.</summary>
    private Vector2? OriginOf(double pick, double spot)
    {
        int total = 0;
        foreach (var slot in _slots)
            if (Inside(slot) is { } inside) total += inside.Length;
        if (total == 0) return null;
        int at = (int)(pick * total);
        foreach (var slot in _slots)
        {
            if (Inside(slot) is not { } inside) continue;
            if (at >= inside.Length)
            {
                at -= inside.Length;
                continue;
            }
            var sprite = slot.Sprite;
            var size = sprite.Texture.GetSize();
            int width = Fields[sprite.Texture]!.Width;
            for (int tries = 0; tries < 8; tries++)
            {
                int i = inside[((int)(spot * inside.Length) + tries * 7919) % inside.Length];
                var p = new Vector2(i % width + 0.5f, i / width + 0.5f);
                if (sprite.Hframes > 1) p.X %= size.X / sprite.Hframes;
                else if (sprite.RegionEnabled)
                {
                    if (!sprite.RegionRect.HasPoint(p)) continue;
                    p -= sprite.RegionRect.Position;
                }
                return _place(sprite, sprite.Offset + p);
            }
            return null;
        }
        return null;
    }

    private static int[]? Inside(Slot slot) =>
        slot.On && slot.Sprite.IsVisibleInTree() && slot.Sprite.Texture is { } texture && FieldOf(texture) is { Inside.Length: > 0 } field ? field.Inside : null;
}
