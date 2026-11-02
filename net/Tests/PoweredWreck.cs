using Stillwake.Sim;

namespace Stillwake.Net.Tests;

/// <summary>
/// A ship with what the starting one no longer has: a panel cabled to a lamp and a generator, a vent, a trade station, and
/// a tank of liquid oxygen piped to the generator's input, its air to the vent (liquid-oxygen). The generic runs on it
/// draw cuts of cables and keep power and piped air in their desync coverage.
/// </summary>
internal static class PoweredWreck
{
    public static readonly Layout Layout = Sim.Layout.Parse(
        [
            "                              PPP           ",
            "                  #################         ",
            "                  .......#....L...#         ",
            "                 ........D.....OOOD..D      ",
            "                ..TT.$$..DGG...OOOD..D      ",
            "               ...TT.$$..DGGV..OOOD..D      ",
            "              ###########################   ",
        ],
        ["", "", "", "", "                   o       --", "                  --------- -"],
        [((30, 0), (26, 4)), ((30, 0), (30, 2))]).Doubled().InWorld() with
    { Known = Content.Research.StartKnown };
}
