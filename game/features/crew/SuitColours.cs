using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours, a row in the game menu's settings (SuitColours.tsx's buttons): the one worn ringed,
/// those other heroes wear dimmed, the rest a click away, which sends <c>crew</c>'s <c>PickColour</c>.
/// Reads the sim four times a second while shown, never every frame.
/// </summary>
public partial class SuitColours : Control
{
    private const double RefreshS = 0.25;
    private static readonly Color Ring = new("f1f5f9");
    /// <summary>A button's size and the gap between, in pixels (h-3.5 gap-2, a size up in a menu); a hovered one grows by a quarter.</summary>
    private const float Dot = 20, Gap = 12, Grown = 1.25f;

    private readonly Func<(int? Mine, IReadOnlySet<int> Taken)> _read;
    private readonly Action<int> _pick;
    private double _sinceRead = RefreshS;
    private int? _mine, _hovered;
    private HashSet<int> _taken = [];

    /// <param name="read">The colour this player wears, if any yet, and those other heroes wear, by index into <see cref="Suits.All"/>.</param>
    /// <param name="pick">Sends the pick of a free colour.</param>
    public SuitColours(Func<(int? Mine, IReadOnlySet<int> Taken)> read, Action<int> pick)
    {
        _read = read;
        _pick = pick;
        // Room for a hovered button grown at either end.
        CustomMinimumSize = new Vector2(Suits.All.Length * Dot + (Suits.All.Length - 1) * Gap + Dot * (Grown - 1), Dot * Grown);
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>The middle of colour <paramref name="i"/>'s button on screen, for scripted runs.</summary>
    public Vector2 CenterOf(int i) => GetGlobalTransformWithCanvas() * CentreOf(i);

    private bool Free(int i) => i != _mine && !_taken.Contains(i);

    private Vector2 CentreOf(int i) => new(Dot * (Grown - 1) / 2 + i * (Dot + Gap) + Dot / 2, Size.Y / 2);

    private int? At(Vector2 point)
    {
        for (int i = 0; i < Suits.All.Length; i++)
            if (point.DistanceTo(CentreOf(i)) <= Dot / 2) return i;
        return null;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        var (mine, taken) = _read();
        if (_mine == mine && _taken.SetEquals(taken)) return;
        _mine = mine;
        _taken = taken.ToHashSet();
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        // Shown again, it reads at once rather than show a colour another hero took meanwhile.
        if (what == NotificationVisibilityChanged) _sinceRead = RefreshS;
        if (what != NotificationMouseExit || _hovered == null) return;
        _hovered = null;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            int? over = At(motion.Position);
            if (over != _hovered)
            {
                _hovered = over;
                MouseDefaultCursorShape = over is int o && Free(o) ? CursorShape.PointingHand : CursorShape.Arrow;
                QueueRedraw();
            }
        }
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click && At(click.Position) is int i && Free(i))
        {
            _pick(i);
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        for (int i = 0; i < Suits.All.Length; i++)
        {
            bool worn = i == _mine, free = Free(i);
            int rgb = Suits.All[i];
            var colour = new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, worn || free ? 1 : 0.25f);
            // hover:scale-125 on a free one.
            float radius = Dot / 2 * (free && i == _hovered ? Grown : 1);
            DrawCircle(CentreOf(i), radius, colour, true, -1, true);
            if (worn) DrawArc(CentreOf(i), radius + 1, 0, Mathf.Tau, 24, Ring, 2, true);
        }
    }
}
