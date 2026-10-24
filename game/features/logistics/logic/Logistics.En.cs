namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Logistics = new()
    {
        // Drones (0062): the switch, its badge, the bay.
        ["logistics.noMachine"] = "There is no machine there",
        ["logistics.bay"] = "A drone bay is never served by drones",
        ["logistics.served"] = "Drones keep this going",
        ["logistics.off"] = "Drones leave this alone",
        ["logistics.unreached"] = "No drone bay reaches this",
        ["logistics.switchOff"] = "click: drones leave it alone",
        ["logistics.switchOn"] = "click: let drones serve it",
        ["logistics.drone"] = "put a drone here to fly",
        ["logistics.kept"] = "Drones keep this full: {item}",
        ["logistics.noneInReach"] = "none in reach",
    };
}
