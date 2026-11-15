using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// A weapon effect skin's look (skins): an effect of its own that replaces the swing's haze and lives on the blade at rest, the
/// user's picks «Пламя» and «Разлом» (effect-skins). Its trail's stretches are seen <see cref="LifeMs"/>; lengths are world px.
/// </summary>
public abstract record EffectLook(double LifeMs);

/// <summary>
/// «Пламя»: low tongues lick up off the blade's lip and embers rise; a swing leaves a ribbon of fire that rises as it ages,
/// thinning away in places, its youngest part hotter, and drops cinders that fall and die.
/// </summary>
public sealed record FireLook() : EffectLook(210)
{
    public (int R, int G, int B) Heart { get; init; } = (242, 184, 112);
    public (int R, int G, int B) Flame { get; init; } = (206, 104, 44);
    public (int R, int G, int B) Deep { get; init; } = (120, 40, 18);
    public (int R, int G, int B) Ember { get; init; } = (236, 140, 64);

    /// <summary>The tongues at rest: how many along the lip, their height, width at the root, beat (Hz) and opacity.</summary>
    public int Tongues { get; init; } = 6;
    public (double From, double To) TonguePx { get; init; } = (1.1, 2.7);
    public double TongueWidePx { get; init; } = 1.1;
    public (double From, double To) TongueHz { get; init; } = (3, 6);
    public double TongueAlpha { get; init; } = 0.42;

    /// <summary>Embers rising at rest: a second, their life (ms) and speed up (px a second).</summary>
    public double RestEmbersPerS { get; init; } = 6;
    public (double From, double To) EmberMs { get; init; } = (500, 1100);
    public (double From, double To) EmberRise { get; init; } = (5, 11);

    /// <summary>The swing's ribbon: its opacity, how far it rises over its life, how much of it has thinned away by its end.</summary>
    public double RibbonAlpha { get; init; } = 0.55;
    public double RibbonRisePx { get; init; } = 2.2;
    public double Tear { get; init; } = 0.7;

    /// <summary>How far apart, in frames of the swing, the ribbon's thin places are (one each 1 / this), and how soft its sides are, px.</summary>
    public double TearScale { get; init; } = 0.3;
    public double RibbonSoftPx { get; init; } = 0.6;

    /// <summary>Cinders off the swing: per px swept, their life (ms), the share of the edge's speed they keep, their fall (px/s²) and drag a 60 Hz frame.</summary>
    public double CindersPerPx { get; init; } = 0.35;
    public (double From, double To) CinderMs { get; init; } = (350, 800);
    public double CinderCarry { get; init; } = 0.12;
    public double CinderFall { get; init; } = 16;
    public double CinderDrag { get; init; } = 0.95;
}

/// <summary>
/// «Разлом»: a thin dark seam breathes along the edge, its rim faint; a swing tears space open along its path, a void with bright
/// rims and far specks in it, closing toward the edge's middle as it ages, its tail first.
/// </summary>
public sealed record RiftLook() : EffectLook(320)
{
    public (int R, int G, int B) Void { get; init; } = (7, 5, 13);
    public (int R, int G, int B) Rim { get; init; } = (200, 188, 224);
    public (int R, int G, int B) Specks { get; init; } = (190, 196, 220);

    /// <summary>The tear: the share of its life it stays open, how its opening follows the swing's speed, its rim's width and opacity, the void's opacity.</summary>
    public double OpenShare { get; init; } = 0.35;
    public double ShownToOpen { get; init; } = 1.6;
    public double RimPx { get; init; } = 0.4;
    public double RimAlpha { get; init; } = 0.8;
    public double VoidAlpha { get; init; } = 0.94;

    /// <summary>Far specks in the tear: the share of the world's 3 px cells that hold one.</summary>
    public double SpecksPerCell { get; init; } = 0.12;

    /// <summary>The seam at rest: its width breathing between these, its rim's opacity with it, one breath in this many ms.</summary>
    public (double From, double To) SeamPx { get; init; } = (0.45, 0.85);
    public (double From, double To) SeamRim { get; init; } = (0.1, 0.3);
    public double BreathMs { get; init; } = 1900;
}

/// <summary>The effect skins' looks by skin id (skins): every effect row of the catalogue has one.</summary>
public static class SkinEffects
{
    public const string Fire = "fire", Rift = "rift";

    public static readonly IReadOnlyDictionary<string, EffectLook> On = new Dictionary<string, EffectLook>
    {
        [Fire] = new FireLook(),
        [Rift] = new RiftLook(),
    };

    /// <summary>The look of effect skin <paramref name="id"/>; none for none or an id without a row.</summary>
    public static EffectLook? Of(string? id) => id != null && On.TryGetValue(id, out var look) ? look : null;
}
