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
        ["air.pipes"] = "Жидкий O₂ в баках: {oxygen}",
        ["air.lastsNot"] = "Нет подачи · хватит на {time}",
        ["air.keepsUp"] = "Испаритель справляется · +{n}/мин · баков хватит на {time}",
        ["air.fallsShort"] = "Испаритель не тянет · хватит на {time}",
        ["air.drains"] = "Жидкий O₂ в баках: {oxygen} · кончится через {time}",
        ["atmosphere.noVent"] = "Здесь нет вентиляции",
        ["atmosphere.notPiped"] = "Вентиляция не подключена к испарителю",
        ["atmosphere.unpowered"] = "У испарителя нет энергии",
        ["atmosphere.dry"] = "Бак за испарителем пуст: вставь баллон",
        ["air.tank"] = "Жидкий O₂ · {held}/{of}",
        ["air.makes"] = "Делает {n} O₂ в секунду",
    };
}
