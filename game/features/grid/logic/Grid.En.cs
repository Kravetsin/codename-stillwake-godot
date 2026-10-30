namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Grid = new()
    {
        ["grid.outOfBounds"] = "Outside the ship",
        ["grid.outsideZone"] = "Outside the build area",
        ["grid.occupied"] = "Something is already there",
        ["grid.machineInTheWay"] = "A machine is in the way",
        ["grid.blocked"] = "Something is in the way",
        ["grid.noSuchMaterial"] = "No such material",
        ["grid.noSuchMachine"] = "No such machine",
        ["grid.noExhaust"] = "Its nozzle must go out through the hull into space",
        ["grid.noPlatform"] = "Stands only on a launch platform",
        ["grid.noGround"] = "Needs flat ground under it",
        ["grid.underPlatform"] = "Not under a launch platform: the pod falls through here",
        ["grid.shaftBlocked"] = "Must be empty under it, down to the edge",
        ["grid.reserved"] = "Another ship lies here: nothing is built on it",
        // A vessel is written only inside its own box: the sim's bug if ever seen.
        ["grid.outsideReserved"] = "Outside the other ship",
        // Not in the TypeScript: its types kept a placement to the layers there are.
        ["grid.noSuchLayer"] = "No such layer",
        ["grid.noDoor"] = "There is no door here",
        ["hazards.noWall"] = "There is no wall here",
        ["hazards.neverBreaks"] = "This never breaks",
    };
}
