using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the worn light drone does (light-drone): shines, or scans with its light out.</summary>
public enum DroneMode { Lamp, Scan }

/// <summary>
/// The light drone's modes (light-drone): the client's, never a command and never saved, a new game or a reload on the lamp; sent with the
/// hero so friends see its beam. Held, the drone key opens their wheel (<see cref="Wheel{T}"/>); a tap goes on to the next open one. A mode
/// past the lamp opens with its research node, known crew-wide.
/// </summary>
public static class DroneModes
{
    /// <summary>The wheel's sides, from the top clockwise.</summary>
    public static readonly DroneMode[] Sides = [DroneMode.Lamp, DroneMode.Scan];

    /// <summary>The research node that opens <paramref name="mode"/>; none for the lamp, open from the start.</summary>
    public static string? NodeOf(DroneMode mode) => mode switch
    {
        DroneMode.Scan => Stillwake.Content.LightDrone.ScannerMode,
        _ => null,
    };

    /// <summary>Whether <paramref name="mode"/> is open to a hero who <paramref name="knows"/> what it does.</summary>
    public static bool Opened(DroneMode mode, Func<string, bool> knows) => NodeOf(mode) is not { } node || knows(node);

    /// <summary>The mode a tap goes on to from <paramref name="now"/>: the next open one round the wheel; none with no other open.</summary>
    public static DroneMode? Next(DroneMode now, Func<DroneMode, bool> opened)
    {
        int at = Array.IndexOf(Sides, now);
        for (int i = 1; i < Sides.Length; i++)
            if (Sides[(at + i) % Sides.Length] is var next && opened(next)) return next;
        return null;
    }

    /// <summary>What is sent of <paramref name="mode"/> with the hero: nothing for the lamp, so a hero sends as it did before the drone.</summary>
    public static DroneMode? Sent(DroneMode mode) => mode == DroneMode.Lamp ? null : mode;

    /// <summary>The figure's lamp and scanner for a hero who wears the drone or not, in <paramref name="mode"/> (heard as none: the lamp).</summary>
    public static (bool Lamp, bool Scanner) Shown(bool worn, DroneMode? mode) =>
        (worn && (mode ?? DroneMode.Lamp) == DroneMode.Lamp, worn && mode == DroneMode.Scan);
}
