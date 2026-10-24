using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Dev;

/// <summary>
/// The arguments after <c>--</c> the game was started with, read by name; every name is one of <see cref="DevModes.All"/> or of
/// <paramref name="scenarios"/> (the scenarios' own, <c>DevScenarios.Modes</c>), so a name read but never registered is our bug and throws.
/// </summary>
public sealed class DevArgs(string[] all, IReadOnlyList<DevMode>? scenarios = null)
{
    private readonly IReadOnlyList<DevMode> _scenarios = scenarios ?? [];

    /// <summary>Every argument as given.</summary>
    public string[] All { get; } = all;

    /// <summary>Whether none was given: the title, rather than a session of its own.</summary>
    public bool None => All.Length == 0;

    /// <summary>Whether <paramref name="token"/> was given exactly: <c>--testbed</c>, or a value with it, <c>--lone-hatch=open</c>.</summary>
    public bool Has(string token)
    {
        Registered(token);
        return All.Contains(token);
    }

    /// <summary>The argument after <paramref name="name"/> (<see cref="DevShape.Next"/>): <c>--zoom 3</c>'s <c>3</c>; null without it.</summary>
    public string? Arg(string name)
    {
        Registered(name);
        return All.SkipWhile(a => a != name).Skip(1).FirstOrDefault();
    }

    /// <summary>What follows <c>name=</c> in the first argument that starts so (<see cref="DevShape.Equals"/>); null without one.</summary>
    public string? Value(string name)
    {
        Registered(name);
        return All.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..];
    }

    /// <summary>
    /// <paramref name="name"/> given bare or with a value (<see cref="DevShape.Either"/>): "" bare, the value after <c>=</c>
    /// with one, null not given.
    /// </summary>
    public string? Given(string name)
    {
        Registered(name);
        var token = All.FirstOrDefault(a => a == name || a.StartsWith(name + "=", StringComparison.Ordinal));
        return token == null ? null : token.Length > name.Length ? token[(name.Length + 1)..] : "";
    }

    /// <summary>Whether <paramref name="mode"/> was given in its shape: bare, with a value, or both, as it is written.</summary>
    public bool Gives(DevMode mode) => mode.Shape switch
    {
        DevShape.Flag => Has(mode.Name),
        DevShape.Next => All.Contains(mode.Name),
        DevShape.Equals => Value(mode.Name) != null,
        _ => Given(mode.Name) != null,
    };

    /// <summary>The arguments that are none of <see cref="DevModes.All"/> or <paramref name="scenarios"/>, or given in another shape than theirs.</summary>
    public static List<string> Unknown(IReadOnlyList<string> args, IReadOnlyList<DevMode>? scenarios = null)
    {
        var unknown = new List<string>();
        for (int i = 0; i < args.Count; i++)
        {
            string token = args[i];
            int equals = token.IndexOf('=');
            string name = equals < 0 ? token : token[..equals];
            var mode = Find(name, scenarios ?? []);
            bool fits = mode?.Shape switch
            {
                DevShape.Flag => equals < 0,
                DevShape.Next => equals < 0 && i + 1 < args.Count,
                DevShape.Equals => equals > 0,
                DevShape.Either => true,
                _ => false,
            };
            if (!fits) unknown.Add(token);
            else if (mode!.Shape == DevShape.Next) i++;
        }
        return unknown;
    }

    private static DevMode? Find(string name, IReadOnlyList<DevMode> scenarios) =>
        DevModes.Find(name) ?? scenarios.FirstOrDefault(m => m.Name == name);

    private void Registered(string token)
    {
        int equals = token.IndexOf('=');
        string name = equals < 0 ? token : token[..equals];
        if (Find(name, _scenarios) == null) throw new InvalidOperationException($"{name} is not in DevModes.All or a scenario's [Dev]");
    }
}
