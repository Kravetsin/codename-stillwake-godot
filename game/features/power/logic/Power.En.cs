namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Power = new()
    {
        ["power.noNode"] = "Nothing to fix a cable to",
        ["power.sameNode"] = "Both ends on one place",
        ["power.tooLong"] = "Too long: add an anchor",
        ["power.taut"] = "A cable there would be pulled taut",
        ["power.twice"] = "Already cabled",
        ["power.notAboard"] = "Cables only aboard the ship",
        ["power.noCable"] = "No cable here",
        ["power.supply"] = "Power: {supply} made / {demand} drawn",
        ["power.stored"] = "Stored: {stored} / {capacity}",
    };
}
