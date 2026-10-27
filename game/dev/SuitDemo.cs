using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --suit</c>: the helmet and the body worn and taken off (2026-10-27-suits-are-worn), for recording. The hero is
/// printed bare at the pod, the gauge at its lungs' 20; with the inventory open it makes the free helmet and body at the pod
/// and puts them on, the gauge growing to the helmet's 45 and filling from the room; takes them off again, the gauge back to
/// 20; then walks through the door into the torn room open to space, bare: the frost at once, the gauge emptying two a breath.
/// </summary>
public partial class SuitDemo : Control
{
    private readonly ItemsView _view;
    private readonly Func<(Client Client, Registry M)?> _world;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;
    private (Key Key, Func<int, bool> Done)? _walking;

    /// <summary>The world with creative mode off, so the suit is crafted, worn and breathed as in a game.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["items"]!["creative"] = false;
        return World.Load(root.ToJsonString(), modules());
    }

    public SuitDemo(ItemsView view, Func<(Client Client, Registry M)?> world)
    {
        (_view, _world) = (view, world);
        MouseFilter = MouseFilterEnum.Ignore;
        int head = Spec.Inventory.Slots, body = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot);
        At(1.0, () => Press(Key.Tab));
        At(1.4, () => _target = _view.Panel.CenterOf(new Place(head)));
        At(1.6, () => Send((m, pod) => new Craft(pod, Spec.Helmet.Basic, 1)));
        At(1.7, () => Send((m, pod) => new Craft(pod, Spec.Suit.Basic, 1)));
        // Five seconds each at the pod's fifth of speed; each out of its slot onto the hero.
        At(7.2, () => Send((m, pod) => TakeFrom(m, pod, Spec.Helmet.Basic, head)));
        At(12.4, () => _target = _view.Panel.CenterOf(new Place(body)));
        At(12.6, () => Send((m, pod) => TakeFrom(m, pod, Spec.Suit.Basic, body)));
        // Filled from the room, three units a breath; then both off into the bag.
        At(18.0, () => Send((m, _) => new Move(new Place(head), new Target(m.Items.Inventory(Id(m)).ToList().IndexOf(null)), 1)));
        At(19.0, () => Send((m, _) => new Move(new Place(body), new Target(m.Items.Inventory(Id(m)).ToList().IndexOf(null)), 1)));
        At(20.5, () => Press(Key.Tab));
        // To the door of the torn room, left of the hatch; opened; out into it, open to space.
        At(21.0, () => Walk(Key.A, x => x <= Hatch().X - 5));
        At(22.4, () => Send((m, _) => new SetDoor(DoorOf(m), true)));
        At(23.0, () => Walk(Key.A, x => x <= Hatch().X - 15));
    }

    private static Move TakeFrom(Registry m, int pod, string item, int slot) =>
        new(new Place(m.Items.Crate(pod).ToList().FindIndex(s => s?.Item == item), pod), new Target(slot), 1);

    private int Id(Registry m) => _world() is var (client, _) ? client.Id : 0;

    private void Send(Func<Registry, int, Command> command)
    {
        if (_world() is not var (client, m)) return;
        int pod = m.Grid.Machines.First(x => x.Kind == "lifepod").Id;
        client.Send(command(m, pod));
    }

    /// <summary>The doorway nearest left of the hatch, within a hero's height of its row: the torn room's door.</summary>
    private int DoorOf(Registry m)
    {
        var (hx, hy) = Hatch();
        for (int x = hx - 1; x > hx - 16; x--)
            for (int y = hy; y > hy - 6; y--)
                if (Doorways.IsDoorway(m.Grid.StructureAt(y * m.Grid.Width + x))) return y * m.Grid.Width + x;
        return -1;
    }

    private (int X, int Y) Hatch()
    {
        if (_world() is not var (_, m)) return (0, 0);
        return (m.Crew.Spawn % m.Grid.Width, m.Crew.Spawn / m.Grid.Width);
    }

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Walk(Key key, Func<int, bool> done)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _walking = (key, done);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_walking is var (key, done) && _view.Hero is { } hero && done(hero.X / 1000))
        {
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
            _walking = null;
        }
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(3000 * delta));
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
        DrawColoredPolygon(arrow, Colors.White);
        DrawPolyline([.. arrow, _pos], Colors.Black, 1);
    }

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }
}
