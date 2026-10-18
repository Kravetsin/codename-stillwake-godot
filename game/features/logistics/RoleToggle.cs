using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// A crate's role, three icons in its frame's header (0062, the logistics mockup): a box for storage, an arrow out
/// for a provider in red, an arrow in for a requester in sky; the one it is pressed. A click makes it that. What each
/// means is in the one tooltip. Read on a timer, and at once after its own command.
/// </summary>
public partial class RoleToggle : HBoxContainer
{
    private const double RefreshMs = 100;
    /// <summary>A provider's red and a requester's sky: the crate's lamp in the world is the same.</summary>
    public static readonly Color Provider = new("f87171"), Requester = Style.Sky400;

    private static readonly StyleBoxFlat Up = Style.Box(Style.Slate900, Style.Slate600, radius: 3);
    private static readonly StyleBoxFlat Down = Style.Box(Style.Slate700, Style.Slate400, radius: 3);
    private static readonly StyleBoxFlat Over = Style.Box(Style.Slate800, Style.Slate400, radius: 3);

    private readonly int _crate;
    private readonly Func<RequestWorld?> _world;
    private readonly Action<Command> _send;
    private readonly TipSpots _tips;
    private readonly Icon[] _icons = new Icon[3];
    private Role _shown;
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    public RoleToggle(int crate, Func<RequestWorld?> world, Action<Command> send, Store<Tip?> tip)
    {
        (_crate, _world, _send) = (crate, world, send);
        _tips = new TipSpots(tip);
        Style.Gap(this, 0);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        foreach (var role in (Role[])[Role.Storage, Role.Provider, Role.Requester])
        {
            var icon = _icons[(int)role] = new Icon(role) { CustomMinimumSize = new Vector2(28, 22), MouseFilter = MouseFilterEnum.Stop };
            icon.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                icon.AcceptEvent();
                if (role == _shown) return;
                _send(new SetRole(_crate, role));
                Read();
            };
            string name = role.ToString().ToLowerInvariant();
            _tips.Add(icon, () => new Tip(T($"logistics.{name}"), new TipLine(T($"logistics.{name}.about"), Tone.Dim)));
            AddChild(icon);
        }
        Read();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
        foreach (var icon in _icons) icon.Hovered = icon.GetGlobalRect().HasPoint(_mouse);
        _tips.Frame(IsVisibleInTree() ? _mouse : null);
    }

    public override void _ExitTree() => _tips.Show(null);

    private void Read()
    {
        _sinceRead = 0;
        if (_world() is not { } world) return;
        _shown = Requests.RoleShown(world.Logistics.RoleOf(_crate), _crate, world.InFlight);
        foreach (var icon in _icons) icon.Pressed = icon.Role == _shown;
    }

    /// <summary>One role's button, its icon drawn as the mockup's 14 px glyphs, twice as large as a pixel.</summary>
    private sealed partial class Icon(Role role) : Control
    {
        public Role Role { get; } = role;
        private bool _pressed, _hovered;

        public bool Pressed
        {
            set
            {
                if (_pressed == value) return;
                _pressed = value;
                QueueRedraw();
            }
        }

        public bool Hovered
        {
            set
            {
                if (_hovered == value) return;
                _hovered = value;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            DrawStyleBox(_pressed ? Down : _hovered ? Over : Up, new Rect2(Vector2.Zero, Size));
            var o = (Size - new Vector2(14, 14)) / 2;
            var c = Role switch { Role.Provider => Provider, Role.Requester => Requester, _ => Style.Slate300 };
            if (!_pressed) c = c with { A = 0.7f };
            switch (Role)
            {
                case Role.Storage:
                    DrawRect(new Rect2(o + new Vector2(2, 4), new Vector2(10, 8)), c, false, 1.5f);
                    DrawLine(o + new Vector2(2, 6.5f), o + new Vector2(12, 6.5f), c, 1.5f);
                    break;
                case Role.Provider:
                    DrawRect(new Rect2(o + new Vector2(1.5f, 5), new Vector2(7, 7)), c, false, 1.5f);
                    DrawLine(o + new Vector2(7, 7), o + new Vector2(12, 2), c, 1.5f);
                    DrawPolyline([o + new Vector2(8.5f, 2), o + new Vector2(12, 2), o + new Vector2(12, 5.5f)], c, 1.5f);
                    break;
                default:
                    DrawRect(new Rect2(o + new Vector2(5.5f, 5), new Vector2(7, 7)), c, false, 1.5f);
                    DrawLine(o + new Vector2(7, 7), o + new Vector2(2, 2), c, 1.5f);
                    DrawPolyline([o + new Vector2(2, 5.5f), o + new Vector2(2, 2), o + new Vector2(5.5f, 2)], c, 1.5f);
                    break;
            }
        }
    }
}
