namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Shared = new()
    {
        ["world.elsewhere"] = "You are not there any more",
        ["world.noSurface"] = "There is no such place",
        ["world.there"] = "You are there already",
        // A tool card's rows (2026-11-07-tool-card-rows): their groups, then each row's label, a small word after it, its value.
        ["card.group.hit"] = "Hit",
        ["card.group.shot"] = "Shot",
        ["card.group.reach"] = "Reach",
        ["card.group.special"] = "Special",
        ["card.damage"] = "Damage",
        ["card.onWrecks"] = "wrecks",
        ["card.block"] = "Block",
        ["card.onRock"] = "rock",
        ["card.noRock"] = "cuts none",
        ["card.pierce"] = "Kept through",
        ["card.energy"] = "Charge a shot",
        ["card.hit"] = "Hits every",
        ["card.seconds"] = "{n}s",
        ["card.arc"] = "Swing",
        ["card.degrees"] = "{n}°",
        ["card.range"] = "Range",
        ["card.cells"] = "{n} cells",
        ["card.reload"] = "Reload time",
        ["card.reel"] = "Reel",
        ["card.pull"] = "Pull",
        ["card.loot"] = "Loot from wrecks",
        ["card.more"] = "+{n}%",
        ["card.perSecond"] = "{n}/s",
        ["card.smeltsHere"] = "Smelts here",
        ["card.burnsHere"] = "Burns here",
        ["card.growsHere"] = "Grows here",
        // A smelt's time on the line of "Smelts here", past a minute.
        ["card.minutes"] = "{n} min",
        // An electric tool's row: what it draws while it works, by a bolt.
        ["card.draw"] = "Battery draw",
        // The name the pointer's tip gives a machine never built, which has no card in the build menu.
        ["machine.lifepod"] = "Lifepod",
        // The helmet lamp's card: how far its light reaches, and its beam across.
        ["card.reach"] = "CELLS REACH",
        ["card.beam"] = "BEAM {n}°",
        ["card.guard"] = "% OFF BLOWS",
        ["card.warmth"] = "{n}% OFF THE COLD",
        // What a battery holds, on its card: the share of full, and what it holds of full in power-ticks.
        ["card.charge"] = "Charge",
        ["card.percent"] = "{n}%",
        ["card.stored"] = "{n} / {all}",
        // A canister's or a bottle's litres on its card, and a stack's all told; what a canister breathed gives the suit, at rest.
        ["card.volume"] = "{n} l",
        ["card.volumes"] = "{n} l · ×{count} = {all} l",
        ["card.breath"] = "+{n} s of air",
        // A charging station's panel: what its slot does now.
        ["charger.empty"] = "empty",
        ["charger.full"] = "full",
        ["charger.charging"] = "charging",
        ["charger.noPower"] = "no power",
        ["charger.rate"] = "+{n}/tick",
        // A station's block on the card: a recipe's header, the keys a click there takes.
        ["card.craft"] = "Craft",
        ["card.fromInputs"] = "craft from the inputs",
        ["card.repeatFromInputs"] = "repeat from the inputs",
        ["card.craftIt"] = "craft",
        ["card.many"] = "×{n}",
        ["card.oneIn"] = "1 in {n}",
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
