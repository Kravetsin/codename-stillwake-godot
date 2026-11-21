using System;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The drone's thoughts and moods (drone-thoughts), for frames, the hero standing still all along: a corridor of hull open to space at its left
/// end, a lamp hanging 30 cells left of the hero, a wall at the right end close by. The hero is bare, paired, knows the alarms and not the lamp.
/// At once the cold shows, then curious at the lamp; at 6 s creative keeps it alive; at 20 s a meteor is seen coming at the right wall; at
/// about 61 s it is bored; at 64 s it dies and wakes 5 s on, love; creative again after.
/// </summary>
[Dev("--drone-moods", "a bare hero standing paired by a lamp: cold, curious, a meteor at 20 s, bored at 61 s, a death at 64 s and love (DroneMoodsScenario)")]
public sealed class DroneMoodsScenario : DevScenario
{
    /// <summary>The corridor, a character a 16 px block, doubled (0104): the lamp at column 27, the hero at 42, the wall struck at 49.</summary>
    private static readonly Layout Corridor = (Layout.Parse(
        [
            new string('#', 50),
            new string('.', 27) + "L" + new string('.', 21) + "#",
            new string('.', 49) + "#",
            new string('.', 42) + "S" + new string('.', 6) + "#",
            new string('.', 49) + "#",
            new string('#', 50),
        ]).Doubled() with
    { Known = Spec.Research.StartKnown }) with
    { Meteors = true };

    private const int Peer = 0, Second = World.TicksPerSecond;
    private const int Kept = 6 * Second, Meteor = 20 * Second, Dies = 64 * Second, Alive = 72 * Second;

    private int _step;

    public override Layout? Map => Corridor;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["players"]!["drones"] = new JsonArray(Peer);
        root["slices"]!["knowledge"]!["learners"] = new JsonArray(new JsonObject
        {
            ["peer"] = Peer,
            ["found"] = new JsonArray(),
            ["granted"] = new JsonArray(Spec.LightDrone.AlarmsMode),
            ["deeds"] = new JsonArray(),
        });
        root["slices"]!["hazards"]!["nextIn"] = Meteor;
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    /// <summary>Creative on once the curious mood has shown, off for the death, on again after waking.</summary>
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (m.Crew.HeroOf(client.Id) is not { } hero) return;
        long tick = client.World.Tick;
        if (_step == 0 && tick >= Kept) Send(new Sim.Items.SetCreative(true), "creative: kept alive");
        else if (_step == 1 && tick >= Dies) Send(new Sim.Items.SetCreative(false), "creative off for the death");
        else if (_step == 2 && tick >= Dies + Second) Send(new Sim.Crew.Succumb(), "succumbs");
        else if (_step == 3 && tick >= Alive && hero.Out == 0) Send(new Sim.Items.SetCreative(true), "woke: creative again");

        void Send(Command command, string what)
        {
            client.Send(command);
            log($"drone-moods: {what} at tick {tick}");
            _step++;
        }
    }
}
