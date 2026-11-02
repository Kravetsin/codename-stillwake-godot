namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Atmosphere = new()
    {
        ["atmosphere.noTank"] = "Здесь нет бака",
        ["atmosphere.tankFull"] = "Бак полон",
        ["air.aired"] = "Есть воздух · {cells} {cells|клетка|клетки|клеток}",
        ["air.filling"] = "Наполняется · {aired} из {cells} {cells|клетки|клеток|клеток}",
        ["air.venting"] = "Воздух уходит в космос",
        ["air.vacuum"] = "Нет воздуха · {cells} {cells|клетка|клетки|клеток}",
        ["air.holding"] = "Мало воздуха · {aired} из {cells} {cells|клетки|клеток|клеток}",
        ["air.pipes"] = "O₂ в трубах: {oxygen}",
        ["air.lasts"] = "O₂ в трубах: {oxygen} · хватит на {time}",
        ["air.lastsNot"] = "Нет подачи · хватит на {time}",
        ["air.keepsUp"] = "Генератор справляется · +{n}/мин",
        ["air.fallsShort"] = "Генератор не тянет · хватит на {time}",
        ["air.drains"] = "O₂ в трубах: {oxygen} · кончится через {time}",
        ["atmosphere.noVent"] = "Здесь нет вентиляции",
        ["atmosphere.notPiped"] = "Вентиляция не подключена к баку или генератору",
        ["atmosphere.unpowered"] = "У генератора нет энергии",
        ["atmosphere.dry"] = "Трубы пусты: вставь баллон в бак",
    };
}
