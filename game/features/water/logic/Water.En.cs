namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Water = new()
    {
        ["water.noCistern"] = "Pour it into a cistern and pipe it here",
        ["water.full"] = "The cistern is full",
        ["water.low"] = "Less water in the cistern",
        ["cistern.draw"] = "Take {n} ({held}/{all} l · {whole} bottles)",
        ["water.noWater"] = "No water: pipe a cistern's output to its input",
        ["water.tanksFull"] = "The tanks on its output are full",
        ["water.noPower"] = "No power",
        ["water.off"] = "Switched off",
        ["water.cistern"] = "Water · {held}/{of} l · {whole} bottles",
        ["water.splitting"] = "Splitting {n} l/min of water into oxygen",
    };
}
