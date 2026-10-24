using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stillwake.Game.Dev;

/// <summary>Every <see cref="DevScenario"/> in the game, found by its <see cref="DevAttribute"/>: none in a release build.</summary>
public static class DevScenarios
{
    private static readonly (Type Type, DevAttribute[] Devs)[] Found = typeof(DevScenario).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(DevScenario)))
        .Select(t => (t, t.GetCustomAttributes<DevAttribute>(inherit: false).ToArray()))
        .Where(f => f.Item2.Length > 0)
        .OrderBy(f => f.Item2.Min(d => d.Order)).ThenBy(f => f.Item2[0].Name, StringComparer.Ordinal)
        .ToArray();

    /// <summary>The scenarios' arguments, for <see cref="DevArgs"/>.</summary>
    public static readonly IReadOnlyList<DevMode> Modes = [.. Found.SelectMany(f => f.Devs).Select(d => new DevMode(d.Name, d.Shape, d.Doc))];

    /// <summary>The scenarios <paramref name="args"/> start, in their order.</summary>
    public static List<DevScenario> Active(DevArgs args) =>
    [
        .. Found.Where(f => f.Devs.Any(d => args.Gives(new DevMode(d.Name, d.Shape, d.Doc))))
            .Select(f =>
            {
                var scenario = (DevScenario)Activator.CreateInstance(f.Type)!;
                scenario.Args = args;
                return scenario;
            }),
    ];
}
