using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --breathe</c>: air coming back, for recording (0081). The hero stands in the wreck's torn room, open to space, its
/// suit low and the red edges showing, canisters on the hotbar. The script opens the inventory and breathes one with a
/// right click (the slot's pulse, the gauge's surge, the vortex, the danger letting go), closes it, opens the door to the
/// pod's room and walks in to the pod, breathing its air again (the wave, the puff at the visor), and breathes another
/// from the hand there, in the lit room. <c>--breathe-air</c>
/// starts lower and only walks in: the danger lets go by room air.
/// </summary>
public partial class BreatheDemo : Control
{
    /// <summary>Where the canisters lie: the hotbar's second slot.</summary>
    public const int CanisterSlot = 1;

    /// <summary>Where the hero starts: fifteen cells left of the pod's hatch, the torn room, as <see cref="DieDemo"/>.</summary>
    public static int StartOf(Registry m) => m.Crew.Spawn - 15;

    /// <summary>The world with peer 0's hero in on a low suit, canisters on its hotbar, creative mode off.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, int suit)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        items["creative"] = false;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == CanisterSlot ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = 3 }
                : i == Spec.Inventory.Total - 2 ? new JsonObject { ["item"] = Spec.Suit.Basic, ["count"] = 1 }
                : i == Spec.Inventory.Slots ? new JsonObject { ["item"] = Spec.Helmet.Basic, ["count"] = 1 } : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        root["slices"]!["crew"]!["heroes"] = new JsonArray(new JsonObject
        {
            ["id"] = 0,
            ["cell"] = (int)root["slices"]!["crew"]!["spawn"]! - 15,
            ["air"] = suit,
            ["out"] = 0,
            ["exerting"] = false,
            ["thrusting"] = false,
            ["thrustTicks"] = 0,
        });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly ItemsView _view;
    private readonly Func<Registry> _m;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private Func<Vector2>? _follow;
    private double _t;
    private int _next;
    private (Key Key, Func<int, bool> Done)? _walking;

    public BreatheDemo(ItemsView view, Func<Registry> m, bool canister)
    {
        (_view, _m) = (view, m);
        MouseFilter = MouseFilterEnum.Ignore;
        double t = 0.8;
        if (canister)
        {
            At(1.0, () => Press(Key.Tab));
            At(1.2, () => _target = _view.Panel.CenterOf(new Place(CanisterSlot)));
            At(2.0, () => Click(MouseButton.Right));
            At(2.05, () => _target = new Vector2(900, 560));
            At(4.0, () => Press(Key.Tab));
            t = 4.4;
        }
        // To the door of the pod's room, seven cells left of the hatch; opened with E; in to the pod, whose air it breathes.
        At(t, () => Walk(Key.D, x => x >= Hatch().X - 9));
        At(t + 1.6, () => Aim(Hatch().X - 7, Hatch().Y - 1));
        At(t + 1.9, () => Press(Key.E));
        At(t + 2.5, () => Walk(Key.D, x => x >= Hatch().X - 2));
        if (!canister) return;
        // In the lit room, another canister from the hand: the hotbar's slot chosen, a right click over the world.
        At(t + 5.0, () => Press(Key.Key2));
        At(t + 5.1, () => (_follow, _target) = (null, new Vector2(1000, 300)));
        At(t + 5.6, () => Click(MouseButton.Right));
    }

    private (int X, int Y) Hatch()
    {
        var m = _m();
        return (m.Crew.Spawn % m.Grid.Width, m.Crew.Spawn / m.Grid.Width);
    }

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Aim(int x, int y) =>
        _follow = () => GetViewport().GetCanvasTransform() * (new Vector2(x + 0.5f, y + 0.5f) * ShipCamera.Cell);

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
        if (_follow != null) _target = _follow();
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

    private void Click(MouseButton button)
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton
            {
                ButtonIndex = button,
                Pressed = pressed,
                // The world reads which buttons are down from the mask.
                ButtonMask = pressed ? (button == MouseButton.Right ? MouseButtonMask.Right : MouseButtonMask.Left) : 0,
                Position = _pos,
                GlobalPosition = _pos,
            });
    }

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }
}
