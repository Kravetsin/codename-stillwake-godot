using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>A script for recordings: every doorway opened at tick 20 and shut at tick 40, so shadows and doors are seen to follow.</summary>
public static class Autopilot
{
    /// <summary><c>-- --segments</c>: a door of three segments in a room's wall and a hatch of one in its floor (0104), for <c>--doors</c>.</summary>
    public static readonly Layout Segments = Layout.Parse(
    [
        "#############",
        "#.LL..#..LL.#",
        "#.LL..D..LL.#",
        "#.....D.....#",
        "#.....D..S..#",
        "###D#########",
        "#...........#",
        "#############",
    ]);

    /// <summary>
    /// <c>-- --hatches</c>, a cell a character: a hatch segment as one is built, two cells by one in a deck a cell thick, a
    /// ladder flush under it; above, plans of a lone hatch segment, three side by side and a door of two (<see cref="Plans"/>, 0104).
    /// </summary>
    public static readonly Layout Hatches = Layout.Parse(
    [
        "##################################",
        "#.................LL.............#",
        "#.................LL.............#",
        "#................................#",
        "#................................#",
        "#................................#",
        "#................................#",
        "#................................#",
        "##############hh##################",
        "#.............HH...LL............#",
        "#.............HH...LL............#",
        "#.............HH.................#",
        "#.............HH.................#",
        "#.............HH.................#",
        "#.............HH....S............#",
        "##################################",
    ]) with
    { Known = [.. Stillwake.Content.Research.StartKnown, "door", "hatch", "ladder"] };

    /// <summary>
    /// <c>-- --lone-hatch</c> (0112): two rooms, one over the other, a hatch in the deck between them and no ladder, a cell a character;
    /// a vent fed from a full tank airs the lower one, a
    /// shut door walls its lamp off from it, so the lower left is lit only through the doorways. With <c>--doors</c> the hatch opens and joins the rooms; <c>--lone-hatch=open</c> draws it
    /// open, for <c>--autopilot</c> to climb up through.
    /// </summary>
    public static Layout LoneHatch(bool open) => Layout.Parse(
    [
        "############################",
        "#..LL..............LL......#",
        "#..LL..............LL......#",
        "#..........................#",
        "#..........................#",
        "#..........................#",
        "#..........................#",
        $"############{(open ? "oo" : "hh")}##############",
        "#.................DD..LL...#",
        "#.................DD..LL...#",
        "#.......TTTT......DD.......#",
        "#.......TTTT......DD.......#",
        "#..VV...TTTT......DD.......#",
        "#..VV...TTTT....S.DD.......#",
        "############################",
    ],
    [
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "                            ",
        "         ooo                ",
        "         o                  ",
        "   ------                   ",
        "                            ",
        "                            ",
    ]);

    /// <summary>The plans <see cref="Hatches"/> shows, sent once as the world passes tick 2 (from <paramref name="before"/>).</summary>
    public static IReadOnlyList<Command> Plans(int before, int tick, int width)
    {
        if (before >= 2 || tick < 2) return [];
        var hatch = new Placement("structure", Stillwake.Content.Builds.Hatch);
        var door = new Placement("structure", "door");
        return
        [
            new Sim.Building.PlacePlan(3 * width + 3, hatch),
            new Sim.Building.PlacePlan(3 * width + 8, hatch), new Sim.Building.PlacePlan(3 * width + 10, hatch),
            new Sim.Building.PlacePlan(3 * width + 12, hatch),
            new Sim.Building.PlacePlan(2 * width + 22, door), new Sim.Building.PlacePlan(4 * width + 22, door),
        ];
    }

    public static IReadOnlyList<Command> At(int tick, IGridQueries grid)
    {
        if (tick != 20 && tick != 40) return [];
        var toggles = new List<Command>();
        var s = grid.StructureLayer;
        int w = grid.Width;
        for (int cell = 0; cell < s.Length; cell++)
            if (Doorways.IsDoorway(s[cell]) && DoorShape.Leads(Doorways.LinksOf(cell, w, s.Length, grid.StructureAt))) toggles.Add(new SetDoor(cell, tick == 20));
        return toggles;
    }
}
