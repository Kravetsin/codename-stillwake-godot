using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The melee strokes to play by hand (swing-motions): the hero out in space beside a scrap of hull, the axe, the pickaxe, the
/// sledgehammer and the spear on the hotbar's first four slots, pieces of wreckage held still above, ahead, below and behind it,
/// in creative mode so nothing runs out while every sector is swung at and the hero pushes off mid-swing.
/// </summary>
[Dev("--melee", "the hero in space by a hull's wall and still pieces all round, axe, pickaxe, sledgehammer and spear on 1-4, creative (swing-motions)")]
public sealed class MeleeScenario : DevScenario
{
    private bool _creative;

    /// <summary>A scrap of hull right of the hero to swing at and push off; drawn a character a 16 px cell and doubled (0104).</summary>
    private static readonly Layout Hull = Layout.Parse(
        [
            new string(' ', 40),
            "",
            "",
            "",
            "                 ######",
            "                 #....#",
            "              S  #....#",
            "                 #....#",
            "                 ######",
            "",
            "",
            "",
            "",
        ]).Doubled() with
    { Known = Spec.Research.StartKnown };

    /// <summary>The pieces held still round the hero: what, and how many drawing cells right of and below it.</summary>
    private static readonly (string Item, double Right, int Below)[] Pieces =
    [
        ("hullSection", -0.5, -3),
        ("scrapChunk", -3.5, -1),
        ("hullPlate", -0.5, 3),
        ("scrapChunk", -3, 2),
        ("hullPlate", 1.5, -2),
    ];

    public override Layout? Map => Hull;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        int width = Hull.Width, spawn = Hull.CellsOf('S').First();
        int sx = spawn % width, sy = spawn / width;
        var pieces = new JsonArray();
        for (int i = 0; i < Pieces.Length; i++)
            pieces.Add(new JsonObject
            {
                ["id"] = i + 1,
                ["zone"] = Spec.Debris.Ship.Id,
                ["item"] = Pieces[i].Item,
                ["x"] = (int)((sx + 2 * Pieces[i].Right) * 1000),
                ["y"] = (sy + 2 * Pieces[i].Below) * 1000,
                ["speed"] = 0,
                ["start"] = width * 1000,
                ["end"] = 0,
                ["damage"] = 0,
            });
        // Waking in space, the hero leaves no back wall where it woke: S's block, its bottom-left.
        var grid = root["slices"]!["grid"]!;
        var back = Runs.Cells(grid["back"]!);
        var backMaterial = Runs.Cells(grid["backMaterial"]!);
        foreach (int cell in (int[])[spawn, spawn + 1, spawn - width, spawn - width + 1]) back[cell] = backMaterial[cell] = 0;
        grid["back"] = Runs.Node(back);
        grid["backMaterial"] = Runs.Node(backMaterial);
        var debris = root["slices"]!["debris"]!;
        debris["pieces"] = pieces;
        debris["nextId"] = Pieces.Length + 1;
        string[] hotbar = [Spec.Weapons.Axe.Item, Spec.Weapons.Pickaxe.Item, Spec.Weapons.Sledgehammer.Item, Spec.Weapons.Spear.Item];
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++) slots.Add(i < hotbar.Length ? new JsonObject { ["item"] = hotbar[i], ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_creative || m.Crew.HeroOf(client.Id) == null) return;
        _creative = true;
        client.Send(new Sim.Items.SetCreative(true));
    }
}
