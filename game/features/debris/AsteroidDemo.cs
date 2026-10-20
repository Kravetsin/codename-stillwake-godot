using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --asteroid</c>: the big world (0048), the hero out in the asteroid band above the hull (0054) with a
/// jetpack on, the drill on the hotbar's first slot and the fire axe on its second, and one battery on the belt
/// half full: enough for one asteroid and a little of the next. Two asteroids float by it, still, one
/// on either side. A script played through Godot's own input, for recordings with <c>--write-movie</c>: the
/// drill chosen and held on the right one till it breaks into rock, then on the left one till the belt runs
/// flat and the drill winds down, then the axe chosen and the left one chopped till it breaks; a canister breathed
/// whenever the suit runs half empty. With
/// <c>--manual</c> the script stays off and the hands are the player's. It draws the pointer, which a movie does not.
/// </summary>
public partial class AsteroidDemo : CanvasLayer
{
    /// <summary>What the belt's battery holds, in thousandths of full: 300 ticks of the drill running, 240 of them on the first rock.</summary>
    public const int Charge = 500;

    /// <summary>The asteroids, by id: cells right of the hero's middle (left when negative), and up from its cell's top, to their middles.</summary>
    private static readonly (int Id, int Right, int Up)[] Rocks = [(1, 2_050, 300), (2, -2_050, 300)];

    /// <summary>
    /// <paramref name="world"/>, read through <paramref name="m"/>: peer 0 out at <see cref="FarDemo.StartOf"/> with the
    /// <paramref name="drill"/> (the plain one unless said: <c>--arc-drill</c> the arc drill), the axe, canisters, a jetpack and a battery, and the two asteroids beside it, put in through its save.
    /// </summary>
    public static World Seed(World world, Registry m, Func<IReadOnlyList<Module>> modules, string? drill = null)
    {
        var root = JsonNode.Parse(world.Save())!;
        int belt = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Charge.Belt);
        int back = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf("back");
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = drill ?? Spec.Weapons.Drill.Item, ["count"] = 1 }
                : i == 1 ? new JsonObject { ["item"] = Spec.Weapons.Axe.Item, ["count"] = 1 }
                : i == 9 ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = 5 }
                : i == back ? new JsonObject { ["item"] = Spec.Jetpack.Item, ["count"] = 1 }
                : i == belt ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery / 1000 * Charge }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        int start = FarDemo.StartOf(m), sx = start % m.Grid.Width, sy = start / m.Grid.Width;
        root["slices"]!["crew"]!["heroes"] = new JsonArray(new JsonObject
        {
            ["id"] = 0,
            ["cell"] = start,
            ["suit"] = Spec.Crew.Suit,
            ["out"] = 0,
            ["exerting"] = false,
            ["thrusting"] = false,
            ["thrustTicks"] = 0,
        });
        var debris = root["slices"]!["debris"]!;
        var list = debris["pieces"]!.AsArray();
        int next = (int)debris["nextId"]!;
        foreach (var (id, right, up) in Rocks)
        {
            int x = sx * 1000 + 500 + right;
            list.Add(new JsonObject
            {
                ["id"] = next++,
                ["zone"] = Spec.Debris.Asteroids.Id,
                ["item"] = "asteroid",
                ["x"] = x,
                ["y"] = sy * 1000 - up,
                // Still, so the script's hands stay on them through a recording.
                ["speed"] = 0,
                ["start"] = x,
                ["end"] = 0,
                ["damage"] = 0,
                ["reel"] = null,
                ["seenAt"] = 0,
            });
        }
        debris["nextId"] = next;
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly DebrisView _view;
    private readonly Ui _ui;
    private readonly Func<(Client Client, Registry M)?> _world;
    private double _breathed = -10;
    private readonly Node2D _arrow = new();
    private Vector2 _pos = new(640, 300);
    private double _t, _stageAt;
    private int _stage, _first = -1;
    private bool _holding;

    public AsteroidDemo(DebrisView view, Ui ui, Func<(Client Client, Registry M)?> world)
    {
        (_view, _ui, _world) = (view, ui, world);
        Layer = ScreenLayers.Pointer;
        AddChild(_arrow);
        _arrow.Draw += () =>
        {
            Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
            _arrow.DrawColoredPolygon(arrow, Colors.White);
            _arrow.DrawPolyline([.. arrow, _pos], Colors.Black, 1);
        };
    }

    /// <summary>The id of the <paramref name="nth"/> asteroid out at the start, by the first id seen, and where it is on the screen; none once it is gone.</summary>
    private Vector2? OnScreen(int nth)
    {
        if (_first < 0 && _view.Pieces.FirstOrDefault(p => p.Item == "asteroid") is { } p0) _first = p0.Id;
        return _view.Pieces.FirstOrDefault(p => p.Id == _first + nth) is { } p
            ? _view.GetViewport().GetCanvasTransform() * (new Vector2(p.X, p.Y) / 1000f * ShipCamera.Cell)
            : null;
    }

    private static void Tap(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }

    private void Hold(bool held)
    {
        if (held == _holding) return;
        _holding = held;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = _pos,
            GlobalPosition = _pos,
        });
    }

    private void Next(string said)
    {
        GD.Print($"[demo] {said} at {_t:0.00} s");
        (_stage, _stageAt) = (_stage + 1, _t);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        // Out in space the suit runs low over a long recording: a canister breathed whenever it is half empty.
        if (_world() is var (client, m) && m.Crew.HeroOf(client.Id) is { } hero && hero.Suit < Spec.Crew.Suit / 2 && _t - _breathed > 2)
        {
            client.Send(new Sim.Crew.Refill());
            _breathed = _t;
        }
        if (_stage > 6) return;
        int rock = _stage < 3 ? 0 : 1;
        if (OnScreen(rock) is { } at) _pos = _pos.MoveToward(at, (float)(1200 * delta));
        var aimed = _ui.AimedPiece.Get();
        switch (_stage)
        {
            case 0 when _t >= 0.8:
                Tap(Key.Key1);
                Next("drill chosen");
                break;
            case 1 when _t >= _stageAt + 0.8 && aimed is (_, false):
                Hold(true);
                Next("drilling the right rock");
                break;
            case 2 when OnScreen(0) == null:
                Hold(false);
                Next("the right rock broken");
                break;
            case 3 when _t >= _stageAt + 1.2 && aimed is (_, false):
                Hold(true);
                Next("drilling the left rock");
                break;
            // Flat: the sim would refuse the next hit, and the drill winds down; held a while longer to show it.
            case 4 when aimed is (_, true) && _t >= _stageAt + 0.5:
                Next("the belt flat");
                break;
            case 5 when _t >= _stageAt + 2.0:
                Hold(false);
                Tap(Key.Key2);
                Hold(true);
                Next("the axe on the left rock");
                break;
            case 6 when OnScreen(1) == null:
                Hold(false);
                _arrow.Visible = false;
                Next("the left rock broken: done, the mouse is the player's");
                return;
        }
        Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        _arrow.QueueRedraw();
    }
}
