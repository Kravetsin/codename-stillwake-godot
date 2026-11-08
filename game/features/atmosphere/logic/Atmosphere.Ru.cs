namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Atmosphere = new()
    {
        ["atmosphere.noTank"] = "Здесь нет бака",
        ["atmosphere.tankFull"] = "Бак полон",
        ["atmosphere.tankLow"] = "В баке меньше баллона",
        ["tank.draw"] = "Достать {n} ({held}/{all} л)",
        ["air.aired"] = "Есть воздух · {cells} {cells|клетка|клетки|клеток}",
        ["air.filling"] = "Наполняется · {aired} из {cells} {cells|клетки|клеток|клеток}",
        ["air.venting"] = "Воздух уходит в космос",
        ["air.vacuum"] = "Нет воздуха · {cells} {cells|клетка|клетки|клеток}",
        ["air.holding"] = "Мало воздуха · {aired} из {cells} {cells|клетки|клеток|клеток}",
        ["air.made"] = "+{n} л/мин",
        ["air.leak"] = "−{n} л/мин",
        ["air.liquid"] = "{held} л · {time}",
        ["air.litres"] = "{n} л",
        ["atmosphere.noVent"] = "Здесь нет вентиляции",
        ["atmosphere.notPiped"] = "Вентиляция не подключена к испарителю",
        ["atmosphere.unpowered"] = "У испарителя нет энергии",
        ["atmosphere.off"] = "Испаритель выключен",
        ["atmosphere.dry"] = "Бак за испарителем пуст: вставь баллон",
        ["atmosphere.leaky"] = "Стены травят больше, чем дают испарители: построй стены получше или ещё испаритель",
        ["atmosphere.burning"] = "Огонь здесь забирает свою долю воздуха: поставь ещё испаритель или стены получше",
        ["air.tank"] = "{held}/{of} л",
    };
}
