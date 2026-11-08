namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Power = new()
    {
        ["power.noNode"] = "Не к чему крепить провод",
        ["power.noSwitch"] = "У него нет выключателя",
        ["switch.on"] = "Вкл",
        ["switch.off"] = "Выкл",
        ["power.sameNode"] = "Оба конца в одном месте",
        ["power.tooLong"] = "Слишком длинно: добавь опору",
        ["power.taut"] = "Провод здесь натянется",
        ["power.twice"] = "Провод уже есть",
        ["power.notAboard"] = "Провода только на борту",
        ["power.noCable"] = "Здесь нет провода",
        ["power.supply"] = "Энергия: выработка {supply} / расход {demand}",
        ["power.stored"] = "Запас: {stored} / {capacity}",
    };
}
