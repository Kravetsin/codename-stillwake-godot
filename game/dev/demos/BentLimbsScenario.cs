using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// The limbs drawn whole and bent at the joint, against rigid (exp/bent-limbs): the <see cref="HeroAutopilot.Hall"/> played by hand,
/// the axe and the drill on 1 and 2 (<see cref="GroundDemo.Seed"/>), creative; F7 switches the way every figure is drawn
/// (<see cref="BentLimbsDemo"/>), <c>=bent</c> starting bent. With <c>--autopilot</c> the hall's script walks it (<see cref="HeroAutopilot.PosesRun"/>).
/// </summary>
[Dev("--bent-limbs", "the hall by hand, axe and drill on 1-2, creative; F7 draws limbs rigid or bent as one piece, =bent starting bent; --autopilot walks it (exp/bent-limbs)", Shape = DevShape.Either)]
public sealed class BentLimbsScenario : DevScenario
{
    private HeroAutopilot? _script;
    private bool _creative;

    public override Layout? Map => HeroAutopilot.Hall;

    public override World Seed(World world, DevSeed seed) => GroundDemo.Seed(world, seed.Modules);

    public override HeroAutopilot? Script => Args.Has("--autopilot") ? _script ??= HeroAutopilot.PosesRun() : null;

    public override void Attach(DevContext c)
    {
        Figure.BentLimbs = Args.Given("--bent-limbs") == "bent";
        c.Add(new BentLimbsDemo());
        if (Script != null) c.Add(new PosesDemo(Script));
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_creative || m.Crew.HeroOf(client.Id) == null) return;
        _creative = true;
        client.Send(new Sim.Items.SetCreative(true));
    }
}
