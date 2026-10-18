using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Logistics;

/// <summary>What the badge reads: the logistics module and this peer's commands in flight.</summary>
public sealed record DroneWorld(LogisticsModule Logistics, IReadOnlyList<Command> InFlight);

/// <summary>
/// A machine's drone badge, at the right of its frame's header (0062, the "drones keep it going" mockup): a drone on
/// sky while drones serve it and a bay reaches it, dim and crossed while switched off, grey where no bay reaches. A
/// click switches it, sending <see cref="SetServed"/>. Why it looks so is in the one tooltip. Read on a timer, and at
/// once after its own command.
/// </summary>
public partial class ServedBadge : Control
{
    private const double RefreshMs = 100;
    private static readonly Vector2 Side = new(22, 18);
    private static readonly StyleBoxFlat Lit = Style.Box(Style.Sky400 with { A = 0.22f }, Style.Sky400 with { A = 0.6f }, radius: 4);
    private static readonly StyleBoxFlat Grey = Style.Box(Style.Slate800, Style.Slate600, radius: 4);
    private static readonly StyleBoxFlat Off = Style.Box(Colors.Transparent, Style.Slate700, radius: 4);
    private static readonly Texture2D Drone = ItemIcons.Icon(Content.Logistics.Drone);

    private readonly int _machine;
    private readonly Func<DroneWorld?> _world;
    private readonly Action<Command> _send;
    private readonly TipSpots _tips;
    private BadgeLook _look = BadgeLook.Lit;
    private bool _served = true, _hovered;
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    public ServedBadge(int machine, Func<DroneWorld?> world, Action<Command> send, Store<Tip?> tip)
    {
        (_machine, _world, _send) = (machine, world, send);
        _tips = new TipSpots(tip);
        CustomMinimumSize = Side;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Stop;
        GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            AcceptEvent();
            _send(new SetServed(_machine, !_served));
            Read();
        };
        _tips.Add(this, () =>
        {
            var (name, click) = Served.TipOf(_look);
            var lines = new List<TipLine>();
            if (_look == BadgeLook.Off && !Reached) lines.Add(new TipLine(T("logistics.unreached"), Tone.Dim));
            lines.Add(new TipLine(T(click), Tone.Dim));
            return new Tip(T(name), lines);
        });
        Read();
    }

    private bool Reached => _world() is { } world && world.Logistics.Reached(_machine);

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
        bool hovered = GetGlobalRect().HasPoint(_mouse);
        if (hovered != _hovered)
        {
            _hovered = hovered;
            QueueRedraw();
        }
        _tips.Frame(IsVisibleInTree() ? _mouse : null);
    }

    public override void _ExitTree() => _tips.Show(null);

    private void Read()
    {
        _sinceRead = 0;
        if (_world() is not { } world) return;
        _served = Served.Shown(world.Logistics.Served(_machine), _machine, world.InFlight);
        var look = Served.LookOf(_served, world.Logistics.Reached(_machine));
        if (look == _look) return;
        _look = look;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var box = new Rect2(Vector2.Zero, Size);
        DrawStyleBox(_look switch { BadgeLook.Lit => Lit, BadgeLook.Unreached => Grey, _ => Off }, box);
        var icon = new Rect2((Size - new Vector2(16, 16)) / 2, new Vector2(16, 16));
        var tint = _look switch { BadgeLook.Lit => Colors.White, BadgeLook.Unreached => new Color(0.55f, 0.58f, 0.62f), _ => new Color(1, 1, 1, 0.4f) };
        if (_hovered) tint = tint.Lightened(0.15f);
        DrawTextureRect(Drone, icon, false, tint);
        if (_look != BadgeLook.Off) return;
        // Crossed: a slash corner to corner over the dimmed drone.
        DrawLine(new Vector2(4, Size.Y - 4), new Vector2(Size.X - 4, 4), Style.Slate300 with { A = 0.85f }, 1.5f);
    }
}
