using System;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Hazards;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's wreck with its first meteor seen coming at once, not in minutes: flying in and striking.
/// <c>--meteors=N</c> gives it power N (the shower's 2 to 7), and ten seconds after it strikes a second of that power hits the same
/// row's face, the cell or the wall behind it, as a dev panel hit, no rock seen: a crack, then a breach on the cracked patch (unified-damage).
/// </summary>
[Dev("--meteors", "the wreck's first meteor seen coming at once, of power N with =N, a second on its cell 10 s on (MeteorScenario)", Shape = DevShape.Either)]
public sealed class MeteorScenario : DevScenario
{
    private const int Again = 10 * World.TicksPerSecond;
    private int? _power;
    private Meteor? _coming;
    private (int Cell, int Tick)? _struck;

    public override Layout? Map => Registry.Wreck;

    /// <summary>The next meteor drawn on the first tick by the sim's own rules; its power then set as asked, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["nextIn"] = 1;
        world = World.Load(root.ToJsonString(), seed.Modules());
        if (!int.TryParse(Args.Given("--meteors"), out int power)) return world;
        _power = power;
        world.Step([]);
        root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["incoming"]!["power"] = power;
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    /// <summary>With a power given: the first meteor's strike seen, and ten seconds on, the second sent at its cell.</summary>
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_power is not int power) return;
        if (_struck is var (cell, tick))
        {
            if (client.World.Tick < tick + Again) return;
            // At the row's face now: the cell struck, or the wall behind it when the first broke it.
            for (int i = 0; i < 4 && m.Grid.WallAt(cell) == null && cell % m.Grid.Width > 0; i++) cell--;
            client.Send(new MeteorHit(cell, power));
            log($"meteors: a second of power {power} at cell {cell}");
            _power = null;
            return;
        }
        if (_coming is { } was && m.Hazards.Incoming == null) _struck = (was.Cell, client.World.Tick);
        _coming = m.Hazards.Incoming;
    }
}
