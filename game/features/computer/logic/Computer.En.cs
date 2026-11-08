namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Computer = new()
    {
        ["computer.noPower"] = "Not enough power",
        ["computer.noModules"] = "No modules found",
        ["computer.select"] = "Select a module",
        ["computer.short.radar"] = "Radar: not enough power",
        ["computer.program.radar"] = "Radar",
        ["computer.radar.ship"] = "My ship",
        ["computer.radar.planet"] = "Planet",
        ["computer.radar.wreck"] = "Derelict ship",
        ["computer.radar.km"] = "{n} km",
        ["computer.radar.setOff"] = "Set off",
        ["contacts.unknownKind"] = "No such kind of radar contact",
        ["contacts.outOfReach"] = "Out of the radar's reach",
        ["use.open.computer"] = "[{key:use}] Ship computer",
    };
}
