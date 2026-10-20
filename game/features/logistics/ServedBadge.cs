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
/// A machine's drone key, at the right of its frame's header (0062, 0081): a key of the wrist computer's metal whose face
/// is backlit sky with a drone on it while drones serve it and a bay reaches it, dim and crossed while switched off, grey
/// where no bay reaches. A press switches it, sending <see cref="SetServed"/>. Why it looks so is in the one tooltip.
/// Read on a timer, and at once after its own command.
/// </summary>
public partial class ServedBadge : WristKey
{
    private const double RefreshMs = 100;
    private static readonly Texture2D Drone = ItemIcons.Icon(Content.Logistics.Drone);
    private static readonly Color Sky = new("38bdf8"), Grey = new(0.55f, 0.58f, 0.62f), Slash = new Color("cbd5e1") with { A = 0.85f };

    private readonly int _machine;
    private readonly Func<DroneWorld?> _world;
    private readonly Action<Command> _send;
    private readonly TipSpots _tips;
    private BadgeLook _look = BadgeLook.Lit;
    private bool _served = true;
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    public ServedBadge(int machine, Func<DroneWorld?> world, Action<Command> send, Store<Tip?> tip)
        : base(new Vector2(WristLook.KeycapSide, WristLook.KeycapSide))
    {
        (_machine, _world, _send) = (machine, world, send);
        _tips = new TipSpots(tip);
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

    protected override void Pressed()
    {
        _send(new SetServed(_machine, !_served));
        Read();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
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

    protected override void DrawCap(Rect2 face)
    {
        bool lit = _look == BadgeLook.Lit, hover = State == KeyState.Hover;
        Face(face, lit);
        var icon = new Rect2(face.Position + new Vector2((face.Size.X - 16) / 2, (face.Size.Y - 16) / 2 + (State == KeyState.Down ? 1 : 0)), new Vector2(16, 16));
        // Backlit, the drone glows in the screen's blue round its outline.
        if (lit) DrawTextureRect(Drone, icon.Grow(1), false, Sky with { A = hover ? 0.6f : 0.35f });
        var tint = _look switch
        {
            BadgeLook.Lit => Colors.White,
            BadgeLook.Unreached => Grey with { A = hover ? 0.9f : 0.7f },
            _ => Colors.White with { A = hover ? 0.6f : 0.4f },
        };
        DrawTextureRect(Drone, icon, false, tint);
        if (_look != BadgeLook.Off) return;
        // Crossed: a slash corner to corner over the dimmed drone.
        DrawLine(new Vector2(face.Position.X + 1, face.End.Y - 1), new Vector2(face.End.X - 1, face.Position.Y + 1), Slash, 1.5f);
    }
}
