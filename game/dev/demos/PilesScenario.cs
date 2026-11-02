using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's ship with an axe in hand and the hero in the hold, to break its piles (hold-piles); with <c>--autopilot</c>
/// the piles are hit by themselves, one after the other, at the axe's pace, for a recording.
/// </summary>
[Dev("--piles", "the wreck's hold with an axe in hand, the piles broken with --autopilot (PilesScenario)")]
public sealed class PilesScenario : DevScenario
{
    private long _next = 20;

    public override Layout? Map => Registry.Wreck;

    public override Func<Registry, int>? Start => m => m.Grid.MachinesOf("pileCargo")[0].Cell - 2 + 2 * m.Grid.Width;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(DieDemo.Seed(world, seed.Modules).Save())!;
        root["slices"]!["crew"]!["heroes"]![0]!["air"] = Spec.Items.Get(Spec.Helmet.Basic).Tank;
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (!Args.Has("--autopilot") || client.World.Tick < _next) return;
        _next = client.World.Tick + Spec.Weapons.Axe.Pace;
        var pile = m.Grid.Machines.FirstOrDefault(x => Spec.Machines.Get(x.Kind).Strength != null);
        if (pile == null) return;
        int w = m.Grid.Width;
        client.Send(new Hew(Spec.Weapons.Axe.Item, pile.Cell % w * 1000 + 500, pile.Cell / w * 1000 + 500, 0));
    }
}
