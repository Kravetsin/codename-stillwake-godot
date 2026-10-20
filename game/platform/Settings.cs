using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Platform;

/// <summary>
/// This machine's player's settings, kept in Godot's user folder as the profile is (0032): for now the slots'
/// size (0058) and whether heat shimmers (0075). Read once at the start; a change in the menu is written at once. A file that cannot be read
/// gives the defaults, one that cannot be written keeps the setting for this run alone.
/// </summary>
public static class Settings
{
    private const string File = "user://settings.cfg";

    /// <summary>The slots' size kept, <see cref="SlotScale.Normal"/> on a first run.</summary>
    public static SlotScale SlotScale()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return Slots.ScaleNamed(kept.GetValue("ui", "slots", "").AsString());
    }

    /// <summary>Whether heat shimmers (0075): on unless switched off.</summary>
    public static bool Shimmer()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return kept.GetValue("effects", "shimmer", true).AsBool();
    }

    /// <summary>Keeps whether heat shimmers for the next run.</summary>
    public static void KeepShimmer(bool on)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("effects", "shimmer", on);
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the shimmer in {File}: it holds for this run alone");
    }

    /// <summary>Keeps <paramref name="scale"/> for the next run.</summary>
    public static void Keep(SlotScale scale)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("ui", "slots", Slots.NameOf(scale));
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the slots' size in {File}: it holds for this run alone");
    }
}
