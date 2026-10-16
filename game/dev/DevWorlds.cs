using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>Worlds for development: the reference world the budgets are measured on, and a gallery of every picture.</summary>
public static class DevWorlds
{
    /// <summary>
    /// The reference world of CLAUDE.md's budgets (dev/perf-map.ts): 256×128 cells, every layer in
    /// use — walls, floors, ladders, doors, pipe runs with valves, tanks and vents — and a lamp in one
    /// room of five, about a hundred shadowed lights.
    /// </summary>
    public static Layout Perf()
    {
        const int width = 256, height = 128;
        string[] tile =
        [
            "########",
            "#TT.V.-#",
            "#TT.H.v#",
            "#=.DH.-#",
            "#--x-H.#",
            "#.V.dH.#",
            "#==..=.#",
            "########",
        ];
        var rows = new List<string>();
        for (int y = 0; y < height; y++)
        {
            var row = string.Concat(Enumerable.Range(0, width / 8).Select(tx =>
                y % 8 == 1 && (tx + y / 8 * 3) % 5 == 0 ? "#TT.VL-#" : tile[y % 8]));
            rows.Add(row);
        }
        rows[1] = "#TTS" + rows[1][4..];
        return Layout.Parse(rows);
    }

    /// <summary>
    /// Every picture of the ship side by side: scrap walls and back walls on the left, iron on the
    /// right, doors shut and open, pipes with both valves, machines, and a hole in a back wall that
    /// lets the stars in.
    /// </summary>
    public static World Gallery(IReadOnlyList<Module> modules)
    {
        string[] rows =
        [
            "                                        ",
            "  ##############        ##############  ",
            "  #....L.......#  PPP   #......L.....#  ",
            "  #............##########............#  ",
            "  #..AA...TT...D........d....FF..KK..#  ",
            "  #..AA...TT..VD...GG...d....FF......#  ",
            "  #=====H======D...GG...d==H=========#  ",
            "  #.....H.-v-x-##########-.H.........#  ",
            "  #.....H......##########..H...S.....#  ",
            "  ####################################  ",
            "                                        ",
        ];
        string[] under = ["", "", "", "", "", "", "", "               ----------"];
        var layout = Layout.Parse(rows, under);
        var save = JsonNode.Parse(World.Create(1, layout, modules).Save())!;
        // Iron is built, never drawn on a map: the right room is turned to iron in the save.
        var grid = save["slices"]!["grid"]!;
        int iron = Materials.Get("iron").Code;
        var material = Runs.Cells(grid["material"]!);
        var back = Runs.Cells(grid["back"]!);
        var backMaterial = Runs.Cells(grid["backMaterial"]!);
        for (int cell = 0; cell < layout.Width * layout.Height; cell++)
        {
            int x = cell % layout.Width, y = cell / layout.Width;
            if (x >= 24 && material[cell] != 0) material[cell] = iron;
            if (x >= 24 && back[cell] != 0) backMaterial[cell] = iron;
            // A hole in the left room's back wall, where the stars shine in.
            if (x is >= 11 and <= 12 && y is >= 2 and <= 3) { back[cell] = 0; backMaterial[cell] = 0; }
        }
        grid["material"] = Runs.Node(material);
        grid["back"] = Runs.Node(back);
        grid["backMaterial"] = Runs.Node(backMaterial);
        return World.Load(save.ToJsonString(), modules);
    }
}
