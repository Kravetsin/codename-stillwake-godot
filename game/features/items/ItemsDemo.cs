using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// <c>-- --items</c>: a room with a crate, a furnace and a tech station, a solar panel, the hero
/// carrying a few stacks, lamps lit and wire lying on the floor; and a script
/// played through Godot's own input, for recordings with <c>--write-movie</c>: the hotbar chosen by the
/// wheel and keys, stacks picked, split and put down in the inventory, shifted to the bag and back to the
/// hotbar, dragged along the closed hotbar,
/// the wire walked over and picked up, the furnace, a stack thrown with Q,
/// the crate, its sort key and the bag's. It draws the pointer, which a movie does not.
/// </summary>
public partial class ItemsDemo : Control
{
    /// <summary>
    /// The demo's room, drawn a character a 16 px cell and doubled (0104): the hero wakes at S; the panel P out in space above
    /// the roof powers two lamps.
    /// </summary>
    public static readonly Layout Map = Layout.Parse(
        [
            "        PPP       ",
            "##################",
            "#...L........L...#",
            "#.........AA..FF.#",
            "#.KK...S..AA..FF.#",
            "##################",
        ],
        cables: [((8, 0), (4, 2)), ((10, 0), (13, 2))]).Doubled() with
    { Known = Spec.Research.StartKnown };

    /// <summary>The wire lying on the floor, the drawing's column and row: in its cells' bottom-left, doubled.</summary>
    private static readonly (int X, int Y) Lying = (12, 4);

    /// <summary>
    /// <paramref name="world"/> with peer 0 carrying a few stacks and wire
    /// lying on the floor: put into its save, as no command makes items out of nothing.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        JsonNode? Stack(string item, int count) => new JsonObject { ["item"] = item, ["count"] = count };
        var slots = new JsonArray();
        (string, int)?[] carried =
        [
            ("scrap", 120), ("iron", 64), ("planks", 30), ("scrapChunk", 6), ("hullPlate", 3), ("canister", 2), null, null, null,
            ("scrap", 500), null, ("planks", 50), null, ("iron", 500),
        ];
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i < carried.Length && carried[i] is var (item, count) ? Stack(item, count) : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        int width = Map.Width, x = 2 * Lying.X, y = 2 * Lying.Y + 1;
        items["floor"] = new JsonArray(new JsonObject
        {
            ["id"] = 1,
            ["item"] = "wire",
            ["count"] = 2,
            ["cell"] = y * width + x,
            ["x"] = x * 1000 + 500,
            ["y"] = (y + 1) * 1000,
            ["vx"] = 0,
            ["vy"] = 0,
            ["by"] = 99,
            ["at"] = 0,
        });
        items["nextFloorId"] = 2;
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly ItemsView _view;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private Func<Vector2>? _follow;
    private double _t;
    private int _next;
    private (Key Key, Func<HeroSpot, bool> Done)? _walking;

