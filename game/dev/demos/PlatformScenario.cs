using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// Launch platforms laid in the hull's floor over space, a lamp lighting them: a run of two pieces cabled to a panel out in space, its
/// lamp green, E opening it and venting the room; a run of three with no cable, red, staying shut; a piece alone.
/// </summary>
[Dev("--platform", "launch platforms in the floor: a run cabled to a panel, lit green, opened by E; a run uncabled, red; a piece alone")]
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
            "#..................#",
            "##__#___###_########",
            "                    ",
        ],
        [],
        [((2, 1), (2, 8)), ((2, 1), (5, 4))]).Doubled();
}
