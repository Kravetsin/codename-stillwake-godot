namespace Stillwake.Game.I18n;

public static partial class En
{
    private static readonly Strings Fuel = new()
    {
        ["fuel.noStore"] = "It holds no fuel",
        ["fuel.low"] = "Less fuel in it",
        ["fuel.draw"] = "Take {n} ({held}/{all} l · {whole} cans)",
        ["fuel.held"] = "Biofuel · {held}/{of} l · {whole} cans",
        ["fuel.lasts"] = "Lasts {time} at this draw",
        ["fuel.idle"] = "Nothing drawn from it",
        ["fuel.hours"] = "{h} h {m} min",
    };
}
