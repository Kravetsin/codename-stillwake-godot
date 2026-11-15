namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Water = new()
    {
        ["water.noCistern"] = "Залей её в цистерну и подведи трубу сюда",
        ["water.full"] = "Цистерна полна",
        ["water.low"] = "В цистерне меньше воды",
        ["cistern.draw"] = "Достать {n} ({held}/{all} л · {whole} {whole|бутылка|бутылки|бутылок})",
        ["water.noWater"] = "Нет воды: подведи выход цистерны к его входу",
        ["water.tanksFull"] = "Баки на его выходе полны",
        ["water.noPower"] = "Нет энергии",
        ["water.off"] = "Выключен",
        ["water.cistern"] = "Вода · {held}/{of} л · {whole} {whole|бутылка|бутылки|бутылок}",
        ["water.splitting"] = "Разлагает {n} л/мин воды в кислород",
    };
}
