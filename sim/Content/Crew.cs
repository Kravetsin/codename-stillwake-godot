namespace Stillwake.Content;

/// <summary>
/// A hero's breathing, in oxygen units and ticks (20 a second). Every <see cref="BreathEvery"/> ticks a
/// hero takes a breath of one unit, from an aired room's air or else from the suit, which holds
/// <see cref="Suit"/> units; in an aired room the suit also refills by up to <see cref="Refill"/> units a
/// breath. An empty suit passes the hero out for <see cref="PassOut"/> ticks. Heroes wear one of
/// <see cref="Colours"/> suit colours, each taken by one hero at most while there are colours to go round.
/// Running or swimming fast, a hero is out of breath and breathes <see cref="Exerted"/> units a breath:
/// stamina is the air it costs.
/// </summary>
public static class Crew
{
    public const int Suit = 45;
    public const int BreathEvery = 20;
    public const int Refill = 3;
    public const int PassOut = 100;
    public const int Colours = 4;
    public const int Exerted = 2;
}
