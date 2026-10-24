using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>
    /// A creature struck and killed (0102), by its species, whatever the tool, as a wall or a growth is (0093): placeholders from
    /// what is here till the user picks — a hit, the cap's squelch lower with a soft body thump under it; its death, a heavy
    /// landing's thud higher with the squelch. This player's own on Suit, a friend's on World, where it is struck.
    /// </summary>
    public static IReadOnlyDictionary<string, (Cue Hit, Cue Thump, Cue Death)> Creatures => FaunaDerived.Creatures;

    /// <summary>A creature winding up to ram snorts (0103), a placeholder from what is here till the audition: a canister's hiss low and short, where it stands.</summary>
    public static Cue Snort => FaunaDerived.Snort;

    /// <summary>The cues made from another file's, made when first read, after every static initializer has run.</summary>
    private static class FaunaDerived
    {
        public static readonly IReadOnlyDictionary<string, (Cue Hit, Cue Thump, Cue Death)> Creatures = new Dictionary<string, (Cue, Cue, Cue)>
        {
            [Stillwake.Content.Fauna.Loper.Id] = (
                Glowcap with { Tune = 0.8f, Pitch = 0.12f },
                LandSoft with { VolumeDb = -3, Tune = 1.35f, Pitch = 0.1f, Heard = ToolReach },
                LandHard with { VolumeDb = -2, Tune = 1.25f, Heard = BreakReach }),
            // The sixleg the loper's; the heavier tapir lower, the small fluff higher (placeholders all).
            [Stillwake.Content.Fauna.Sixleg.Id] = (
                Glowcap with { Tune = 0.8f, Pitch = 0.12f },
                LandSoft with { VolumeDb = -3, Tune = 1.35f, Pitch = 0.1f, Heard = ToolReach },
                LandHard with { VolumeDb = -2, Tune = 1.25f, Heard = BreakReach }),
            [Stillwake.Content.Fauna.Tapir.Id] = (
                Glowcap with { Tune = 0.68f, Pitch = 0.1f },
                LandSoft with { VolumeDb = -2, Tune = 1.1f, Pitch = 0.1f, Heard = ToolReach },
                LandHard with { VolumeDb = -1, Tune = 1.05f, Heard = BreakReach }),
            [Stillwake.Content.Fauna.Fluff.Id] = (
                Glowcap with { Tune = 1.0f, Pitch = 0.14f },
                LandSoft with { VolumeDb = -5, Tune = 1.6f, Pitch = 0.12f, Heard = ToolReach },
                LandHard with { VolumeDb = -4, Tune = 1.5f, Heard = BreakReach }),
        };

        public static readonly Cue Snort = AirFill with { VolumeDb = -6, Tune = 0.7f, Pitch = 0.1f };
    }
}
