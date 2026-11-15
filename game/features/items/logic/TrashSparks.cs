using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Items;

/// <summary>One spark of the trash's burst: its direction, how far it flies past the ring, how long it lives, how far it sags.</summary>
public readonly record struct Spark(float Angle, float Reach, float LifeMs, float Drop);

/// <summary>
/// The trash's burst (trash-effect A): as a stack it held is destroyed, its ring flashes and steel sparks fly out of it,
/// slow, sag and fade. The same for every item; reach and sag in shares of the slot's side, so it scales with the slot.
/// </summary>
public static class TrashSparks
{
    public const int Count = 14;
    /// <summary>How long the burst lasts, and the ring's flash at its start.</summary>
    public const float Ms = 500, FlashMs = 160;
    /// <summary>The glyph's ring as a share of the icon's side (<c>IconLooks.Ring</c>).</summary>
    public const float RingShare = 0.43f;
    /// <summary>How far behind its head a spark's trail starts, a share of its life.</summary>
    public const float Trail = 0.2f;

    /// <summary>A burst's sparks, spread round the ring, each a little off its share of the circle.</summary>
    public static IReadOnlyList<Spark> Make(Random random)
    {
        var sparks = new Spark[Count];
        for (int i = 0; i < Count; i++)
            sparks[i] = new Spark(
                2 * MathF.PI * (i + random.NextSingle() * 0.7f) / Count,
                0.3f + 0.3f * random.NextSingle(),
                300 + 200 * random.NextSingle(),
                0.06f + 0.1f * random.NextSingle());
        return sparks;
    }

    /// <summary>
    /// Where <paramref name="spark"/> is <paramref name="ms"/> into the burst from the centre of a slot of side
    /// <paramref name="side"/> and ring radius <paramref name="ring"/>, at its life's share <paramref name="share"/> (its
    /// trail's end at an earlier one): out from the ring, slowing, sagging under its weight.
    /// </summary>
    public static (float X, float Y) At(Spark spark, float share, float side, float ring)
    {
        float eased = 1 - (1 - share) * (1 - share);
        float d = ring + spark.Reach * side * eased;
        return (MathF.Cos(spark.Angle) * d, MathF.Sin(spark.Angle) * d + spark.Drop * side * share * share);
    }

    /// <summary>The share of its life <paramref name="spark"/> has lived <paramref name="ms"/> in; null once it is out.</summary>
    public static float? Lived(Spark spark, float ms) => ms < 0 || ms >= spark.LifeMs ? null : ms / spark.LifeMs;

    /// <summary>How bright the ring's flash is <paramref name="ms"/> in: full at once, gone by <see cref="FlashMs"/>.</summary>
    public static float Flash(float ms) => ms < 0 || ms >= FlashMs ? 0 : 1 - ms / FlashMs;
}
