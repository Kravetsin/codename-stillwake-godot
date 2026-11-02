namespace Stillwake.Game.Features.Pipes;

/// <summary>How the pipe's run is laid along a span: its picture has its lit edge on top, and the world's light comes from the upper left.</summary>
public static class TubeRun
{
    /// <summary>
    /// Whether a span from (<paramref name="ax"/>, <paramref name="ay"/>) to (<paramref name="bx"/>, <paramref name="by"/>) is drawn
    /// from its far end, so its run's lit edge faces the light whichever way it was laid.
    /// </summary>
    public static bool LitFromEnd(float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        // The run's top edge, its direction turned a quarter left, against the light's (-0.6, -0.8).
        return dy * -0.6f - dx * -0.8f < 0;
    }
}
