using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours under the oxygen bar, without a word (SuitColours.tsx): the one worn ringed, those
/// other heroes wear dimmed, the rest a click away, which sends <c>crew</c>'s <c>PickColour</c>. Reads the
/// sim four times a second, never every frame.
/// </summary>
public partial class SuitColours : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<(int? Mine, IReadOnlySet<int> Taken)> _read;
    private readonly Action<int> _pick;
    private readonly Row _row = new();
    private double _sinceRead = RefreshS;

    /// <param name="read">The colour this player wears, if any yet, and those other heroes wear, by index into <see cref="Suits.All"/>.</param>
    /// <param name="pick">Sends the pick of a free colour.</param>
    public SuitColours(Func<(int? Mine, IReadOnlySet<int> Taken)> read, Action<int> pick)
    {
        _read = read;
        _pick = pick;
        _row.Picked += i => _pick(i);
        AddChild(_row);
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        var (mine, taken) = _read();
        if (_row.Mine == mine && _row.Taken.SetEquals(taken)) return;
        _row.Mine = mine;
        _row.Taken = taken.ToHashSet();
        _row.QueueRedraw();
    }

    /// <summary>The row of colours, drawn by hand: Tailwind's slate box and round buttons.</summary>
    private sealed partial class Row : Control
    {
        private static readonly Color Back = new(new Color("0f172a"), 0.9f), Edge = new("334155"), Ring = new("f1f5f9");
        /// <summary>A button's size, the gap between and the box's padding, in pixels (h-3.5, gap-2, px-3 py-1.5).</summary>
        private const float Dot = 14, Gap = 8, PadX = 12, PadY = 6;

        public int? Mine { get; set; }
        public HashSet<int> Taken { get; set; } = [];
        public event Action<int>? Picked;
        private int? _hovered;

        public Row()
        {
            // Under the oxygen bar: left-3 top-[6.5rem].
            Position = new Vector2(12, 104);
            Size = new Vector2(2 * PadX + Suits.All.Length * Dot + (Suits.All.Length - 1) * Gap, 2 * PadY + Dot);
            MouseFilter = MouseFilterEnum.Stop;
        }

        private bool Free(int i) => i != Mine && !Taken.Contains(i);

        private Vector2 CentreOf(int i) => new(PadX + i * (Dot + Gap) + Dot / 2, PadY + Dot / 2);

        private int? At(Vector2 point)
        {
            for (int i = 0; i < Suits.All.Length; i++)
                if (point.DistanceTo(CentreOf(i)) <= Dot / 2) return i;
            return null;
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
                Picked?.Invoke(i);
                AcceptEvent();
            }
        }

        public override void _Notification(int what)
        {
            if (what != NotificationMouseExit || _hovered == null) return;
            _hovered = null;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var box = new Rect2(Vector2.Zero, Size);
            DrawRect(box, Back);
            DrawRect(box, Edge, false, 1);
            for (int i = 0; i < Suits.All.Length; i++)
            {
                bool worn = i == Mine, free = Free(i);
                int rgb = Suits.All[i];
                var colour = new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, worn || free ? 1 : 0.25f);
                // hover:scale-125 on a free one.
                float radius = Dot / 2 * (free && i == _hovered ? 1.25f : 1);
                DrawCircle(CentreOf(i), radius, colour, true, -1, true);
                if (worn) DrawArc(CentreOf(i), radius + 1, 0, Mathf.Tau, 24, Ring, 2, true);
            }
        }
    }
}
