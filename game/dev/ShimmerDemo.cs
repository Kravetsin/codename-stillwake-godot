using System;
using System.Linq;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>
/// A script for recording heat shimmer against what is drawn over the world (<c>-- --testbed --near=furnace
/// --shimmer-demo</c>, at 20 frames a second): the pointer on the burning furnace, its outline on (frames 5–30);
/// the plan out on the tablet with the wall card, its ghost over the furnace (30–45); a wall planned above the
/// furnace and the ceiling over it marked for taking down, red (45–70); the plan welded with the right button held
/// on it, aimed at (70–160). Shimmer must bend the world under these and never show them.
/// </summary>
public sealed class ShimmerDemo(Hands hands)
{
    private (double X, double Y)? _pointer;
    private int _frame;

    public void Start() => hands.Mouse.Script = () => _pointer;

    private static Tool Card(string id) => Tools.All.First(t => t.Id == id);

    private void Press(int held) => hands.Mouse.Buttons.OnPointer(true, held, _pointer!.Value);

    private void Release() => hands.Mouse.Buttons.OnPointer(false, 0, _pointer!.Value);

    public void Frame(Client client, IGridQueries grid, Action<string> log)
    {
        if (grid.MachinesOf("furnace").FirstOrDefault() is not { } furnace) return;
        int w = grid.Width, fx = furnace.Cell % w, fy = furnace.Cell / w;
        (double, double) At(double dx, double dy) => (fx + dx, fy + dy);
        switch (_frame)
        {
            case 5:
                log("the pointer on the furnace: its outline on");
                _pointer = At(1, 1);
                hands.Mouse.Buttons.OnPointer(false, 0, _pointer.Value);
                break;
            case 30:
                log("B: the tablet out, the wall card, its ghost over the furnace");
                hands.Plan.Toggle();
                client.Send(new Sim.Items.SetCreative(true));
                hands.Menu.Pick(Card("wallScrap"));
                _pointer = At(0.5, -0.5);
                hands.Mouse.Buttons.OnPointer(false, 0, _pointer.Value);
                break;
            case 45:
                log("a wall planned over the furnace");
                Press(Buttons.Left);
                break;
            case 47:
                _pointer = At(1.5, -0.5);
                hands.Mouse.Buttons.OnPointer(false, Buttons.Left, _pointer.Value);
                break;
            case 49:
                Release();
                break;
            case 55:
                log("the ceiling over it marked for taking down with the removal card");
                hands.Menu.Pick(Card("remove"));
                _pointer = At(-1.5, -2.5);
                Press(Buttons.Left);
                break;
            case 57:
                _pointer = At(3.5, -2.5);
                hands.Mouse.Buttons.OnPointer(false, Buttons.Left, _pointer.Value);
                break;
            case 59:
                Release();
                break;
            case 70:
                log("the tablet away, the screwdriver in hand: the plan welded, aimed at");
                hands.Plan.PutPlanAway();
                _pointer = At(0.5, -0.5);
                Press(Buttons.Left);
                break;
            case 160:
                Release();
                break;
        }
        _frame++;
    }
}
