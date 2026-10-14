using System;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Starlight as the TypeScript game worked it out (0019), cell by cell: the stars shine in every open
/// cell with no back wall and everywhere past the grid's edge, and spread from there in sweeps along
/// every row and column, fading by what they cross. Worked out over the whole grid only when the ship
/// changes, a texel a cell marked wall or open, and handed to <see cref="Lit"/>, whose material reads it
/// smoothed between the cells of a picture's own side wherever this light, a directional one with no
/// shadows, falls: the sweeps have already stopped it at walls. Its buffers and texture are kept and
/// filled anew, so a door toggled allocates nothing and costs only the sweeps.
/// </summary>
public partial class Starlight : DirectionalLight2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>Cells of open space worked out round the grid, so its edge is lit as the space beyond it.</summary>
    private const int Margin = 4;
    private const byte Air = 0, Machine = 1, Wall = 2;

    private byte[] _kind = [], _bytes = [];
    /// <summary>Each cell's share of the stars' light: the stars are the one colour it comes in.</summary>
    private float[] _light = [];
    private Image? _image;
    private ImageTexture? _texture;

    public Starlight() => RangeItemCullMask = Lit.StarMask;

    /// <summary>Works the starlight out anew for the grid as it stands.</summary>
    public void Show(IGridQueries grid)
    {
        int w = grid.Width + Margin * 2, h = grid.Height + Margin * 2;
        if (_kind.Length != w * h) (_kind, _light, _bytes) = (new byte[w * h], new float[w * h], new byte[w * h * 4]);
        var kind = _kind;
        var light = _light;
        Array.Clear(kind);
        Array.Clear(light);
        ReadOnlySpan<int> structure = grid.StructureLayer, back = grid.BackLayer;
        // A machine dims the light through every cell of its footprint.
        foreach (var m in grid.Machines)
        {
            var spec = Machines.Get(m.Kind);
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++)
                    kind[(m.Cell / grid.Width + dy + Margin) * w + m.Cell % grid.Width + dx + Margin] = Machine;
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x, gx = x - Margin, gy = y - Margin;
                bool inside = gx >= 0 && gy >= 0 && gx < grid.Width && gy < grid.Height;
                int cell = gy * grid.Width + gx;
                if (inside && structure[cell] is Structure.Wall or Structure.DoorClosed) kind[i] = Wall;
                if (kind[i] != Wall && (!inside || back[cell] == Back.None)) light[i] = 1;
            }
        Spread(light, kind, w, h);
        var bytes = _bytes;
        var (r, g, b) = L.Star;
        for (int i = 0; i < w * h; i++)
        {
            // Walls are drawn taking WallIn of the light on their face, so a wall cell holds its face's light.
            float face = kind[i] == Wall ? light[i] / L.WallIn : light[i];
            bytes[i * 4] = (byte)(Math.Min(1, r * face) * 255);
            bytes[i * 4 + 1] = (byte)(Math.Min(1, g * face) * 255);
            bytes[i * 4 + 2] = (byte)(Math.Min(1, b * face) * 255);
            // Its alpha says whether it is a wall's, so the shader blends it only with its own side.
            bytes[i * 4 + 3] = kind[i] == Wall ? (byte)255 : (byte)0;
        }
        if (_image != null && _texture != null && _image.GetWidth() == w && _image.GetHeight() == h)
        {
            _image.SetData(w, h, false, Image.Format.Rgba8, bytes);
            _texture.Update(_image);
            return;
        }
        _image = Image.CreateFromData(w, h, false, Image.Format.Rgba8, bytes);
        _texture = ImageTexture.CreateFromImage(_image);
        Lit.ShowStarlight(_texture, new Rect2(-Margin * Cell, -Margin * Cell, w * Cell, h * Cell));
    }

    /// <summary>The share of light going on from a cell of one kind into the next, by <c>from × 3 + to</c>.</summary>
    private static readonly float[] Shares = SharesOf();

    private static float[] SharesOf()
    {
        float[] into = [L.Air, L.Machine];
        var shares = new float[9];
        for (int from = 0; from < 3; from++)
            for (int to = 0; to < 3; to++)
                shares[from * 3 + to] = to == Wall
                    ? from == Wall ? L.WallDeep : L.WallIn
                    : (from == Wall ? L.WallOut : 1) * into[to];
        return shares;
    }

    /// <summary>
    /// Terraria's sweeps (lightOf in cells.ts): each cell takes its neighbour's light times what crossing
    /// keeps, the brightest way winning. One channel: every share is the same for red, green and blue.
    /// </summary>
    private static void Spread(float[] light, byte[] kind, int w, int h)
    {
        var shares = Shares;
        int n = w * h;
        for (int round = 0; round < L.Rounds; round++)
        {
            for (int row = 0; row < n; row += w)
            {
                for (int to = row + 1; to < row + w; to++)
                {
                    float v = light[to - 1];
                    if (v == 0) continue;
                    v *= shares[kind[to - 1] * 3 + kind[to]];
                    if (v > light[to]) light[to] = v;
                }
                for (int to = row + w - 2; to >= row; to--)
                {
                    float v = light[to + 1];
                    if (v == 0) continue;
                    v *= shares[kind[to + 1] * 3 + kind[to]];
                    if (v > light[to]) light[to] = v;
                }
            }
            // Every column down, then up, a row at a time: the same order within each column, in memory order.
            for (int to = w; to < n; to++)
            {
                float v = light[to - w];
                if (v == 0) continue;
                v *= shares[kind[to - w] * 3 + kind[to]];
                if (v > light[to]) light[to] = v;
            }
            for (int to = n - w - 1; to >= 0; to--)
            {
                float v = light[to + w];
                if (v == 0) continue;
                v *= shares[kind[to + w] * 3 + kind[to]];
                if (v > light[to]) light[to] = v;
            }
        }
    }
}
