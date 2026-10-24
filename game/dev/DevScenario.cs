using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A scenario's argument (<see cref="DevScenario"/>): its name, what it does, and how it is written. A class may carry
/// several, for flags that share its code; it is active when any of them is given.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DevAttribute(string name, string doc) : Attribute
{
    public string Name { get; } = name;
    public string Doc { get; } = doc;
    public DevShape Shape { get; init; } = DevShape.Flag;
    /// <summary>Where only one scenario's map, world, script or start can be had, the lowest wins; the rest run in this order.</summary>
    public int Order { get; init; }
}

/// <summary>
/// One dev scenario (docs/plans/dev-scenarios.md): what a run started with its <see cref="DevAttribute"/> plays, each hook
/// overridden where it differs from a plain run. Found by <see cref="DevScenarios"/>; Main names none.
/// </summary>
public abstract class DevScenario
{
    /// <summary>The arguments the game was started with, for the scenario's own and its modifiers.</summary>
    public DevArgs Args { get; internal set; } = null!;

    /// <summary>The map played and the camera aimed at; the wreck as drawn with none.</summary>
    public virtual Layout? Map => null;

    /// <summary>The world played alone, in place of one made from <see cref="Map"/>.</summary>
    public virtual World? Create(IReadOnlyList<Module> modules) => null;

    /// <summary>The world with what the scenario needs put in it, through its save.</summary>
    public virtual World Seed(World world, DevSeed seed) => world;

    /// <summary>Whether <see cref="Seed"/> is played hosting too, so a guest sees it.</summary>
    public virtual bool Hosted => false;

    /// <summary>The script the hero is played by.</summary>
    public virtual HeroAutopilot? Script => null;

    /// <summary>The cell the hero starts on.</summary>
    public virtual Func<Registry, int>? Start => null;

    /// <summary>Its nodes, made once Main has made everything else; the order they are added in is their draw order.</summary>
    public virtual void Attach(DevContext c) { }

    /// <summary>The commands sent as the session starts.</summary>
    public virtual void Started(Client client) { }

    /// <summary>Each frame, before the hands: scripts that send commands or work the hands.</summary>
    public virtual void Frame(Client client, Registry m, Action<string> log) { }
}

/// <summary>What a seed may read: the modules a world is loaded with, and the registry playing now.</summary>
public sealed record DevSeed(Func<IReadOnlyList<Module>> Modules, Registry M);

/// <summary>What Main has made, for a scenario's nodes; null where a run made none (no items with <c>--perf</c>).</summary>
public sealed class DevContext
{
    public required Ui Ui { get; init; }
    public required Hands Hands { get; init; }
    public required DebrisView Debris { get; init; }
    public ItemsView? Items { get; init; }
    public FabricatorPanel? Fabricator { get; init; }
    /// <summary>The world played now, once a session runs.</summary>
    public required Func<(Client Client, Registry M)?> World { get; init; }
    /// <summary>The registry played now.</summary>
    public required Func<Registry> M { get; init; }
    public required Action<Node> Add { get; init; }
}
