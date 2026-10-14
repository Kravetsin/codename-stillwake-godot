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
/// changes, a texel a cell, and handed to <see cref="Lit"/>, whose material reads it smoothed between
/// cells wherever this light, a directional one with no shadows, falls: the sweeps have already
/// stopped it at walls.
/// </summary>
public partial class Starlight : DirectionalLight2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>Cells of open space worked out round the grid, so its edge is lit as the space beyond it.</summary>
    private const int Margin = 4;
    private const byte Air = 0, Machine = 1, Wall = 2;

    public Starlight() => RangeItemCullMask = Lit.StarMask;

    /// <summary>Works the starlight out anew for the grid as it stands.</summary>
    public void Show(IGridQueries grid)
    {
        int w = grid.Width + Margin * 2, h = grid.Height + Margin * 2;
        var kind = new byte[w * h];
        var light = new float[w * h * 3];
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
                if (kind[i] != Wall && (!inside || back[cell] == Back.None))
                    (light[i * 3], light[i * 3 + 1], light[i * 3 + 2]) = L.Star;
            }
        Spread(light, kind, w, h);
        var bytes = new byte[w * h * 3];
        for (int i = 0; i < w * h; i++)
        {
            // Walls are drawn taking WallIn of the light on their face, so a wall cell holds its face's light.
            float face = kind[i] == Wall ? 1 / L.WallIn : 1;
            for (int c = 0; c < 3; c++) bytes[i * 3 + c] = (byte)(Math.Min(1, light[i * 3 + c] * face) * 255);
        }
        var texture = ImageTexture.CreateFromImage(Image.CreateFromData(w, h, false, Image.Format.Rgb8, bytes));
        Lit.ShowStarlight(texture, new Rect2(-Margin * Cell, -Margin * Cell, w * Cell, h * Cell));
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

    /// <summary>Terraria's sweeps (lightOf in cells.ts): each cell takes its neighbour's light times what crossing keeps, the brightest way winning.</summary>
    private static void Spread(float[] light, byte[] kind, int w, int h)
    {
        void Pass(int a, int b)
        {
            float share = Shares[kind[a] * 3 + kind[b]];
            for (int c = 0; c < 3; c++)
            {
                float v = light[a * 3 + c] * share;
                if (v > light[b * 3 + c]) light[b * 3 + c] = v;
            }
        }
        for (int round = 0; round < L.Rounds; round++)
        {
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = row + 1; x < row + w; x++) Pass(x - 1, x);
                for (int x = row + w - 2; x >= row; x--) Pass(x + 1, x);
            }
            for (int x = 0; x < w; x++)
            {
                for (int at = x + w; at < w * h; at += w) Pass(at - w, at);
                for (int at = x + (h - 2) * w; at >= 0; at -= w) Pass(at + w, at);
            }
        }
    }
}
