using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// Every debris net (docs/decisions/2026-11-03-debris-net.md): the grid draws its booms; this its plates on the hull's face, the pieces
/// it holds, each in its own picture, heaped in catch order, and the net in front of them, sagging by load (<see cref="NetShows"/>).
/// A catch pops in; the net is read <see cref="RefreshMs"/> apart.
/// </summary>
public partial class NetView : Node2D
{
    /// <summary>How often what the nets hold is read: a caught piece leaves the drift on its tick and shows in the net this soon after.</summary>
    private const double RefreshMs = 100;

    private sealed class Shown(Sprite2D mount, Sprite2D front)
    {
        public Sprite2D Mount { get; } = mount;
        public Sprite2D Front { get; } = front;
        public List<Sprite2D> Held { get; } = [];
        /// <summary>The pieces drawn, by id in catch order: drawn again only when they change.</summary>
        public int[] Ids { get; set; } = [];
    }

    private readonly IGridQueries _grid;
    private readonly DebrisModule _debris;
    private readonly Dictionary<int, Shown> _shown = new();
    private readonly HashSet<int> _seen = new();
    private readonly List<int> _gone = new();
    private double _sinceRead = RefreshMs;

    public NetView(IGridQueries grid, DebrisModule debris)
    {
        _grid = grid;
        _debris = debris;
    }

    public override void _Process(double delta)
    {
        if ((_sinceRead += delta * 1000) < RefreshMs) return;
        _sinceRead = 0;
        _seen.Clear();
        foreach (var net in _grid.MachinesWith(Stillwake.Content.MachineTrait.Catches)) Show(net);
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var shown = _shown[id];
            shown.Mount.QueueFree();
            shown.Front.QueueFree();
            foreach (var piece in shown.Held) piece.QueueFree();
            _shown.Remove(id);
        }
    }

    private void Show(Machine net)
    {
        _seen.Add(net.Id);
        var corner = new Vector2(net.Cell % _grid.Width, net.Cell / _grid.Width) * ShipCamera.Cell;
        if (!_shown.TryGetValue(net.Id, out var shown))
        {
            shown = new Shown(Made(Layers.NetHeld, PixelArt.Lit("net_mount")), Made(Layers.NetFront, PixelArt.Lit(NetShows.FrontOf(0))));
            shown.Mount.Position = corner - new Vector2(NetInk.Margin, 0);
            shown.Front.Position = corner - new Vector2(NetInk.Margin, NetInk.Margin);
            _shown[net.Id] = shown;
        }
        var held = _debris.HeldIn(net.Id);
        if (shown.Ids.Length == held.Count && held.Select(e => e.Id).SequenceEqual(shown.Ids)) return;
        shown.Ids = [.. held.Select(e => e.Id)];
        shown.Front.Texture = PixelArt.Lit(NetShows.FrontOf(held.Count));
        while (shown.Held.Count < held.Count) shown.Held.Add(Made(Layers.NetHeld, null, centred: true));
        for (int k = 0; k < shown.Held.Count; k++)
        {
            var piece = shown.Held[k];
            piece.Visible = k < held.Count;
            if (!piece.Visible) continue;
            var (x, y, turn) = NetShows.Place(held.Count, k);
            piece.Texture = DebrisView.PictureOf(held[k].Kind, held[k].Id);
            piece.Position = corner + new Vector2(x, y);
            piece.Rotation = turn;
        }
    }

    /// <summary>A lit picture of the net's at <paramref name="z"/>, its corner at its position unless <paramref name="centred"/>.</summary>
    private Sprite2D Made(int z, Texture2D? texture, bool centred = false)
    {
        var sprite = new Sprite2D { Texture = texture, Centered = centred, ZIndex = z };
        Lit.Set(sprite);
        AddChild(sprite);
        return sprite;
    }
}
