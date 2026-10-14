using System;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// A script for recordings of the plan and the power and air overlays (<c>-- --plan-demo</c>), frame by
/// frame at 20 a second in the wreck: the menu with what is known and a card's tooltip; creative mode
/// and every card; a wall dragged over the lamp, red where it may not go, and planned; the plan put
/// away and a blueprint built by holding the left button on it; the cable card, the lamp's cable carried
/// by its plug to the wall and dropped, a new cable laid from the lamp; then each overlay in turn.
/// </summary>
public sealed class PlanDemo(Hands hands, Layout layout)
{
    private (double X, double Y)? _pointer;
    private int _frame;
    private readonly int _lamp = Cell(layout, 'L');
    private readonly int _width = layout.Width;

    private static int Cell(Layout layout, char c)
    {
        foreach (int cell in layout.CellsOf(c)) return cell;
        throw new InvalidOperationException($"no {c} in the layout");
    }

    /// <summary>Points at the script's pointer from now on, in place of the mouse.</summary>
    public void Start() => hands.Mouse.Script = () => _pointer;

    private (double X, double Y) Of(int cell, double dx = 0.5, double dy = 0.5) => (cell % _width + dx, cell / _width + dy);

    private void Press(int held, bool shift = false) => hands.Mouse.Buttons.OnPointer(true, held, _pointer!.Value, shift);
    private void Release() => hands.Mouse.Buttons.OnPointer(false, 0, _pointer!.Value, false);

    /// <summary>The pointer moved from <paramref name="from"/> to <paramref name="to"/> over frames <paramref name="start"/> to <paramref name="end"/>.</summary>
    private void Glide(int start, int end, (double X, double Y) from, (double X, double Y) to)
    {
        if (_frame < start || _frame > end) return;
        double t = (double)(_frame - start) / (end - start);
        _pointer = (from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
        hands.Mouse.Buttons.OnPointer(false, Held, _pointer.Value, Shift);
    }

    private int Held { get; set; }
    private bool Shift { get; set; }

    private static Tool Card(string id) => Tools.All.First(t => t.Id == id);

    /// <summary>The next frame of the script; it says what it does to the log.</summary>
    public void Frame(Client client, IGridQueries grid, Action<string> log)
    {
        int lampX = _lamp % _width, lampY = _lamp / _width;
        int Around(int dx, int dy) => (lampY + dy) * _width + lampX + dx;
        // The lamp hangs under the ceiling of the room the hero wakes in; a wall planned beside it, left.
        var wallFrom = Of(Around(-4, 0));
        var wallOver = Of(Around(1, 0));
        var wallTo = Of(Around(-1, 0));
        var plug = CableShape.EndOf(grid, _lamp);
        var peg = Of(Around(3, -1));
        switch (_frame)
        {
            case 1:
                _pointer = Of(Around(0, 2));
                break;
            case 10:
                log("B: the plan out, the cards known at the start");
                hands.Plan.Toggle();
                break;
            case 14:
                hands.Menu.Hover(Card("wallScrap"));
                break;
            case 18:
                hands.Menu.Hover(null);
                log("creative mode: every card");
                client.Send(new SetCreative(true));
                break;
            case 22:
                log("the scrap wall card");
                hands.Menu.Pick(Card("wallScrap"));
                _pointer = wallFrom;
                break;
            case 24:
                Held = Buttons.Left;
                Press(Held);
                break;
            case 38:
                Held = 0;
                Release();
                log("a wall planned along the ceiling");
                break;
            case 44:
                log("B: the plan away; the left button held on the blueprint builds it");
                hands.Plan.Toggle();
                break;
            case 46:
                Held = Buttons.Left;
                Press(Held);
                break;
            case 76:
                Held = 0;
                Release();
                break;
            case 80:
                log("the cable card: the power overlay");
                hands.Menu.Pick(Card("cable"));
                _pointer = plug;
                break;
            case 84:
                log("Shift and the lamp's plug: its cable carried");
                Held = Buttons.Left;
                Shift = true;
                Press(Held, shift: true);
                break;
            case 96:
                Held = 0;
                Shift = false;
                Release();
                log("let go on the wall: a peg there");
                break;
            case 106:
                _pointer = Of(_lamp);
                break;
            case 108:
                log("a new cable laid from the lamp");
                Held = Buttons.Left;
                Press(Held);
                break;
            case 120:
                Held = 0;
                Release();
                break;
            case 132:
                log("B: the plan away");
                hands.Plan.Toggle();
                break;
            case 136:
                log("O: air");
                hands.Overlays.Press("O");
                _pointer = Of(Around(1, 2));
                break;
            case 146:
                log("P: pipes");
                hands.Overlays.Press("P");
                break;
            case 156:
                log("L: power");
                hands.Overlays.Press("L");
                _pointer = Of(_lamp);
                break;
        }
        Glide(24, 32, wallFrom, wallOver);
        Glide(34, 37, wallOver, wallTo);
        Glide(46, 60, wallTo, wallTo);
        Glide(61, 75, Of(Around(-2, 0)), Of(Around(-2, 0)));
        Glide(84, 92, plug, peg);
        Glide(108, 116, Of(_lamp), peg);
        _frame++;
    }
}
