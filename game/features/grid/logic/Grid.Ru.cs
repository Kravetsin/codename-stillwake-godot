namespace Stillwake.Game.I18n;

public static partial class Ru
{
    private static readonly Strings Grid = new()
    {
        ["grid.outOfBounds"] = "За пределами корабля",
        ["grid.outsideZone"] = "За пределами стройки",
        ["grid.occupied"] = "Здесь уже что-то есть",
        ["grid.machineInTheWay"] = "Мешает машина",
        ["grid.blocked"] = "Что-то мешает",
        ["grid.noSuchMaterial"] = "Нет такого материала",
        ["grid.noSuchMachine"] = "Нет такой машины",
        ["grid.noExhaust"] = "Сопло должно выходить сквозь обшивку в космос",
        ["grid.notOnHull"] = "Вешай снаружи, на правый борт",
        ["grid.noPlatform"] = "Ставится только на посадочную платформу",
        ["grid.noGround"] = "Нужна ровная земля",
        ["grid.underPlatform"] = "Не под посадочной платформой: здесь падает капсула",
        ["grid.shaftBlocked"] = "Под ней должно быть пусто до самого края",
        ["grid.noSuchLayer"] = "Нет такого слоя",
        ["grid.noDoor"] = "Здесь нет двери",
        ["grid.doorBusy"] = "Дверь ещё движется",
        ["hazards.noWall"] = "Здесь нет стены",
        ["hazards.neverBreaks"] = "Это не ломается",
        ["hazards.flareComing"] = "Вспышка уже на подходе",
        ["hazards.badTicks"] = "Вспышка бьёт не раньше чем через тик",
    };
}
