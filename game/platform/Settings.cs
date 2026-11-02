using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Platform;

/// <summary>
/// This machine's player's settings, kept in Godot's user folder as the profile is (0032): the language
/// (2026-11-02-russian-language), the slots' size (0058), whether heat shimmers (0075) and the hero's look (2026-10-28-looks-are-printed). Read once at the start; a change in the menu is written at once. A file that cannot be read
/// gives the defaults, one that cannot be written keeps the setting for this run alone.
/// </summary>
public static class Settings
{
    private const string File = "user://settings.cfg";

    /// <summary>The language kept; on a first run Russian on a Russian system, else English.</summary>
    public static Language Language()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return Languages.Named(kept.GetValue("ui", "language", "").AsString()) ?? Languages.OfSystem(OS.GetLocaleLanguage());
    }

    /// <summary>Keeps <paramref name="language"/> for the next run.</summary>
    public static void Keep(Language language)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("ui", "language", Languages.NameOf(language));
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the language in {File}: it holds for this run alone");
    }

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

    private static readonly string[] LookKeys = ["skin", "hair", "hairColour", "face", "beard", "outfit"];

    /// <summary>The look this player chose, kept from an earlier run; null before it ever chose one.</summary>
    public static Look? Look()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        if (!kept.HasSection("look")) return null;
        var i = LookKeys.Select(k => kept.GetValue("look", k, 0).AsInt32()).ToArray();
        return new Look(i[0], i[1], i[2], i[3], i[4], i[5]);
    }

    /// <summary>Keeps <paramref name="look"/> for the next run.</summary>
    public static void KeepLook(Look look)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        int[] i = [look.Skin, look.Hair, look.HairColour, look.Face, look.Beard, look.Outfit];
        for (int k = 0; k < LookKeys.Length; k++) kept.SetValue("look", LookKeys[k], i[k]);
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the look in {File}: it holds for this run alone");
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