    public ItemsDemo(ItemsView view)
    {
        _view = view;
        MouseFilter = MouseFilterEnum.Ignore;
        static Place Inv(int slot) => new(slot);
        // The drawing's column: two cells.
        int Column(HeroSpot h) => h.X / 2000;
        // The hotbar: the wheel and a key choose a slot.
        At(0.3, () => Wheel(MouseButton.WheelDown));
        At(0.5, () => Wheel(MouseButton.WheelDown));
        At(0.8, () => Press(Key.Key4));
        // The inventory: a stack picked and put down, half of one picked, one of it put, the rest put.
        At(1.1, () => Press(Key.Tab));
        At(1.2, () => Go(Inv(9)));
        At(1.6, () => Click(MouseButton.Left));
        At(1.7, () => Go(Inv(16)));
        At(2.1, () => Click(MouseButton.Left));
        At(2.2, () => Go(Inv(13)));
        At(2.5, () => Click(MouseButton.Right));
        At(2.6, () => Go(Inv(21)));
        At(2.9, () => Click(MouseButton.Right));
        At(3.0, () => Go(Inv(22)));
        At(3.3, () => Click(MouseButton.Left));
        // With no machine open, shift sends a stack between the hotbar and the bag, flying there: down and back up.
        At(3.5, () => Go(Inv(4)));
        At(3.8, () => Click(MouseButton.Left, shift: true));
        At(4.3, () => Go(Inv(9)));
        At(4.6, () => Click(MouseButton.Left, shift: true));
        At(5.1, () => Press(Key.Tab));
        // Closed, the hotbar still takes a drag from one of its slots to another.
        At(5.3, () => Go(Inv(0)));
        At(5.6, () => Button(MouseButton.Left, true));
        At(5.65, () => Go(Inv(7)));
        At(6.0, () => Button(MouseButton.Left, false));
        // Over to the wire on the floor, which is picked up on the way.
        At(6.2, () => Walk(Key.D, h => Column(h) >= 12));
        // The furnace: a plank into its fuel slot and a chunk into its input with shift.
        At(7.2, () => Aim(14, 4));
        At(7.5, () => Press(Key.E));
        At(7.7, () => Go(Inv(2)));
        At(8.0, () => Click(MouseButton.Left, shift: true));
        At(8.1, () => Go(Inv(3)));
        At(8.4, () => Click(MouseButton.Left, shift: true));
        At(8.6, () => Go(new Place(Spec.Machines.Get("furnace").FuelSlot!.Value, FurnaceId())));
        At(9.2, () => Press(Key.Escape));
        // A stack thrown with Q, the way the hero faces; then back to the crate.
        At(14.2, () => Press(Key.Key2));
        At(14.3, () => Press(Key.Q));
        At(14.8, () => Walk(Key.A, h => Column(h) <= 6));
        At(15.6, () => Aim(3, 4));
        At(15.9, () => Press(Key.E));
        At(16.1, () => Go(Inv(11)));
        At(16.4, () => Click(MouseButton.Left, shift: true));
        At(16.6, () => Go(Inv(5)));
        At(16.8, () => Button(MouseButton.Left, true));
        At(16.85, () => Go(new Place(4, CrateId())));
        At(17.3, () => Button(MouseButton.Left, false));
        // The sort keys (0081): the crate's in its header, then the bag's on the display's edge, each held a moment.
        At(17.6, () => GoTo(_view.Panel.SortKeyAt(crate: true)));
        At(18.0, () => Button(MouseButton.Left, true));
        At(18.15, () => Button(MouseButton.Left, false));
        At(18.8, () => GoTo(_view.Panel.SortKeyAt(crate: false)));
        At(19.2, () => Button(MouseButton.Left, true));
        At(19.35, () => Button(MouseButton.Left, false));
    }

    private void GoTo(Vector2? at)
    {
        _follow = null;
        if (at is { } point) _target = point;
    }

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Go(Place place)
    {
        _follow = null;
        _target = _view.Panel.CenterOf(place);
    }

    /// <summary>Points at the middle of the drawing's cell (<paramref name="x"/>, <paramref name="y"/>), wherever the camera takes it.</summary>
    private void Aim(int x, int y) =>
        _follow = () => GetViewport().GetCanvasTransform() * (new Vector2(2 * x + 1, 2 * y + 1) * ShipCamera.Cell);

    private int MachineId(char c) => _view.Grid?.MachineAt(Map.CellsOf(c).First())?.Id ?? -1;
    private int FurnaceId() => MachineId('F');
    private int CrateId() => MachineId('K');

    private void Walk(Key key, Func<HeroSpot, bool> done)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _walking = (key, done);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_walking is var (key, done) && _view.Hero is { } hero && done(hero))
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

    private void Click(MouseButton button, bool shift = false)
    {
        Button(button, true, shift);
        Button(button, false, shift);
    }

    private void Button(MouseButton button, bool pressed, bool shift = false) =>
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            ShiftPressed = shift,
            Position = _pos,
            GlobalPosition = _pos,
        });

    private void Wheel(MouseButton wheel) => Button(wheel, true);

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }
}
