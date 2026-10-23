using System;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A script for the plan menu's buttons and the hand tools (0107, 0108), by the generator of the wreck's first room, the
/// hero carrying the axe (<c>-- --layer-demo MODE</c>): <c>plan</c> the tablet out with the scrap back wall's card and back
/// walls planned over open space by a left drag; <c>remove</c> the scrap wall's card marking the ceiling by a right drag;
/// <c>focus</c> the plan out with the back walls focused by the wheel, the rest faded, the pointer on the lifepod (0110);
/// <c>axe</c> the axe swung by the right button at the room's back wall, refused as a ship's, and <c>aim</c> its aim just
/// before; <c>strip</c> the stripper held, a cable laid from the generator to the wall, the cables' overlay showing; <c>pipe</c>
/// the wrench held, a pipe laid aslant from the generator's port to a joint and a branch on from it, the pipes' overlay
/// showing, its menu out and the first span hovered; <c>valve</c> the same with the branch's joint made a valve and shut, the wrench put away.
/// </summary>
public sealed class LayerDemo(Hands hands, string mode)
{
    private (double X, double Y)? _pointer;
    private int _done = -1;

    public void Start() => hands.Mouse.Script = () => _pointer;

    private void Press(int held) => hands.Mouse.Buttons.OnPointer(true, held, _pointer!.Value);

    private void Move(int held) => hands.Mouse.Buttons.OnPointer(false, held, _pointer!.Value);

    private static Tool Card(string id) => Tools.All.First(t => t.Id == id);

    /// <summary>Picks the hotbar slot <paramref name="item"/> is in, as its digit key would.</summary>
    private static void Wield(Registry m, int peer, string item)
    {
        int slot = m.Items.Inventory(peer).ToList().FindIndex(s => s?.Item == item);
        if (slot < 0) return;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Key1 + slot, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Key1 + slot, Pressed = false });
    }

    /// <summary>A step of the script on world tick <c>client.World.Tick</c>; it says what it does to the log.</summary>
    public void Frame(Client client, Registry m, Action<string> log)
    {
        var grid = m.Grid;
        if (grid.MachinesOf("generator").FirstOrDefault() is not { } generator) return;
        int w = grid.Width, gx = generator.Cell % w, gy = generator.Cell / w;
        // Each step once, on the first frame of its tick or after: frames outnumber ticks.
        int tick = client.World.Tick switch { >= 60 => 60, >= 50 => 50, >= 40 => 40, >= 30 => 30, >= 20 => 20, >= 18 => 18, >= 10 => 10, _ => -1 };
        if (tick <= _done) return;
        _done = tick;
        (double, double) At(double dx, double dy) => (gx + dx, gy + dy);
        switch (mode)
        {
            case "plan" when tick == 20:
                log("B: the scrap back wall's card; a left drag over open space plans back walls");
                hands.Plan.Toggle();
                hands.Menu.Pick(Card("backWallScrap"));
                _pointer = At(0.5, -7.5);
                Press(Buttons.Left);
                break;
            case "remove" when tick == 20:
                log("B: the scrap wall's card; a right drag over the ceiling marks it for removal");
                hands.Plan.Toggle();
                hands.Menu.Pick(Card("wallScrap"));
                _pointer = At(2.5, -5.5);
                Press(Buttons.Right);
                break;
            case "plan" or "remove" when tick == 30:
                _pointer = At(5.5, -6.5);
                Move(mode == "plan" ? Buttons.Left : Buttons.Right);
                break;
            case "plan" or "remove" when tick == 40:
                Move(0);
                _pointer = At(3.5, -2.5);
                Move(0);
                break;
            case "focus" when tick == 20:
                log("B, the wheel twice: the back walls focused, the rest faded; the pointer on the lifepod");
                hands.Plan.Toggle();
                hands.Plan.TurnFocus(1);
                hands.Plan.TurnFocus(1);
                _pointer = grid.MachinesOf("lifepod").FirstOrDefault() is { } pod ? (pod.Cell % w + 3.0, pod.Cell / w + 3.0) : At(3.5, -2.5);
                break;
            case "axe" when tick == 20:
            case "aim" when tick == 18:
                log("the axe swung by the right button at the room's back wall");
                _pointer = At(5.5, -2.5);
                Press(Buttons.Right);
                break;
            case "pipe" or "valve" when tick == 10:
                log("creative; the wrench held: the pipes show");
                client.Send(new Sim.Items.SetCreative(true));
                Wield(m, client.Id, Content.HandTools.Wrench);
                break;
            case "pipe" or "valve" when tick == 20:
                log("a pipe laid aslant from the generator's port to a joint on the back wall");
                _pointer = At(0.5, 2.5);
                Press(Buttons.Left);
                break;
            case "pipe" or "valve" when tick == 30:
                _pointer = At(5.5, -3.5);
                Move(0);
                break;
            case "pipe" or "valve" when tick == 40:
                log("a branch on from the joint, down and right");
                _pointer = At(5.5, -3.5);
                Press(Buttons.Left);
                _pointer = At(9.5, -0.5);
                Move(0);
                break;
            case "pipe" or "valve" when tick == 50:
                _pointer = At(-1.5, -4.5);
                Press(Buttons.Left);
                _pointer = At(5.5, -3.5);
                Move(0);
                _pointer = At(7.5, 1.5);
                if (mode == "valve")
                {
                    log("the joint made a valve and shut; the wrench put away");
                    int joint = (gy - 4) * w + gx + 5;
                    client.Send(new Sim.Pipes.MakeValve(joint, true));
                    client.Send(new Sim.Pipes.SetValve(joint, false));
                    Wield(m, client.Id, Content.Weapons.Axe.Item);
                }
                break;
            case "pipe" when tick == 60:
                log("the first span hovered: lit whole, what the right button would take");
                _pointer = At(3, -0.5);
                Move(0);
                break;
            case "strip" when tick == 10:
                log("creative, so the cable is paid; the stripper held: the cables show");
                client.Send(new Sim.Items.SetCreative(true));
                Wield(m, client.Id, Content.HandTools.Stripper);
                break;
            case "strip" when tick == 20:
                log("a cable laid from the generator to the wall");
                _pointer = At(1.5, 1.5);
                Press(Buttons.Left);
                break;
            case "strip" when tick == 30:
                _pointer = At(-4.5, -3.5);
                Move(Buttons.Left);
                break;
            case "strip" when tick == 40:
                Move(0);
                _pointer = At(4.5, -1.5);
                Move(0);
                break;
        }
    }
}
