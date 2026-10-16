using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The debris drifting by (features/debris): each piece its item's picture, drawn between ticks so it
/// glides, tumbling slowly, faded in over the start of its path and out at its end; behind the hull,
/// since it never collides with it (0006). The piece the crew's E would catch is outlined, green while
/// it would and red while the sim would refuse it (<see cref="Ui.AimedPiece"/>); a piece this peer is
/// grabbing is gone at once (0014).
/// </summary>
public partial class DebrisView(Ui ui) : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = DebrisModule.Unit;
    /// <summary>A piece drawn as its item, with no pictures of its own: its size, in cells.</summary>
    private const float ItemSize = 0.8f;
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");

    private readonly Dictionary<int, Sprite2D> _shown = new();
    /// <summary>The piece aimed at, outlined over the rest.</summary>
    private readonly Outline _aimed = new(1);
    private double _clockMs;
    private readonly HashSet<int> _ids = [];
    private readonly List<int> _gone = [];

    /// <summary>The pieces read last frame: for scripted runs.</summary>
    public IReadOnlyList<Piece> Pieces { get; private set; } = [];

    public override void _Ready()
    {
        // Behind the back walls: debris drifts past behind the hull.
        ZIndex = Layers.Debris;
        AddChild(_aimed);
    }

    /// <summary>
    /// Shows the client's world through the current registry's debris, a resync bringing a new one.
    /// <paramref name="progress"/> is how far the world is between its last tick and the next.
    /// </summary>
    public void Frame(Client client, DebrisModule debris, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        Pieces = debris.Pieces;
        var grabbed = Predicted.Grabbing(client.InFlight);
        // Kept between frames, refilled: a frame allocates nothing here.
        _ids.Clear();
        var all = debris.Pieces;
        for (int i = 0; i < all.Count; i++)
            if (!grabbed.Contains(all[i].Id)) _ids.Add(all[i].Id);
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_ids.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _shown[id].QueueFree();
            _shown.Remove(id);
        }

        var aimed = ui.AimedPiece.Get();
        _aimed.Begin();
        for (int i = 0; i < all.Count; i++)
        {
            var piece = all[i];
            if (!_ids.Contains(piece.Id)) continue;
            if (!_shown.TryGetValue(piece.Id, out var one))
            {
                one = PictureOf(piece.Item, piece.Id);
                _shown[piece.Id] = one;
                AddChild(one);
            }
            double x = piece.X - piece.Speed * progress;
            one.Position = new Vector2((float)(x / Unit * Cell), (float)((double)piece.Y / Unit * Cell));
            one.Modulate = new Color(1, 1, 1, (float)Fading.FadeOf(x, piece.Start, piece.End));
            // A slow tumble, each piece its own way.
            one.Rotation = (float)(_clockMs / 4000 * (piece.Id % 2 == 0 ? 1 : -1) + piece.Id);
            if (aimed is { } a && a.Id == piece.Id)
                _aimed.Add(one.Texture, one.Position, one.Rotation, one.Scale.X, (a.Refused ? AimRefused : AimReady) with { A = one.Modulate.A });
        }
        _aimed.End();
    }

    /// <summary>The picture piece <paramref name="id"/> wears (<see cref="DebrisLooks"/>), a pixel of art a pixel of the world, or its item's the size of a piece; centred, lit.</summary>
    private static Sprite2D PictureOf(string item, int id)
    {
        var body = DebrisLooks.PictureOf(item, id) is string look
            ? new Sprite2D { Texture = GD.Load<Texture2D>(look) }
            : new Sprite2D { Texture = ItemIcons.Icon(item), Scale = Vector2.One * (ItemSize * Cell / ItemIcons.Pixels) };
        Lit.Set(body);
        return body;
    }
}
