namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Atmosphere = new()
    {
        ["atmosphere.noTank"] = "There is no tank here",
        ["atmosphere.tankFull"] = "The tank is full",
        ["atmosphere.tankLow"] = "Less than a canister in the tank",
        ["tank.draw"] = "Take {n} ({held}/{all})",
        ["air.aired"] = "Aired · {cells} cells",
        ["air.filling"] = "Filling · {aired}/{cells} cells",
        ["air.venting"] = "Venting to space",
        ["air.vacuum"] = "No air · {cells} cells",
        ["air.holding"] = "Some air · {aired}/{cells} cells",
        // A room tip's lines, after their signs: its share of its vents' making and its leak a minute; the liquid behind it and how long it lasts.
        ["air.made"] = "+{n}/min",
        ["air.leak"] = "−{n}/min",
        ["air.liquid"] = "{held} · {time}",
        ["atmosphere.noVent"] = "No vent here",
        ["atmosphere.notPiped"] = "Vent not piped to a vaporizer",
        ["atmosphere.unpowered"] = "Vaporizer has no power",
        ["atmosphere.dry"] = "The tank behind the vaporizer is empty: load a canister",
        ["atmosphere.leaky"] = "The walls leak more than the vaporizers make: build better walls or another vaporizer",
        ["atmosphere.burning"] = "The fires burning here take their share of the air: build another vaporizer or better walls",
        ["air.tank"] = "{held}/{of}",
    };
}
