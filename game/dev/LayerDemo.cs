using System;
using System.Linq;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>
/// A script for the two layers' buttons (0107), by the generator of the wreck's first room, the hero carrying the axe
/// (<c>-- --layer-demo MODE</c>): <c>plan</c> the tablet out with the scrap card and back walls planned over open space by a
/// right drag; <c>fade</c> the right button held on the ceiling, the walls round the pointer faded to show the back wall
/// behind; <c>axe</c> the axe swung by the right button at the room's back wall, and <c>aim</c> its dashed aim just before it strikes.
/// </summary>
public sealed class LayerDemo(Hands hands, string mode)
{
    private (double X, double Y)? _pointer;
    private int _done = -1;

    public void Start() => hands.Mouse.Script = () => _pointer;

    private void Press(int held) => hands.Mouse.Buttons.OnPointer(true, held, _pointer!.Value);

    private void Move(int held) => hands.Mouse.Buttons.OnPointer(false, held, _pointer!.Value);

    /// <summary>A step of the script on world tick <c>client.World.Tick</c>; it says what it does to the log.</summary>
    public void Frame(Client client, IGridQueries grid, Action<string> log)
    {
        if (grid.MachinesOf("generator").FirstOrDefault() is not { } generator) return;
        int w = grid.Width, gx = generator.Cell % w, gy = generator.Cell / w;
        // Each step once, on the first frame of its tick or after: frames outnumber ticks.
        int tick = client.World.Tick switch { >= 40 => 40, >= 30 => 30, >= 20 => 20, >= 18 => 18, _ => -1 };
        if (tick <= _done) return;
        _done = tick;
        (double, double) At(double dx, double dy) => (gx + dx, gy + dy);
        switch (mode)
        {
            case "plan" when tick == 20:
                log("B: the scrap card; a right drag over open space plans its back walls");
                hands.Plan.Toggle();
                hands.Menu.Pick(Tools.All.First(t => t.Id == "wallScrap"));
                _pointer = At(0.5, -7.5);
                Press(Buttons.Right);
                break;
            case "plan" when tick == 30:
                _pointer = At(5.5, -6.5);
                Move(Buttons.Right);
                break;
            case "plan" when tick == 40:
                Move(0);
                _pointer = At(3.5, -2.5);
                Move(0);
                break;
            case "fade" when tick == 20:
                log("the right button held on the ceiling: the walls round the pointer fade");
                _pointer = At(3.5, -5.5);
                Press(Buttons.Right);
                break;
            case "axe" when tick == 20:
            case "aim" when tick == 18:
                log("the axe swung by the right button at the room's back wall");
                _pointer = At(5.5, -2.5);
                Press(Buttons.Right);
                break;
        }
    }
}
