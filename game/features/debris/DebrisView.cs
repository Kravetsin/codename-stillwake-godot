using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The debris drifting by (features/debris): each piece its item's picture, drawn between ticks so it
/// glides, tumbling slowly, faded in over the start of its path and out at its end; behind the hull,
/// since it never collides with it (0006). Out in space, E or the left button on a piece within reach
/// catches it, outlined green while it would and red while the sim would refuse it; a piece this peer
/// is grabbing is gone at once (0014).
/// </summary>
public partial class DebrisView : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = DebrisModule.Unit;
    /// <summary>A piece's size, in cells.</summary>
    private const float Size = 0.8f;
    private static readonly Color AimReady = new("7fe08a"), AimRefused = new("e05555");

    private readonly Dictionary<int, (Node2D View, Sprite2D Rim)> _shown = new();
    private double _clockMs;
    private bool _use;
    private Vector2 _mouse = new(float.NaN, float.NaN);

    /// <summary>The piece aimed at last frame, if any: for scripted runs.</summary>
    public Catch? Aimed { get; private set; }

    /// <summary>The cell the hero's body was in last frame, if it is placed, and the pieces read then: for scripted runs.</summary>
    public int? HeroCell { get; private set; }
    public IReadOnlyList<Piece> Pieces { get; private set; } = [];

    public override void _Ready()
    {
        // Behind the back walls: debris drifts past behind the hull.
        ZIndex = -20;
    }

    /// <summary>
    /// Shows the client's world through the current registry's queries, a resync bringing new ones.
    /// <paramref name="hero"/> is the hero's body cell and what its hands reach, none until it is placed;
    /// <paramref name="busy"/> says whether E works something else under the pointer, which comes first.
    /// <paramref name="progress"/> is how far the world is between its last tick and the next.
    /// </summary>
    public void Frame(
        Client client, IRoomsQueries rooms, DebrisModule debris, int width, (int Cell, Func<int, bool> Reaches)? hero,
        Func<(double X, double Y), bool> busy, double progress, double elapsedMs)
    {
        _clockMs += elapsedMs;
        (HeroCell, Pieces) = (hero?.Cell, debris.Pieces);
        var grabbed = Catching.Grabbing(client.InFlight);
        var pieces = debris.Pieces.Where(p => !grabbed.Contains(p.Id)).ToList();
        var ids = pieces.Select(p => p.Id).ToHashSet();
        foreach (int id in _shown.Keys.Where(id => !ids.Contains(id)).ToList())
        {
            _shown[id].View.QueueFree();
            _shown.Remove(id);
        }

        Aimed = hero is var (cell, reaches) && Pointer() is { } p && !busy(p)
            ? Catching.AimAt(debris, rooms, width, client.Id, cell, reaches, p, grabbed)
            : null;
        if (Catching.GrabOf(Aimed, _use) is { } grab) client.Send(grab);
        _use = false;
        // Outlined only while E would do something: within reach.
        var outlined = Aimed is { InReach: true } a ? a : null;

        foreach (var piece in pieces)
        {
            if (!_shown.TryGetValue(piece.Id, out var one))
            {
                one = PictureOf(piece.Item);
                _shown[piece.Id] = one;
                AddChild(one.View);
            }
            double x = piece.X - piece.Speed * progress;
            one.View.Position = new Vector2((float)(x / Unit * Cell), (float)((double)piece.Y / Unit * Cell));
            one.View.Modulate = new Color(1, 1, 1, (float)Catching.FadeOf(x, piece.Start, piece.End));
            // A slow tumble, each piece its own way.
            one.View.Rotation = (float)(_clockMs / 4000 * (piece.Id % 2 == 0 ? 1 : -1) + piece.Id);
            one.Rim.Visible = outlined?.Piece == piece.Id;
            if (one.Rim.Visible) one.Rim.Modulate = outlined!.Problem == null ? AimReady : AimRefused;
        }
    }

    /// <summary>An item's picture the size of a piece, centred, and its outline behind it, hidden until aimed at.</summary>
    private static (Node2D View, Sprite2D Rim) PictureOf(string item)
    {
        var rim = new Sprite2D
        {
            Texture = ItemIcons.Silhouette(item),
            Scale = Vector2.One * ((Size * Cell + 4) / ItemIcons.Pixels),
            Visible = false,
            // Unlit: the outline says what E does, whatever light falls there.
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        };
        var body = new Sprite2D { Texture = ItemIcons.Icon(item), Scale = Vector2.One * (Size * Cell / ItemIcons.Pixels) };
        Lit.Set(body);
        var view = new Node2D();
        view.AddChild(rim);
        view.AddChild(body);
        return (view, rim);
    }

    /// <summary>The world point under the pointer, in cells; none before the pointer has moved over the window.</summary>
    private (double X, double Y)? Pointer()
    {
        if (float.IsNaN(_mouse.X)) return null;
        var at = GetViewport().GetCanvasTransform().AffineInverse() * _mouse / Cell;
        return (at.X, at.Y);
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // The left button catches too: out in space, with the keys held and the mouse aiming, E is a stretch.
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.E }
            or InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            _use = true;
    }
}
