using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --wreck</c>: the hero floats out in space with a fire axe in the second hotbar slot, a hull
/// section drifting slowly up to it, three more behind it already chopped a third, a half and four
/// fifths of the way to show the cracks; and a script played through Godot's own input, for recordings
/// with <c>--write-movie</c>: the pointer on the section, bare-handed (outlined red, too big), the axe
/// chosen with 2, the left button held till the section breaks, then the hero swims through what it
/// broke into. It draws the pointer, which a movie does not. <c>-- --wrecks</c> lines up one of every
/// kind of wreckage instead, and the hero swims from one to the next, chopping each till it breaks.
/// <c>-- --harpoon</c> puts a harpoon in the third slot and a cargo crate and a hull section out of reach:
/// the harpoon chosen with 3, the crate shot, hooked and reeled in with a second click, caught with E;
/// then, once the gun is ready again, the section shot and reeled in close enough for the axe (0044).
/// </summary>
public partial class WreckDemo : CanvasLayer
{
    /// <summary>
    /// A scrap of hull far to the left, so debris has a ship to drift past; the hero wakes out in space at S. Drawn a character a
    /// 16 px cell and doubled (0104), as the pieces' places below are in its drawing's cells, doubled in <see cref="Seed"/>.
    /// </summary>
    public static readonly Layout Map = Layout.Parse(
        [
            new string(' ', 120),
            "",
            "",
            "",
            "",
            "  #####",
            "  #...#",
            "  #...#                                S",
            "  #####",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
        ]).Doubled() with
    { Known = Spec.Research.StartKnown };

    /// <summary>The section chopped, and the others: id, cells right of and below the hero's cell, damage.</summary>
    private const int Target = 1;
    private static readonly (int Id, string Item, double Right, int Below, int Damage)[] Drifting =
    [
        (Target, "hullSection", 4.5, -1, 0),
        (2, "hullSection", 10.5, -5, 5),
        (3, "hullSection", 11.5, -1, 8),
        (4, "hullSection", 10.5, 3, 12),
        (5, "scrapChunk", 7.5, -4, 0),
        (6, "hullPlate", 15.5, 1, 0),
    ];
    /// <summary>How fast they drift, slower than the sim's own, so the section stays in reach as it is chopped.</summary>
    private const int Speed = 24;

    /// <summary>For <c>--harpoon</c>: a crate up and right, out of the hands' reach, and a hull section farther, below.</summary>
    private static readonly (int Id, string Item, double Right, int Below, int Damage)[] Shootable =
    [
        (1, "cargoCrate", 9.5, -2, 0),
        (2, "hullSection", 10.5, 3, 0),
    ];

    /// <summary>One of every kind of wreckage, in a row at the hero's height from four cells right of it, two cells apart.</summary>
    private static (int Id, string Item, double Right, int Below, int Damage)[] Row()
    {
        var row = new List<(int, string, double, int, int)>();
        double at = 4;
        foreach (var (item, _) in Spec.Wreckage.All)
        {
            double width = DebrisLooks.Wrecks[item].W / (2.0 * ShipCamera.Cell);
            row.Add((row.Count + 1, item, at + width / 2, 0, 0));
            at += width + 2;
        }
        return [.. row];
    }

    /// <summary>
    /// <paramref name="world"/>, on <see cref="Map"/>, with the pieces out (every kind in a row with
    /// <paramref name="all"/>, two to shoot with <paramref name="harpoon"/>), the axe in peer 0's second
    /// hotbar slot and a harpoon in its third: put into its save, as no command makes items or places debris.
    /// </summary>
    /// <summary>For <c>--grapple</c>: a hall aboard, lamps under its ceiling (lit with <c>--lit</c>), the hero waking at its left end.</summary>
    public static readonly Layout Hall = Layout.Parse(
        [
            new string(' ', 30),
            "  ########################",
            "  #...L.......L.......L..#",
            "  #......................#",
            "  #......................#",
            "  #......................#",
            "  #......................#",
            "  #.....S................#",
            "  ########################",
            "",
        ]).Doubled() with
    { Known = Spec.Research.StartKnown };

