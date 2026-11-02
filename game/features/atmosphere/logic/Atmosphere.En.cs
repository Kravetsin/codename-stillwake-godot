namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Atmosphere = new()
    {
        ["atmosphere.noTank"] = "There is no tank here",
        ["atmosphere.tankFull"] = "The tank is full",
        ["air.aired"] = "Aired · {cells} cells",
        ["air.filling"] = "Filling · {aired}/{cells} cells",
        ["air.venting"] = "Venting to space",
        ["air.vacuum"] = "No air · {cells} cells",
        ["air.holding"] = "Some air · {aired}/{cells} cells",
        ["air.pipes"] = "Liquid O₂ in tanks: {oxygen}",
        ["air.lastsNot"] = "No supply · lasts {time}",
        ["air.keepsUp"] = "Vaporizer keeps up · +{n}/min · tanks last {time}",
        ["air.fallsShort"] = "Vaporizer falls short · lasts {time}",
        ["air.drains"] = "Liquid O₂ in tanks: {oxygen} · gone in {time}",
        ["atmosphere.noVent"] = "No vent here",
        ["atmosphere.notPiped"] = "Vent not piped to a vaporizer",
        ["atmosphere.unpowered"] = "Vaporizer has no power",
        ["atmosphere.dry"] = "The tank behind the vaporizer is empty: load a canister",
    };
}
