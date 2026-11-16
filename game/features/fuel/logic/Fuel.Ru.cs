namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Fuel = new()
    {
        ["fuel.noStore"] = "Здесь нет бака для топлива",
        ["fuel.low"] = "Здесь меньше топлива",
        ["fuel.draw"] = "Достать {n} ({held}/{all} л · {whole} {whole|канистра|канистры|канистр})",
        ["fuel.held"] = "Биотопливо · {held}/{of} л · {whole} {whole|канистра|канистры|канистр}",
        ["fuel.lasts"] = "Хватит на {time} при таком расходе",
        ["fuel.idle"] = "Расхода нет",
        ["fuel.hours"] = "{h} ч {m} мин",
    };
}