    /// <summary><paramref name="world"/>, on <see cref="Hall"/>, with a harpoon in peer 0's first hotbar slot, put in through its save.</summary>
    public static World SeedGrapple(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++) slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Harpoon.Item, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, bool all = false, bool harpoon = false)
    {
        var drifting = all ? Row() : harpoon ? Shootable : Drifting;
        var root = JsonNode.Parse(world.Save())!;
        int width = Map.Width, spawn = Map.CellsOf('S').First();
        int sx = spawn % width, sy = spawn / width;
        // The hull's columns, its drawing's doubled.
        const int Right = 13, Left = 4, Lead = Spec.Debris.Lead;
        int start = Math.Min(width - 1, Right + Lead) * 1000 + 500, end = Math.Max(0, Left - Lead) * 1000;
        var pieces = new JsonArray();
        foreach (var (id, item, right, below, damage) in drifting)
            pieces.Add(new JsonObject
            {
                ["id"] = id,
                ["zone"] = Spec.Debris.Ship.Id,
                ["item"] = item,
                ["x"] = (int)((sx + 2 * right) * 1000),
                ["y"] = (sy + 2 * below) * 1000,
                ["speed"] = Speed,
                ["start"] = start,
                ["end"] = end,
                ["damage"] = damage,
            });
        // Waking in space, the hero leaves no back wall where it woke: S's block, its bottom-left.
        var grid = root["slices"]!["grid"]!;
        var back = Runs.Cells(grid["back"]!);
        var backMaterial = Runs.Cells(grid["backMaterial"]!);
        foreach (int cell in (int[])[spawn, spawn + 1, spawn - width, spawn - width + 1]) back[cell] = backMaterial[cell] = 0;
        grid["back"] = Runs.Node(back);
        grid["backMaterial"] = Runs.Node(backMaterial);
        var debris = root["slices"]!["debris"]!;
        debris["pieces"] = pieces;
        debris["nextId"] = drifting.Length + 1;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 1 ? new JsonObject { ["item"] = Spec.Weapons.Axe.Item, ["count"] = 1 }
                : i == 2 ? new JsonObject { ["item"] = Spec.Harpoon.Item, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly DebrisView _view;
    private readonly Ui _ui;
    /// <summary>The pointer, drawn over everything on the screen.</summary>
    private readonly Node2D _arrow = new();
    private Vector2 _pos = new(640, 300);
    private double _t;
    private bool _chose, _holding, _broken;
    private double _brokeAt;
    private bool _swimming, _swum, _done;
    /// <summary>Every kind in a row (<c>--wrecks</c>), chopped one after another: the one at it now, by id.</summary>
    private readonly bool _all;
    private int _target = Target;
    private Key? _swim;
    /// <summary>The harpoon's script (<c>--harpoon</c>): the piece shot at now, its step, and when that step began.</summary>
    private readonly bool _harpoon, _grapple;
    private int _shotAt = 1, _stage;
    private double _stageAt, _firedAt = -100;
    private bool _clicking, _reeled;

    public WreckDemo(DebrisView view, Ui ui, bool all = false, bool harpoon = false, bool grapple = false)
    {
        _view = view;
        _ui = ui;
        _all = all;
        _harpoon = harpoon;
        _grapple = grapple;
        Layer = ScreenLayers.Pointer;
        AddChild(_arrow);
        _arrow.Draw += () =>
        {
            Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
            _arrow.DrawColoredPolygon(arrow, Colors.White);
            _arrow.DrawPolyline([.. arrow, _pos], Colors.Black, 1);
        };
    }

    /// <summary>Where piece <paramref name="id"/> is on the screen; none once it is gone.</summary>
    private Vector2? OnScreen(int id) =>
        _view.Pieces.FirstOrDefault(p => p.Id == id) is { } p
            ? _view.GetViewport().GetCanvasTransform() * (new Vector2(p.X, p.Y) / 1000f * ShipCamera.Cell)
            : null;

    private static void Press(Key key, bool pressed) =>
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });

    private void Mouse(bool held) =>
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = _pos,
            GlobalPosition = _pos,
        });

    /// <summary>Swims with <paramref name="key"/> held, or none; the key held before is let go.</summary>
    private void Swim(Key? key)
    {
        if (key == _swim) return;
        if (_swim is { } was) Press(was, false);
        if (key is { } now) Press(now, true);
        _swim = key;
    }

    public override void _Process(double delta)
    {
        if (_done) return;
        _t += delta;
        if (_all)
        {
            PlayRow(delta);
            return;
        }
        if (_harpoon)
        {
            PlayHarpoon(delta);
            return;
        }
        if (_grapple)
        {
            PlayGrapple(delta);
            return;
        }
        var target = OnScreen(Target);
        if (target is { } at && !_broken) _pos = _pos.MoveToward(at, (float)(1500 * delta));
        // Bare-handed first, outlined red; then the axe.
        if (!_chose && _t >= 2.2)
        {
            Press(Key.Key2, true);
            Press(Key.Key2, false);
            _chose = true;
            GD.Print($"[demo] axe chosen at {_t:0.00} s");
        }
        if (_chose && !_holding && !_broken && _t >= 3.0 && _ui.AimedPiece.Get() is (Target, false))
        {
            Mouse(true);
            _holding = true;
            GD.Print($"[demo] chopping from {_t:0.00} s");
        }
        if (_holding && target == null)
        {
            Mouse(false);
            (_holding, _broken, _brokeAt) = (false, true, _t);
            GD.Print($"[demo] section broken at {_t:0.00} s");
        }
        // Then it swims to what the section broke into, drifting back past the ship toward it.
        if (_broken && !_swimming && !_swum && _t >= _brokeAt + 0.6)
        {
            _swimming = true;
            Press(Key.D, true);
        }
        if (_swimming && _t >= _brokeAt + 2.6)
        {
            Press(Key.D, false);
            (_swimming, _swum) = (false, true);
        }
        // The script played, the mouse is the player's again: it no longer pins the pointer where it ended.
        if (_swum && _t >= _brokeAt + 3.5)
        {
            _done = true;
            _arrow.Visible = false;
            GD.Print($"[demo] done at {_t:0.00} s: the mouse is the player's");
            return;
        }
        Pointer();
    }

    /// <summary>
    /// The harpoon's script: 3 chooses it; the pointer goes to the piece, a click fires, a second click once
    /// it is hooked reels it in; a crate in is caught with E. Then the section, once the gun is ready again.
    /// </summary>
    private void PlayHarpoon(double delta)
    {
        // A click is a press one frame and its release the next.
        if (_clicking)
        {
            Mouse(false);
            _clicking = false;
        }
        var piece = _view.Pieces.FirstOrDefault(p => p.Id == _shotAt);
        if (OnScreen(_shotAt) is { } at) _pos = _pos.MoveToward(at, (float)(900 * delta));
        void Next(string said)
        {
            GD.Print($"[demo] {said} at {_t:0.00} s");
            (_stage, _stageAt) = (_stage + 1, _t);
        }
        switch (_stage)
        {
            case 0 when _t >= 1.0:
                Press(Key.Key3, true);
                Press(Key.Key3, false);
                Next("harpoon chosen");
                break;
            // The gun is ready again 5 s after its last shot.
            case 1 when _t >= _stageAt + 1.2 && _t >= _firedAt + 5.5 && piece != null && OnScreen(_shotAt) is { } aim && aim.DistanceTo(_pos) < 2:
                Click();
                _firedAt = _t;
                Next($"fired at piece {_shotAt}");
                break;
            case 2 when _t >= _stageAt + 0.8:
                Click();
                Next($"reeling piece {_shotAt}");
                break;
            case 3:
                _reeled |= piece?.Reel != null;
                if (piece != null && (!_reeled || piece.Reel != null) && _t < _stageAt + 6) break;
                Next($"piece {_shotAt} in");
                break;
            case 4 when _t >= _stageAt + 0.6:
                if (piece is { Item: "cargoCrate" })
                {
                    Press(Key.E, true);
                    Press(Key.E, false);
                }
                Next(piece is { Item: "cargoCrate" } ? "crate caught" : "done with it");
                break;
            case 5 when _shotAt == 1 && _t >= _stageAt + 0.5:
                (_shotAt, _stage, _reeled) = (2, 1, false);
                GD.Print($"[demo] to the section at {_t:0.00} s");
                break;
            case 5 when _shotAt == 2 && _t >= _stageAt + 1.5:
                _done = true;
                _arrow.Visible = false;
                GD.Print($"[demo] done at {_t:0.00} s: the mouse is the player's");
                return;
        }
        Pointer();
    }

    /// <summary>
    /// The grapple's script (<c>--grapple</c>): 1 chooses the harpoon; the pointer goes to the hall's ceiling up
    /// and to the right, a click fires into it, a second pulls the hero up to it; it hangs there a while, then D
    /// lets go and it drops; then the same into the far wall.
    /// </summary>
    private void PlayGrapple(double delta)
    {
        if (_clicking)
        {
            Mouse(false);
            _clicking = false;
        }
        // In the ceiling over the hall's middle, then in its right wall, in thousandths of a cell of the doubled hall.
        // Hanging, the pointer goes back into the hall, where a player would aim next.
        var at = _stage is 4 or 5 ? new Vector2(26_000, 10_000) : _shotAt == 1 ? new Vector2(31_000, 3_800) : new Vector2(50_200, 11_000);
        var aim = GetViewport().GetCanvasTransform() * (at / 1000f * ShipCamera.Cell);
        _pos = _pos.MoveToward(aim, (float)(1500 * delta));
        void Next(string said)
        {
            GD.Print($"[demo] {said} at {_t:0.00} s");
            (_stage, _stageAt) = (_stage + 1, _t);
        }
        switch (_stage)
        {
            case 0 when _t >= 0.8:
                Press(Key.Key1, true);
                Press(Key.Key1, false);
                Next("harpoon chosen");
                break;
            case 1 when _t >= _stageAt + 0.8 && _t >= _firedAt + 5.5 && aim.DistanceTo(_pos) < 2:
                Click();
                _firedAt = _t;
                Next(_shotAt == 1 ? "fired into the ceiling" : "fired into the wall");
                break;
            case 2 when _t >= _stageAt + 0.6:
                Click();
                Next("pulling");
                break;
            case 3 when _t >= _stageAt + 2.0:
                Next("hanging");
                break;
            case 4 when _t >= _stageAt + 1.2:
                Press(Key.D, true);
                Next("let go with D");
                break;
            case 5 when _t >= _stageAt + 0.15:
                Press(Key.D, false);
                Next("dropped");
                break;
            case 6 when _shotAt == 1 && _t >= _stageAt + 0.5:
                (_shotAt, _stage) = (2, 1);
                break;
            case 6 when _shotAt == 2 && _t >= _stageAt + 1.5:
                _done = true;
                _arrow.Visible = false;
                GD.Print($"[demo] done at {_t:0.00} s: the mouse is the player's");
                return;
        }
        Pointer();
    }

    private void Click()
    {
        Mouse(true);
        _clicking = true;
    }

    private void Pointer()
    {
        Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        _arrow.QueueRedraw();
    }

    /// <summary>
    /// The row's script: the axe chosen, then each wreck in turn swum up to till the blade would touch it and
    /// chopped till it breaks; past the last, the hero swims on through what they broke into.
    /// </summary>
    private void PlayRow(double delta)
    {
        if (!_chose && _t >= 1.0)
        {
            Press(Key.Key2, true);
            Press(Key.Key2, false);
            _chose = true;
        }
        var target = OnScreen(_target);
        if (target == null && _target <= Spec.Wreckage.All.Count && _chose)
        {
            GD.Print($"[demo] {Spec.Wreckage.All[_target - 1].Item} broken at {_t:0.00} s");
            _target += 1;
            _brokeAt = _t;
            return;
        }
        if (_target > Spec.Wreckage.All.Count)
        {
            Mouse(false);
            _holding = false;
            Swim(_t < _brokeAt + 2.5 ? Key.D : null);
            if (_t >= _brokeAt + 3.0)
            {
                _done = true;
                _arrow.Visible = false;
                GD.Print($"[demo] done at {_t:0.00} s: the mouse is the player's");
                return;
            }
            Pointer();
            return;
        }
        if (target is { } at) _pos = _pos.MoveToward(at, (float)(1500 * delta));
        bool aimed = _chose && _ui.AimedPiece.Get() is (int id, false) && id == _target;
        if (aimed != _holding)
        {
            Mouse(aimed);
            _holding = aimed;
        }
        // Up to it while the blade cannot reach, backing off once the hero is inside it.
        var piece = _view.Pieces.FirstOrDefault(p => p.Id == _target);
        if (piece != null && _ui.Heroes.Count > 0 && _chose)
        {
            float gap = piece.X / 1000f * ShipCamera.Cell - _ui.Heroes[0].X;
            float half = DebrisLooks.Wrecks[piece.Item].W / 2f;
            Swim(aimed ? null : gap > half + 2 * ShipCamera.Cell ? Key.D : gap < half * 0.5f ? Key.A : null);
        }
        Pointer();
    }
}
