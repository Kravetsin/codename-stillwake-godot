using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The thread icon each of the drone's forms is shown by (drone-lessons): the lamp's and the scan's from the pick «Одна нить»; the alarms', the
/// meteor the drone thinks when one comes (drone-thoughts, the pick «Контур»).
/// </summary>
public static class FormIcons
{
    /// <summary>The icon of <paramref name="form"/>; null for what is no form.</summary>
    public static IconLines? Of(string form) =>
        form == Stillwake.Content.LightDrone.ScannerMode ? IconInk.Scan
        : form == Stillwake.Content.LightDrone.LampMode ? IconInk.Lamp
        : form == Stillwake.Content.LightDrone.AlarmsMode ? IconInk.Alarms
        : null;
}
