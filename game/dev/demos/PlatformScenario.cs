using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// Launch platforms to look at, a lamp lighting them: a run of two pieces cabled to a panel out in space, its lamp green; a run of three with no
/// cable, red; a piece alone. Nothing opens yet.
/// </summary>
[Dev("--platform", "launch platforms: a run cabled to a panel, lit green, a run uncabled, red, and a piece alone")]
public sealed class PlatformScenario : DevScenario
{
    public override Layout? Map => Layout.Parse(
        [
            "                    ",
            "  PPP               ",
            "                    ",
            "####################",
            "#....L.............#",
            "#..................#",
            "#.S................#",
            "#.__.___..._.......#",
            "####################",
        ],
        [],
        [((2, 1), (2, 7)), ((2, 1), (5, 4))]).Doubled();
}
