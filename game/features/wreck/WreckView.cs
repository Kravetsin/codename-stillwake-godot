using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>
/// A wreck's looks (vessel-wrecks W-2, <see cref="WreckLooks"/>): painted once as the surface opens, a picture a chunk and layer, each
/// drawn only in view, lit as the hull is. A chunk whose cells changed (a wall cut, a machine taken) is painted again from the live grid,
/// one at most every <see cref="RepaintMs"/> (a chunk costs a few ms): its frost goes with the wall it lay on, a burnt machine's char with
/// the machine.
/// </summary>
public partial class WreckView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const double RepaintMs = 100;

    private readonly IGridQueries _grid;
    private readonly WreckScene _scene;
    private readonly List<WreckMark> _marks;
    private readonly WreckPaint _paint = new();
    private readonly Dictionary<(WreckLayer, int, int), Sprite2D> _sprites = new();
    private readonly List<(int X, int Y)> _dirty = [];
    private readonly HashSet<(int, int)> _queued = [];
    private int _version;
    private double _sinceMs = RepaintMs;

    /// <summary>The looks of <paramref name="built"/>'s wreck on <paramref name="grid"/>; none for an intact vessel.</summary>
    public static WreckView? Of(IGridQueries grid, Built? built) => built?.Wreck == null ? null : new WreckView(grid, built);

    private WreckView(IGridQueries grid, Built built)
    {
        _grid = grid;
        _scene = new WreckScene(built);
        var livery = Looks.LiveriesOf(built.Vessel.Class)[Looks.LiveryOf(built.Vessel.Class, built.Vessel.Seed)];
        uint hull = built.Wreck!.Scavenged ? Looks.Faded(livery).Hull : livery.Hull;
        var pictures = new Dictionary<string, (WreckPicture, int, int)?>();
        (WreckPicture Picture, int Reach, int Rise)? Picture(string kind)
        {
            if (pictures.TryGetValue(kind, out var known)) return known;
            var image = HiRes.Load($"machine_{kind}");
            if (image == null) return pictures[kind] = null;
            image = (Image)image.Duplicate();
            image.Convert(Image.Format.Rgba8);
            return pictures[kind] = (new WreckPicture(image.GetWidth(), image.GetHeight(), image.GetData()), ShipTiles.ReachOf(kind), ShipTiles.RiseOf(kind));
        }
        _marks = WreckLooks.Of(_scene, hull, Picture);
        var chunks = new SortedSet<(int, int)>();
        foreach (var m in _marks)
        {
            var (x0, y0) = WreckPaint.ChunkOf((int)Math.Floor(m.X0), (int)Math.Floor(m.Y0));
            var (x1, y1) = WreckPaint.ChunkOf((int)Math.Ceiling(m.X1), (int)Math.Ceiling(m.Y1));
            for (int cy = y0; cy <= y1; cy++)
                for (int cx = x0; cx <= x1; cx++) chunks.Add((cx, cy));
        }
        foreach (var (cx, cy) in chunks) Paint(cx, cy);
        _version = grid.Version;
    }

    public override void _Process(double delta)
    {
        if (_grid.Version != _version) Follow();
        _sinceMs += delta * 1000;
        if (_dirty.Count > 0 && _sinceMs >= RepaintMs)
        {
            _sinceMs = 0;
            var (cx, cy) = _dirty[^1];
            _dirty.RemoveAt(_dirty.Count - 1);
            _queued.Remove((cx, cy));
            Paint(cx, cy);
        }
    }

    /// <summary>The cells the grid changed since last read into the scene, the chunks they lie in queued to be painted again.</summary>
    private void Follow()
    {
        var changes = _grid.ChangedSince(_version);
        _version = _grid.Version;
        int m = Content.Vessels.Margin, width = _grid.Width;
        // A log run out (rare): every cell of the frame read again.
        IEnumerable<int> cells = changes?.Select(c => c.Cell).Distinct()
            ?? Enumerable.Range(-WreckScene.Pad, _scene.Height + 2 * WreckScene.Pad).SelectMany(y =>
                Enumerable.Range(-WreckScene.Pad, _scene.Width + 2 * WreckScene.Pad).Select(x => (m + y) * width + m + x));
        bool changed = false;
        foreach (int cell in cells)
        {
            int x = cell % width - m, y = cell / width - m;
            if (!_scene.In(x, y)) continue;
            var wall = _grid.WallAt(cell);
            _scene.Set(x, y, wall != null ? wall.Id == Content.Vessels.Glass ? WreckScene.Glass : WreckScene.Wall
                : _grid.BackAt(cell) != null ? WreckScene.Air : WreckScene.Space);
            Queue(x, y);
            changed = true;
        }
        if (!changed) return;
        foreach (var thing in _scene.Machines)
        {
            if (!thing.Stands) continue;
            int at = (m + thing.Y) * width + m + thing.X;
            if (_grid.MachineAt(at) is { } now && now.Kind == thing.Kind && now.Cell == at) continue;
            thing.Stands = false;
            foreach (var (x, y) in (ReadOnlySpan<(int, int)>)[(thing.X - 2, thing.Y - 2), (thing.X + thing.W + 1, thing.Y - 2), (thing.X - 2, thing.Y + thing.H + 1),
                (thing.X + thing.W + 1, thing.Y + thing.H + 1)]) Queue(x, y);
        }
    }

    private void Queue(int x, int y)
    {
        var chunk = WreckPaint.ChunkOf(x, y);
        if (_queued.Add(chunk)) _dirty.Add(chunk);
    }

    /// <summary>Chunk (<paramref name="cx"/>, <paramref name="cy"/>) painted, each layer, its picture made, replaced or dropped.</summary>
    private void Paint(int cx, int cy)
    {
        foreach (var layer in (ReadOnlySpan<WreckLayer>)[WreckLayer.Rooms, WreckLayer.Over])
        {
            _paint.Begin(cx, cy);
            var (x0, y0, x1, y1) = _paint.Cells;
            foreach (var mark in _marks)
                if (mark.Layer == layer && mark.Meets(x0, y0, x1, y1)) mark.Draw(_paint);
            var key = (layer, cx, cy);
            if (!_paint.Painted)
            {
                if (_sprites.Remove(key, out var gone)) gone.QueueFree();
                continue;
            }
            var texture = ImageTexture.CreateFromImage(Image.CreateFromData(WreckPaint.Side, WreckPaint.Side, false, Image.Format.Rgba8, _paint.Bytes()));
            if (_sprites.TryGetValue(key, out var sprite))
            {
                sprite.Texture = texture;
                continue;
            }
            int m = Content.Vessels.Margin;
            sprite = new Sprite2D
            {
                Texture = texture,
                Centered = false,
                Position = new Vector2(cx * WreckPaint.ChunkCells + m, cy * WreckPaint.ChunkCells + m) * Cell,
                Scale = Vector2.One * Cell / WreckPaint.Texels,
                ZIndex = layer == WreckLayer.Rooms ? Layers.WreckRooms : Layers.WreckOver,
                TextureFilter = TextureFilterEnum.Linear,
            };
            Lit.Set(sprite);
            AddChild(sprite);
            _sprites[key] = sprite;
        }
    }
}
