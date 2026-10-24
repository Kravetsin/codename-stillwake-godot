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
        ["air.pipes"] = "O₂ in pipes: {oxygen}",
        ["air.lasts"] = "O₂ in pipes: {oxygen} · lasts {time}",
        ["air.lastsNot"] = "No supply · lasts {time}",
        ["air.keepsUp"] = "Generator keeps up · +{n}/min",
        ["air.fallsShort"] = "Generator falls short · lasts {time}",
        ["air.drains"] = "O₂ in pipes: {oxygen} · gone in {time}",
        ["atmosphere.noVent"] = "No vent here",
        ["atmosphere.notPiped"] = "Vent not piped to a tank or generator",
        ["atmosphere.unpowered"] = "Generator has no power",
        ["atmosphere.dry"] = "Pipes empty: load a canister into a tank",
    };
}
