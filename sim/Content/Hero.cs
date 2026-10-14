namespace Stillwake.Content;

/// <summary>
/// How the hero moves. Lengths are in thousandths of a cell, speeds in thousandths of a cell per
/// controller step, and the controller steps 60 times a second. Terraria's proportions (ADR 0005): the
/// hero is narrower than a cell and under three tall, so a doorway three cells tall and a shaft one cell
/// wide let them through, and a jump clears three cells but not four. <see cref="Reach"/> is how far from
/// the middle of the body the middle of a cell may be for the hero to act on it. <see cref="Swim"/> is the
/// speed along each axis with the body in space, where there is no gravity and no inertia.
/// <see cref="Tether"/> is how far the middle of the body may get from where the tether is fixed, under
/// the wreck's 16 cells of space round the hull, so a hero never reaches the grid's edge (0007).
/// </summary>
public static class Hero
{
    public const int Width = 950;
    public const int Height = 2625;
    public const int Walk = 240;
    public const int Climb = 180;
    public const int Jump = 420;
    public const int Gravity = 21;
    public const int MaxFall = 1200;
    public const int Reach = 6000;
    public const int Tether = 12000;
    public const int Swim = 120;
}
