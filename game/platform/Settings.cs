using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Platform;

/// <summary>
/// This machine's player's settings, kept in Godot's user folder as the profile is (0032): the language
/// (2026-11-02-russian-language), the slots' size (0058), whether heat shimmers (0075), the volumes (settings-menu), how the window takes the screen and the hero's look (2026-10-28-looks-are-printed). Read once at the start; a change in the menu is written at once. A file that cannot be read
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

    /// <summary>How the window takes the screen: in a window unless chosen otherwise.</summary>
    public static ScreenMode Screen()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return Screens.Named(kept.GetValue("video", "screen", "").AsString());
    }

    /// <summary>Keeps <paramref name="mode"/> for the next run.</summary>
    public static void Keep(ScreenMode mode)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("video", "screen", Screens.NameOf(mode));
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the screen mode in {File}: it holds for this run alone");
    }

    /// <summary>The mode the window is in now: a dev run keeps the window its command line made.</summary>
    public static ScreenMode Shown() => DisplayServer.WindowGetMode() switch
    {
        DisplayServer.WindowMode.ExclusiveFullscreen => ScreenMode.Fullscreen,
        DisplayServer.WindowMode.Fullscreen => ScreenMode.Borderless,
        _ => ScreenMode.Windowed,
    };

    /// <summary>Puts the window in <paramref name="mode"/>; a frameless one is Godot's full screen, which leaves other windows a click away.</summary>
    public static void Apply(ScreenMode mode) => DisplayServer.WindowSetMode(mode switch
    {
        ScreenMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
        ScreenMode.Borderless => DisplayServer.WindowMode.Fullscreen,
        _ => DisplayServer.WindowMode.Windowed,
    });

    /// <summary>The keys kept (2026-11-04-key-binds): the defaults, with what the player changed put on them.</summary>
    public static Binds Binds()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        if (!kept.HasSection("binds")) return Shared.Binds.Default;
        return Shared.Binds.Read(kept.GetSectionKeys("binds").Select(id => (id, kept.GetValue("binds", id, "").AsString())).ToList());
    }

    /// <summary>Keeps <paramref name="binds"/> for the next run: only the keys that differ from the defaults.</summary>
    public static void Keep(Binds binds)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        if (kept.HasSection("binds")) kept.EraseSection("binds");
        foreach (var (id, key) in binds.Changed()) kept.SetValue("binds", id, key);
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the keys in {File}: they hold for this run alone");
    }

    /// <summary>The items marked to stay in the bag (2026-11-06-quick-stash), none on a first run.</summary>
    public static Favourites Favourites()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return Shared.Favourites.Read(kept.GetValue("favourites", "items", "").AsString());
    }

    /// <summary>Keeps <paramref name="favourites"/> for the next run.</summary>
    public static void Keep(Favourites favourites)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("favourites", "items", favourites.Written());
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the favourites in {File}: they hold for this run alone");
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

    /// <summary>The step <paramref name="bus"/>'s slider was left at (settings-menu), the top one, today's mix, on a first run.</summary>
    public static int Volume(string bus)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        return Menu.Volume.Step(kept.GetValue("sound", bus.ToLowerInvariant(), Sound.VolumeSteps).AsInt32());
    }

    /// <summary>Keeps <paramref name="bus"/>'s <paramref name="step"/> for the next run.</summary>
    public static void KeepVolume(string bus, int step)
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("sound", bus.ToLowerInvariant(), Menu.Volume.Step(step));
        if (kept.Save(File) != Error.Ok) GD.PrintErr($"[settings] cannot keep the volume in {File}: it holds for this run alone");
    }

    /// <summary>Sets <paramref name="bus"/> to <paramref name="step"/>: its gain, muted at 0.</summary>
    public static void ApplyVolume(string bus, int step)
    {
        int index = AudioServer.GetBusIndex(bus);
        AudioServer.SetBusVolumeDb(index, Menu.Volume.Db(step));
        AudioServer.SetBusMute(index, Menu.Volume.Muted(step));
    }

    /// <summary>Every slider's bus as kept, at the start before anything plays.</summary>
    public static void ApplyVolumes()
    {
        foreach (var bus in Sound.Volumed) ApplyVolume(bus, Volume(bus));
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
