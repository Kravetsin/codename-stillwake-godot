namespace Stillwake.Game.Features.Light;

/// <summary>How much of its light a lamp's power gives it.</summary>
public static class LampPower
{
    /// <summary>
    /// The share of its light, 0 to 1, of a lamp drawing <paramref name="draws"/> at a sim's <paramref name="satisfaction"/>, out of
    /// 1000: full for one that draws nothing, on no network, a battery lamp (the-scoop).
    /// </summary>
    public static float ShareOf(int? draws, int satisfaction) => draws == null ? 1f : satisfaction / 1000f;
}
