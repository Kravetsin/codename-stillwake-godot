namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Shared = new()
    {
        ["world.elsewhere"] = "You are not there any more",
        ["world.noSurface"] = "There is no such place",
        ["world.there"] = "You are there already",
        ["card.dps"] = "DPS",
        ["card.damage"] = "DMG",
        ["card.hit"] = "HIT",
        ["card.seconds"] = "{n}s",
        ["card.range"] = "RANGE",
        ["card.reload"] = "RELOAD",
        ["card.reel"] = "REEL",
        ["card.perSecond"] = "{n}/s",
        ["card.notAnalysed"] = "Not analysed",
        ["card.smeltsHere"] = "Smelts here",
        ["card.burnsHere"] = "Burns here",
        ["card.growsHere"] = "Grows here",
        // A smelt's time on the line of "Smelts here", past a minute.
        ["card.minutes"] = "{n} min",
        // An electric tool's tile: what it draws while it works, by a bolt.
        ["card.draw"] = "DRAW",
        // The helmet lamp's card: how far its light reaches, and its beam across.
        ["card.reach"] = "CELLS REACH",
        ["card.beam"] = "BEAM {n}°",
        // What a battery holds, on its card: the share of full, and what it holds of full in power-ticks.
        ["card.charge"] = "Charge",
        ["card.percent"] = "{n}%",
        ["card.stored"] = "{n} / {all}",
        // A charging station's panel: what its slot does now.
        ["charger.empty"] = "empty",
        ["charger.full"] = "full",
        ["charger.charging"] = "charging",
        ["charger.noPower"] = "no power",
        ["charger.rate"] = "+{n}/tick",
        // A station's block on the card: a recipe's header, the keys a click there takes.
        ["card.craft"] = "Craft",
        ["card.fromInputs"] = "craft from the inputs",
        ["card.craftIt"] = "craft",
        ["card.many"] = "×{n}",
        ["card.goTo"] = "go to what is missing",
        ["card.take"] = "take",
        ["card.trade.speed"] = "×{n} speed",
        ["card.trade.power"] = "×{n} power",
        ["card.trade.damage"] = "×{n} damage",
        ["card.trade.charging"] = "×{n} charging",
        ["card.trade.heat"] = "+{n} heat",
        ["card.fitted"] = "{module} fitted",
        ["card.takeOut"] = "take out",
        ["time.seconds"] = "{n} s",
        ["time.minutes"] = "{n} min",
    };
}
