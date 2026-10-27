namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Landing = new()
    {
        ["landing.noPlatform"] = "There is no platform here",
        ["landing.unpowered"] = "No power: the platform stays shut",
        ["landing.noPod"] = "There is no drop pod here",
        ["landing.taken"] = "Someone is already in it",
        ["landing.seated"] = "Already in a drop pod",
        ["landing.notSeated"] = "Not in a drop pod",
        ["landing.noFuel"] = "Needs {fuel} biofuel",
        ["landing.notYours"] = "Not your pod",
        ["landing.cannotLeave"] = "There is nowhere to land",
        ["landing.shaftBlocked"] = "Something is built under the platform",
        ["landing.descend"] = "Descend",
        ["landing.return"] = "Return",
    };
}
