using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Players;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --looks</c>: a look chosen and printed (2026-10-28-looks-are-printed), for recording. The hero comes in bare as
/// <see cref="DieDemo"/>'s, in look 0, its lungs low in the torn room open to space; the look editor opens, each picker is
/// moved on, and it closes, sending the look. The hero is unchanged till it passes out; it wakes at the pod in the new
/// look, its body lying where it fell.
/// </summary>
public partial class LooksDemo(LookEditor editor) : Node
{
    /// <summary>When each step goes, in seconds: the editor opened, each picker moved on, the editor closed.</summary>
    private static readonly (double At, int Picker, int By)[] Picks = [(1.2, 0, 3), (1.6, 1, 2), (2.0, 2, 2), (2.4, 3, -1), (2.8, 4, 2), (3.2, 4, -1)];
    private const double OpenAt = 0.6, CloseAt = 4.4;

    private double _t;
    private int _next;
    private bool _opened, _closed;

    /// <summary><see cref="DieDemo"/>'s world, the hero bare with 12 units of air: it passes out some six seconds in.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(DieDemo.Seed(world, modules).Save())!;
        var slots = root["slices"]!["items"]!["inventories"]![0]!["slots"]!.AsArray();
        foreach (var kind in new[] { Spec.Helmet.Slot, Spec.Suit.Slot })
            slots[Spec.Inventory.Slots + ((List<string>)[.. Spec.Inventory.Equipment]).IndexOf(kind)] = null;
        root["slices"]!["crew"]!["heroes"]![0]!["air"] = 12;
        return World.Load(root.ToJsonString(), modules());
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (!_opened && _t >= OpenAt)
        {
            editor.Open(Look.Zero);
            _opened = true;
        }
        while (_next < Picks.Length && Picks[_next].At <= _t) editor.Pick(Picks[_next].Picker, Picks[_next++].By);
        if (!_closed && _t >= CloseAt)
        {
            editor.Close();
            _closed = true;
        }
    }
}
