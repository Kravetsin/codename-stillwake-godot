using System.Collections.Generic;
using System.Linq;
using Godot;
using static Stillwake.Game.Backdrop;

namespace Stillwake.Game.Render;

/// <summary>
/// Wreckage far behind the ship (render/README.md): each wreck a core of one of the wrecks' own pictures
/// with up to <see cref="WreckPiecesMost"/> pieces of debris gathered round it, all grey, dark and
/// soft — made once into an atlas from <c>art/debris</c>, drawn small and blended down its mipmaps —
/// so they read as the same wrecks, out of reach. Never outlined, never lit.
/// </summary>
public partial class FarWrecks : DriftLayer
{
    private const uint Seed = 11;
    /// <summary>Clear texels round each picture in the atlas, so its mipmaps do not bleed into the next.</summary>
    private const int Pad = 4;
    private static readonly Color Space = ProjectSettings.GetSetting("rendering/environment/defaults/default_clear_color").AsColor();

    private readonly Texture2D _atlas;
    private readonly Rect2[] _cores, _pieces;

    public FarWrecks() : base(Seed, WreckTile, WreckMean, WreckParallax, WreckDrift, Reach)
    {
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        var cores = DebrisLooks.Of.Where(k => DebrisLooks.Wrecks.ContainsKey(k.Key)).SelectMany(k => Paths(k.Value)).ToList();
        var pieces = DebrisLooks.Of.Where(k => !DebrisLooks.Wrecks.ContainsKey(k.Key)).SelectMany(k => Paths(k.Value)).ToList();
        (_atlas, var regions) = Atlas(cores.Concat(pieces).ToList());
        _cores = regions[..cores.Count];
        _pieces = regions[cores.Count..];
    }

    /// <summary>The farthest a wreck reaches from its middle, in its plane's pixels: the largest core's half-diagonal and a piece's beyond it, at the largest.</summary>
    private static readonly float Reach = (DebrisLooks.Wrecks.Values.Max(s => new Vector2(s.W, s.H).Length() / 2) + 26) * WreckScaleMax;

    private static IEnumerable<string> Paths((string Look, int Count) look) =>
        Enumerable.Range(1, look.Count).Select(n => $"res://art/debris/{look.Look}_{n:00}.png");

    protected override void DrawSpot(Vector2 at, uint hash)
    {
        float big = Scatter.Next(ref hash);
        float scale = Mathf.Lerp(WreckScaleMin, WreckScaleMax, big);
        float turn = Scatter.Next(ref hash) * Mathf.Tau;
        bool flip = Scatter.Next(ref hash) < 0.5f;
        // The biggest are the nearest: a little lighter, standing out of space more.
        var tint = new Color(WreckTint.R, WreckTint.G, WreckTint.B);
        var colour = Space.Lerp(tint, Mathf.Lerp(WreckFar, WreckNear, big));
        var whole = new Transform2D(turn, new Vector2(flip ? -scale : scale, scale), 0, at);
        var core = _cores[(int)(Scatter.Next(ref hash) * _cores.Length)];
        int count = (int)(Scatter.Next(ref hash) * (WreckPiecesMost + 1));
        // Pieces first, the core over them, so it keeps its shape.
        for (int i = 0; i < count; i++)
        {
            var piece = _pieces[(int)(Scatter.Next(ref hash) * _pieces.Length)];
            float round = Scatter.Next(ref hash) * Mathf.Tau;
            // Centred a little past the core's rim, so most of the piece stands out of it.
            var reach = (core.Size + piece.Size) / 2 - new Vector2(Pad * 2, Pad * 2) - piece.Size / 4;
            var off = new Vector2(Mathf.Cos(round) * reach.X, Mathf.Sin(round) * reach.Y);
            DrawPicture(piece, whole * new Transform2D(Scatter.Next(ref hash) * Mathf.Tau, off), colour);
        }
        DrawPicture(core, whole, colour);
    }

    private void DrawPicture(Rect2 region, Transform2D at, Color colour)
    {
        DrawSetTransformMatrix(at);
        DrawTextureRectRegion(_atlas, new Rect2(-region.Size / 2, region.Size), region, colour);
    }

    /// <summary>
    /// The pictures at <paramref name="paths"/> in a row of one texture, each grey — its lightness lifted
    /// to at least <see cref="WreckLift"/>, so the wreck is soft, not drawn in its outlines — with mipmaps,
    /// and where each lies, its clear border included.
    /// </summary>
    private static (Texture2D, Rect2[]) Atlas(List<string> paths)
    {
        var images = paths.Select(p =>
        {
            var image = GD.Load<Texture2D>(p).GetImage();
            image.Convert(Image.Format.Rgba8);
            return image;
        }).ToList();
        int width = images.Sum(i => i.GetWidth() + Pad * 2), height = images.Max(i => i.GetHeight()) + Pad * 2;
        var data = new byte[width * height * 4];
        // Clear texels take the lifted grey, so the mipmaps blend the edge toward it rather than black.
        byte lift = (byte)(WreckLift * 255);
        for (int i = 0; i < data.Length; i += 4) data[i] = data[i + 1] = data[i + 2] = lift;
        var regions = new Rect2[images.Count];
        int x = 0;
        for (int n = 0; n < images.Count; n++)
        {
            var image = images[n];
            int w = image.GetWidth(), h = image.GetHeight();
            var src = image.GetData();
            for (int y = 0; y < h; y++)
                for (int sx = 0; sx < w; sx++)
                {
                    int s = (y * w + sx) * 4, d = ((y + Pad) * width + x + Pad + sx) * 4;
                    float luma = (src[s] * 0.3f + src[s + 1] * 0.59f + src[s + 2] * 0.11f) / 255;
                    byte grey = (byte)(255 * (WreckLift + (1 - WreckLift) * luma));
                    data[d] = data[d + 1] = data[d + 2] = grey;
                    data[d + 3] = src[s + 3];
                }
            regions[n] = new Rect2(x, 0, w + Pad * 2, h + Pad * 2);
            x += w + Pad * 2;
        }
        var atlas = Image.CreateFromData(width, height, false, Image.Format.Rgba8, data);
        atlas.GenerateMipmaps();
        return (ImageTexture.CreateFromImage(atlas), regions);
    }
}
