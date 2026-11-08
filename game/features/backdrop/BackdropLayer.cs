using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// One layer of a planet's land (composed-backdrops): its plan laid once into its canvas item, the tile twice side by side,
/// what crosses the tile's edge once more beyond it, then carried by the node's position and scale. Quads of one picture in
/// a row go in one triangle array. A small shader veils it toward a dark and the planet's fog by its depth, then the day's tint.
/// </summary>
public partial class BackdropLayer : Node2D
{
    private static readonly Shader Veil = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform vec3 dark;
            uniform float dark_share;
            uniform vec3 haze;
            uniform float haze_share;
            uniform vec3 tint = vec3(1.0);
            void fragment() {
                vec3 c = mix(mix(COLOR.rgb, dark, dark_share), haze, haze_share);
                COLOR.rgb = c * tint;
            }
            """,
    };

    private readonly BackdropPlan.Layer _plan;
    private readonly double _tile;
    private readonly ShaderMaterial _veil;
    // The ground's pictures as repeating sheets, kept while drawn: a canvas texture freed takes its draws with it.
    private readonly Dictionary<string, CanvasTexture> _sheets = new();

    public BackdropLayer(BackdropPlan.Layer plan, double tile, (float R, float G, float B) haze, (float R, float G, float B) dark)
    {
        _plan = plan;
        _tile = tile;
        Material = _veil = new ShaderMaterial { Shader = Veil };
        _veil.SetShaderParameter("dark", new Vector3(dark.R, dark.G, dark.B));
        _veil.SetShaderParameter("dark_share", plan.Depth.Dark);
        _veil.SetShaderParameter("haze", new Vector3(haze.R, haze.G, haze.B));
        _veil.SetShaderParameter("haze_share", plan.Depth.Haze);
    }

    /// <summary>The day's colour the veiled layer is multiplied by (<c>Daylight.Backdrop</c>).</summary>
    public (float R, float G, float B) Tint
    {
        set => _veil.SetShaderParameter("tint", new Vector3(value.R, value.G, value.B));
    }

    public override void _Draw()
    {
        var batch = new Batch(GetCanvasItem());
        float tile = (float)_tile, bottom = (float)(_plan.Depth.Tall + BackdropLooks.First.Below);
        foreach (var draw in _plan.Draws)
            switch (draw)
            {
                case BackdropPlan.Fill fill:
                    Fill(batch, fill, tile, bottom);
                    break;
                case BackdropPlan.Picture p:
                    var texture = PixelArt.Picture(p.Name);
                    float w = (float)p.W, h = (float)p.H, y = (float)p.Y;
                    float left = (float)(p.X - Math.Floor(p.X / _tile) * _tile);
                    // Copies over the two tiles drawn: what crosses the tile's left edge shows at its right too.
                    for (int k = -1; k <= 1; k++)
                    {
                        float x = left + k * tile;
                        if (x + w <= 0 || x >= 2 * tile) continue;
                        batch.Quad(texture.GetRid(), new Rect2(x, y, w, h), p.Flip);
                    }
                    break;
            }
        batch.Flush();
    }

    /// <summary>A ridge's columns from their tops down past the layer's height, its sheet repeated at the layer's scale, then its wash.</summary>
    private void Fill(Batch batch, BackdropPlan.Fill fill, float tile, float bottom)
    {
        var ridge = fill.Ridge;
        var sheet = Sheet(fill.Sheet);
        var size = sheet.GetSize() * (float)_plan.Depth.Scale;
        float cx = (float)ridge.Cx;
        for (int pass = 0; pass < (fill.Wash.A > 0 ? 2 : 1); pass++)
        {
            var colour = pass == 0 ? Colors.White : new Color(fill.Wash.R, fill.Wash.G, fill.Wash.B, fill.Wash.A);
            var rid = pass == 0 ? sheet.GetRid() : default;
            for (int k = 0; k < 2; k++)
                for (int i = 0; i < ridge.N; i++)
                {
                    float x0 = i * cx, x1 = (i + 1) * cx, top = (float)ridge.Top(i);
                    var at = new Rect2(x0 + k * tile, top, x1 - x0, bottom - top);
                    batch.Quad(rid, at, false, new Rect2(x0 / size.X, top / size.Y, (x1 - x0) / size.X, (bottom - top) / size.Y), colour);
                }
        }
    }

    private CanvasTexture Sheet(string name)
    {
        if (_sheets.TryGetValue(name, out var kept)) return kept;
        var picture = (CanvasTexture)PixelArt.Picture(name);
        return _sheets[name] = new CanvasTexture
        {
            DiffuseTexture = picture.DiffuseTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
        };
    }

    /// <summary>Quads gathered while they share a texture and colour, given to the canvas item as one triangle array.</summary>
    private sealed class Batch(Rid item)
    {
        private readonly List<Vector2> _points = [], _uvs = [];
        private readonly List<int> _indices = [];
        private Rid _texture;
        private Color _colour = Colors.White;

        public void Quad(Rid texture, Rect2 at, bool flip, Rect2? uv = null, Color? colour = null)
        {
            var c = colour ?? Colors.White;
            if (texture != _texture || c != _colour) Flush();
            (_texture, _colour) = (texture, c);
            var u = uv ?? new Rect2(0, 0, 1, 1);
            float u0 = flip ? u.End.X : u.Position.X, u1 = flip ? u.Position.X : u.End.X;
            int first = _points.Count;
            _points.AddRange([at.Position, new Vector2(at.End.X, at.Position.Y), at.End, new Vector2(at.Position.X, at.End.Y)]);
            _uvs.AddRange([new Vector2(u0, u.Position.Y), new Vector2(u1, u.Position.Y), new Vector2(u1, u.End.Y), new Vector2(u0, u.End.Y)]);
            _indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
        }

        public void Flush()
        {
            if (_points.Count == 0) return;
            var colours = new Color[_points.Count];
            Array.Fill(colours, _colour);
            RenderingServer.CanvasItemAddTriangleArray(item, [.. _indices], [.. _points], colours, [.. _uvs], texture: _texture);
            _points.Clear();
            _uvs.Clear();
            _indices.Clear();
        }
    }
}
