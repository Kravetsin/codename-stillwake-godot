using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Platform;

namespace Stillwake.Game.Menu;

/// <summary>
/// The world picked in the Worlds panel: any world may be, so one this build cannot play can still be
/// deleted; only a playable one is played or hosted.
/// </summary>
public static class WorldPick
{
    /// <summary>The pick once the list is read afresh: the one before while it is listed, else the first playable, else none.</summary>
    public static string? After(IReadOnlyList<WorldEntry> worlds, string? picked) =>
        worlds.Any(w => w.Id == picked) ? picked : worlds.FirstOrDefault(w => w.Status == WorldStatus.Playable)?.Id;

    /// <summary>The picked world when it can be played, else null.</summary>
    public static string? Playable(IReadOnlyList<WorldEntry> worlds, string? picked) =>
        worlds.FirstOrDefault(w => w.Id == picked && w.Status == WorldStatus.Playable)?.Id;
}
