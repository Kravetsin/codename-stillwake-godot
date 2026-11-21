using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The drone's thoughts and moods (drone-thoughts), for frames, the hero standing still all along: a corridor of hull open to space at its left
/// end, a lamp hanging 30 cells left of the hero, a wall at the right end close by. The hero wears the basic helmet alone, paired, knows the
/// alarms and not the lamp. Curious at the lamp at once; the body cools 0.15° a second, a snowflake at 34° (20 s) and 31° (40 s); a meteor at
/// the right wall at 27 s; creative from 43 s, before the helmet's air runs out, which warms the body; bored at 60 s; at 66 s creative off and
/// the hero succumbs, waking 5 s on: love.
/// </summary>
[Dev("--drone-moods", "a hero in a helmet standing paired by a lamp: curious, cold at 20 and 40 s, a meteor at 27 s, bored at 60 s, a death at 67 s and love (DroneMoodsScenario)")]
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

    private const int Peer = 0, Second = World.TicksPerSecond, Meteor = 27 * Second, Kept = 43 * Second, Dies = 66 * Second;

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
        int helmet = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Helmet.Slot);
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++) slots.Add(i == helmet ? new JsonObject { ["item"] = Spec.Helmet.Basic, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = Peer, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    /// <summary>Creative once the snowflakes have shown, before the air runs out; off for the death, a second before it.</summary>
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (m.Crew.HeroOf(client.Id) == null) return;
        long tick = client.World.Tick;
        if (_step == 0 && tick >= Kept) Send(new Sim.Items.SetCreative(true), "creative: kept alive");
        else if (_step == 1 && tick >= Dies) Send(new Sim.Items.SetCreative(false), "creative off for the death");
        else if (_step == 2 && tick >= Dies + Second) Send(new Sim.Crew.Succumb(), "succumbs");

        void Send(Command command, string what)
        {
            client.Send(command);
            log($"drone-moods: {what} at tick {tick}");
            _step++;
        }
    }
}
