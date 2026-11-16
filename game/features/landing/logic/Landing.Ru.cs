namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Landing = new()
    {
        ["landing.noPlatform"] = "Здесь нет платформы",
        ["landing.unpowered"] = "Нет энергии: платформа закрыта",
        ["landing.noPod"] = "Здесь нет посадочной капсулы",
        ["landing.taken"] = "В ней уже кто-то есть",
        ["landing.seated"] = "Ты уже в капсуле",
        ["landing.notSeated"] = "Ты не в капсуле",
        ["landing.noFuel"] = "Нужно {fuel} л биотоплива в баке",
        ["landing.notYours"] = "Не твоя капсула",
        ["landing.cannotLeave"] = "Садиться некуда",
        ["landing.shaftBlocked"] = "Под платформой что-то построено",
        ["landing.descend"] = "Спуск",
        ["landing.return"] = "Возврат",
        ["landing.noEngine"] = "Здесь нет двигателя",
        ["landing.engineUnpowered"] = "Нет энергии: двигатель холодный",
        ["landing.noExhaust"] = "Сопло замуровано",
        ["landing.crewAway"] = "Не все на борту: нажатие зовёт их домой",
        ["landing.hereAlready"] = "Корабль уже там",
        ["landing.inFlight"] = "Корабль в полёте",
        ["landing.setOff"] = "Отправиться",
        ["landing.noRoute"] = "Нет маршрута",
        ["landing.course"] = "{n} км · {l} л",
        ["landing.home"] = "Домой",
    };
}
