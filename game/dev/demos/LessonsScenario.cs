using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The drone learning its forms (drone-lessons), for frames: a corridor of hull, an emergency lamp hanging at its right end and a piece of
/// wreckage held still before it, the hero paired at the left end with nine wrecks broken already and a sledgehammer in hand, creative so its
/// air never runs out. It walks right into the lamp's reach (the alarms lesson: the drone visits the lamp), then the piece is hit till it
/// breaks, the tenth (the scan lesson).
/// </summary>
[Dev("--lessons", "a hero paired by an emergency lamp and a wreck, nine broken: walks in, learns the alarms, breaks the tenth, learns the scan (LessonsScenario)")]
public sealed class LessonsScenario : DevScenario
{
    /// <summary>The corridor, a character a 16 px block, doubled (0104): the lamp at column 34, the hero at 2, walking to 26.</summary>
    private static readonly Layout Corridor = Layout.Parse(
        [
            new string('#', 40),
            new string('.', 34) + "i" + new string('.', 5),
            new string('.', 40),
            "..S" + new string('.', 37),
            new string('.', 40),
            new string('#', 40),
        ]).Doubled() with
    { Known = Spec.Research.StartKnown };

    /// <summary>The wreck to break: a hull section, six cells by four, held still past where the hero stops, in thousandths of a cell.</summary>
    private const int PieceX = 59_000, PieceY = 4_000, Peer = 0;

    /// <summary>A lesson's beat and the key's long flare (<see cref="Features.Research.FindScan"/>), 7.7 s, in ticks and a little over.</summary>
    private const int LessonTicks = 170;

    private HeroAutopilot? _script;
    private bool _creative, _broken;
    private long? _learned, _next;

    public override Layout? Map => Corridor;

    public override HeroAutopilot? Script => _script ??= HeroAutopilot.Lessons(26);

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["debris"]!["pieces"] = new JsonArray(new JsonObject
        {
            ["id"] = 1,
            ["zone"] = Spec.Debris.Ship.Id,
            ["item"] = "hullSection",
            ["x"] = PieceX,
            ["y"] = PieceY,
            ["speed"] = 0,
            ["start"] = Corridor.Width * 1000,
            ["end"] = 0,
            ["damage"] = 0,
        });
        root["slices"]!["debris"]!["nextId"] = 2;
        root["slices"]!["players"]!["drones"] = new JsonArray(Peer);
        root["slices"]!["knowledge"]!["learners"] = new JsonArray(new JsonObject
        {
            ["peer"] = Peer,
            ["found"] = new JsonArray(),
            ["granted"] = new JsonArray(),
            ["deeds"] = new JsonArray(new JsonObject { ["deed"] = Spec.Lessons.WreckBroken, ["count"] = 9 }),
        });
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++) slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Weapons.Sledgehammer.Item, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = Peer, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (m.Crew.HeroOf(client.Id) == null) return;
        if (!_creative)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        long tick = client.World.Tick;
        // Once the alarms are learned and their lesson has played, the sledgehammer falls on the piece at its pace till it breaks.
        if (_learned == null && m.Knowledge.Knows(client.Id, Spec.LightDrone.AlarmsMode))
        {
            _learned = tick;
            log($"alarms learned at tick {tick}");
        }
        if (_learned is not { } learned || tick < learned + LessonTicks) return;
        if (m.Debris.Pieces.FirstOrDefault() is not { } piece)
        {
            if (!_broken) log($"the tenth broken, the scan learned, at tick {tick}");
            _broken = true;
            return;
        }
        if (tick < _next) return;
        _next = tick + Spec.Weapons.Sledgehammer.Pace;
        client.Send(new Sim.Debris.HewPieces(Spec.Weapons.Sledgehammer.Item, [piece.Id]));
    }
}
