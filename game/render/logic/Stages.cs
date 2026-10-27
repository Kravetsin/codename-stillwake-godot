using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// The effects of one draw layer (effects-on-their-layer): its own luminous particles, flares, arcs and hits, each set with
/// its caps, drawn at that layer by one effects view. A world stage is a layer's z (<see cref="Stages.Of"/>); a held stage
/// is a hero figure's, between the tool and its front arm (<see cref="Stages.Held"/>).
/// </summary>
public sealed class Stage
{
    /// <summary>Its layer's z, or null for a figure's held stage, which draws at its figure's.</summary>
    public int? Z { get; }

    public Glims Glims { get; } = new(Glims.Limit);
    public Flares Flares { get; } = new();
    public Bolts Bolts { get; } = new();
    public HitBurst Hits { get; }

    internal Stage(int? z)
    {
        Z = z;
        Hits = new HitBurst(Glims, Flares);
    }

    /// <summary>Whether nothing of it lives: no particle, flare or arc, so its view has nothing to draw.</summary>
    public bool Empty => Glims.Live.Count == 0 && Bolts.Live.Count == 0 && Flares.Empty;

    /// <summary>Every particle, flare and arc of it gone.</summary>
    public void Clear()
    {
        Glims.Clear();
        Flares.Clear();
        Bolts.Clear();
    }
}

/// <summary>
/// Every stage of effects (effects-on-their-layer): the world's, one a z made on first use, and the figures' held stages
/// while their figures live. Drawn only, each peer its own.
/// </summary>
public static class Stages
{
    private static readonly SortedList<int, Stage> World = [];
    private static readonly List<Stage> HeldStages = [];
    private static readonly List<Stage> AllStages = [];

    /// <summary>The world's stage at layer <paramref name="z"/> (a <c>Layers</c> value), made on first use; names sharing a z share it.</summary>
    public static Stage Of(int z)
    {
        if (World.TryGetValue(z, out var stage)) return stage;
        World.Add(z, stage = new Stage(z));
        Order();
        return stage;
    }

    /// <summary>The world's stages, in z order: what the effects layer gives a view each.</summary>
    public static IList<Stage> WorldStages => World.Values;

    /// <summary>Every stage, the world's in z order then the held ones as they were made: what the light lights from.</summary>
    public static IReadOnlyList<Stage> All => AllStages;

    /// <summary>A new held stage for a figure, listed till it is dropped.</summary>
    public static Stage Held()
    {
        var stage = new Stage(null);
        HeldStages.Add(stage);
        Order();
        return stage;
    }

    /// <summary>A figure's held stage let go of: its effects with it.</summary>
    public static void Drop(Stage stage)
    {
        if (HeldStages.Remove(stage)) Order();
    }

    /// <summary>Every stage's effects gone, as a world is left or loaded.</summary>
    public static void Clear()
    {
        foreach (var stage in AllStages) stage.Clear();
    }

    /// <summary>Every stage forgotten, as the scene that drew them is freed.</summary>
    public static void Reset()
    {
        World.Clear();
        HeldStages.Clear();
        AllStages.Clear();
    }

    private static void Order()
    {
        AllStages.Clear();
        AllStages.AddRange(World.Values);
        AllStages.AddRange(HeldStages);
    }

    /// <summary>The particles flying on every stage: what a recording checks.</summary>
    public static int Count
    {
        get
        {
            int n = 0;
            foreach (var stage in AllStages) n += stage.Glims.Live.Count;
            return n;
        }
    }
}
