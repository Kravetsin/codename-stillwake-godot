using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The thread icon each of the drone's forms is shown by (drone-lessons): the lamp's and the scan's from the pick «Одна нить»; the alarms', a
/// warning sign drawn here by hand until the thoughts' icons are drawn with the rest in anime-art <c>subjects/drone.js</c>.
/// </summary>
public static class FormIcons
{
    /// <summary>The alarms: a triangle with a mark in it, a stand-in.</summary>
    public static readonly IconLines Alarms = new(
        [
            [0f, -0.86f, 0.9f, 0.72f, -0.9f, 0.72f, 0f, -0.86f],
            [0f, -0.34f, 0f, 0.22f],
            [0f, 0.42f, 0f, 0.48f],
        ],
        [0f, -0.86f, 0.9f, 0.72f, -0.9f, 0.72f, 0f, -0.86f, 0f, -0.34f, 0f, 0.22f, 0f, 0.42f, 0f, 0.48f],
        [4, 6]);

    /// <summary>The icon of <paramref name="form"/>; null for what is no form.</summary>
    public static IconLines? Of(string form) =>
        form == Stillwake.Content.LightDrone.ScannerMode ? IconInk.Scan
        : form == Stillwake.Content.LightDrone.LampMode ? IconInk.Lamp
        : form == Stillwake.Content.LightDrone.AlarmsMode ? Alarms
        : null;
}
