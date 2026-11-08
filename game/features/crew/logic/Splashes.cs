using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a hero's feet in water set off, frame by frame (liquids): a splash going in and one coming out, at most one every
/// <c>Sound.SplashGapS</c>, so a foot at the surface does not chatter; and a stroke every <c>Sound.StrokeEveryS</c> while it
/// wades or swims and moves. The first frame splashes nothing: a hero loaded in water was not seen going in.
/// </summary>
public sealed class Splashes
{
    public readonly record struct Cues(bool In, bool Out, bool Stroke);

    private bool? _wet;
    private double _sinceSplash = double.PositiveInfinity, _sinceStroke;

    /// <summary>Takes whether the hero is <paramref name="wet"/> and <paramref name="moving"/>, <paramref name="elapsedS"/> after the last frame.</summary>
    public Cues Step(bool wet, bool moving, double elapsedS)
    {
        _sinceSplash += elapsedS;
        bool turned = _wet is bool was && was != wet;
        _wet = wet;
        bool splash = turned && _sinceSplash >= S.SplashGapS;
        if (splash) _sinceSplash = 0;
        bool stroke = false;
        if (wet && moving)
        {
            _sinceStroke += elapsedS;
            if (_sinceStroke >= S.StrokeEveryS)
            {
                _sinceStroke = 0;
                stroke = !splash;
            }
        }
        else _sinceStroke = S.StrokeEveryS / 2;
        return new Cues(splash && wet, splash && !wet, stroke);
    }
}
