using Stillwake.Sim;

namespace Stillwake.Net.Tests;

/// <summary>
/// A ship with what the starting one no longer has: a panel cabled to a lamp and a generator, a vent, a trade station. The generic runs on it
/// draw cuts of cables and keep power and piped air in their desync coverage.
/// </summary>
internal static class PoweredWreck
{
    public static readonly Layout Layout = Sim.Layout.Parse(
        [
            "                                  PPP       ",
            "                  #################         ",
            "                  .......#....L...#         ",
            "                 ........D.....OOOD..D      ",
            "                .....$$..DGG...OOOD..D      ",
            "               ......$$..DGGV..OOOD..D      ",
            "              ###########################   ",
        ],
        ["", "", "", "", "", "                              ---"],
        [((34, 0), (30, 4)), ((34, 0), (34, 2))]).Doubled().InWorld() with
    { Known = Content.Research.StartKnown };
}
