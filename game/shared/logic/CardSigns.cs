using System;
using System.Globalization;

namespace Stillwake.Game.Shared;

/// <summary>
/// An item card's signs (2026-11-07-tool-card-rows; food and gear, 2026-11-14-food-and-gear-cards): each number's icon, drawn in
/// lines on a 24 px square as the pick drew it, in its own hue, so a row is found by its colour and shape before its words; and
/// each group's title.
/// </summary>
public static class CardSigns
{
    /// <summary>An icon's lines: its shape, the lines cut into it, the marks beside it; each an SVG path, stroked alike.</summary>
    public sealed record Drawing(string Shape, string Cut = "", string Marks = "");

    /// <summary>Two lungs, the calm sign's shape.</summary>
    private const string Lungs = "M10.5 9C8.2 7.6 4.6 10 4.1 14.8C3.7 18.6 5.2 20.6 7.6 20.1C9.7 19.7 10.5 18 10.5 15.5ZM13.5 9C15.8 7.6 19.4 10 19.9 14.8C20.3 18.6 18.8 20.6 16.4 20.1C14.3 19.7 13.5 18 13.5 15.5Z";

    /// <summary>The drawing of <paramref name="sign"/>.</summary>
    public static Drawing Of(CardSign sign) => sign switch
    {
        CardSign.Damage => new("M4 20L7 11L11 13L13 6L16 9.5L19.5 8L17 15.5L13 14.5L10.5 21Z", "M8.6 17.2h.01M12.6 14.4L13.3 10.4", "M19 1.8v2.4M22.2 5h-2.4M21.6 2.4l-1.5 1.5"),
        CardSign.Block => new("M3 17L5 8.5L10 5L17 6L21 11L20 18L13 20.5L6 20Z", "M10.5 5.3L12 10L9.5 13.2L11 20.3M12 10L16.5 12.5"),
        CardSign.Pace => new("M12 5.5a7.75 7.75 0 1 1 0 15.5a7.75 7.75 0 1 1 0-15.5Z", "M12 13.25V8.8M12 13.25L15 15.2", "M9.8 2.5h4.4M12 2.5v3M18.2 6.2l1.6-1.6"),
        // A plate a bolt flies through.
        CardSign.Pierce => new("M11 3.5h3v17h-3Z", "", "M2.5 12h19M18 8.5L21.5 12L18 15.5"),
        CardSign.Arc => new("M4 20L19.45 15.86A16 16 0 0 0 8.14 4.55Z", "M12.69 17.67A9 9 0 0 0 6.33 11.31", "M22 13.2A18.5 18.5 0 0 0 15 4.2"),
        CardSign.Reach => new("M2.5 15.5h19v5h-19Z", "M6.5 15.5v2.6M10.5 15.5v1.6M14.5 15.5v2.6M18.5 15.5v1.6", "M3 8.5h15M14.8 5l3.5 3.5-3.5 3.5M21.5 4.5v8"),
        // A drum the line winds onto.
        CardSign.Reel => new("M9 6.5a5.5 5.5 0 1 1 0 11a5.5 5.5 0 1 1 0-11Z", "M9 12h.01", "M14.5 12H22M17.5 9L14.5 12L17.5 15"),
        CardSign.Battery => new("M3 8h15.5v9H3ZM18.5 10.5H21v4h-2.5Z", "M11.8 9.6L8.9 12.9h3.4L10.1 15.6"),
        CardSign.Loot => new("M3 10h12.5v10.5H3Z", "M3 13.5h12.5M7.2 13.5v7M11.3 13.5v7", "M19.5 3v6M16.5 6h6"),
        // Food and gear (2026-11-14-food-and-gear-cards, set A): each a thing. Lungs and an arrow down: less air a breath.
        CardSign.Calm => new(Lungs, "M12 3.5V11.5M12 11.5L10.5 13M12 11.5L13.5 13", "M19.5 2v4.6M17.7 4.8l1.8 1.8 1.8-1.8"),
        // A canister and a plus: more from each.
        CardSign.Deep => new("M7.5 11.5a3.5 3.5 0 0 1 7 0V21h-7Z", "M9.2 4.5h3.6M11 4.5V8M7.5 14.5h7", "M19 3.5v6M16 6.5h6"),
        // A hammer and an arrow up.
        CardSign.Work => new("M4 4.5h9.5L16 7v3H4ZM8.8 10v10.8h2.4V10", "M13.5 4.5V10", "M19.5 21v-7M17 16.5l2.5-2.5 2.5 2.5"),
        // Two bottles: what is held.
        CardSign.Tank => new("M5 8a2.5 2.5 0 0 1 5 0V20.5H5ZM14 8a2.5 2.5 0 0 1 5 0V20.5h-5Z", "M10 12h4M10 16.5h4M7.5 2.8v2.7M16.5 2.8v2.7"),
        CardSign.Cold => new("M12 3.5L12 20.5M4.64 7.75L19.36 16.25M4.64 16.25L19.36 7.75", "M13.84 4.36L12 6.2L10.16 4.36M6.3 6.59L6.98 9.1L4.47 9.77M4.47 14.23L6.98 14.9L6.3 17.41M10.16 19.64L12 17.8L13.84 19.64M17.7 17.41L17.02 14.9L19.53 14.23M19.53 9.77L17.02 9.1L17.7 6.59"),
        CardSign.Guard => new("M12 3L19.5 5.8V11.5C19.5 16 16.4 19.4 12 21C7.6 19.4 4.5 16 4.5 11.5V5.8Z", "M8.5 10l3.5 2.8 3.5-2.8"),
        // Arrows round a bolt: air made of charge.
        CardSign.Regen => new("M19 12a7 7 0 0 1-12.4 4.45M5 12a7 7 0 0 1 12.4-4.45", "M12.9 8.6L10.4 12.3h3.2L11.1 15.9", "M6.57 19.25L6.6 16.45L9.35 16.96M17.43 4.75L17.4 7.55L14.65 7.04"),
        // A nozzle and its flame.
        CardSign.Speed => new("M8.5 3.5h7L14 9.5h-4ZM12 11.5c2.6 2.6 3.2 5.4 0 9.3-3.2-3.9-2.6-6.7 0-9.3Z", "M12 15.5v2.5", "M4.5 12.5v6M19.5 12.5v6"),
        CardSign.Light => new("M12 4a5.5 5.5 0 0 0-3.3 9.9V16.5h6.6v-2.6A5.5 5.5 0 0 0 12 4Z", "M9.8 19h4.4M10.8 21.2h2.4", "M2.8 9.5h2M19.2 9.5h2M5.2 3.6l1.4 1.4M18.8 3.6l-1.4 1.4"),
        _ => throw new ArgumentOutOfRangeException(nameof(sign), sign, "a number with no sign has no icon"),
    };

    /// <summary>
    /// The hue of <paramref name="sign"/>, "rrggbb": a wreck's hot metal, rock's stone, time's pale cyan, reach's steel, power's and loot's
    /// greens; food's and gear's each their own, as set A picked them.
    /// </summary>
    public static string Hue(CardSign sign) => sign switch
    {
        CardSign.Damage or CardSign.Pierce => "f08c4a",
        CardSign.Block => "c7b08c",
        CardSign.Pace => "8fd6e3",
        CardSign.Arc => "93a8ea",
        CardSign.Reach or CardSign.Reel => "6ca2d6",
        CardSign.Battery => "b9d46a",
        CardSign.Loot => "74d19c",
        CardSign.Calm => "b3a6ff",
        CardSign.Deep => "6fa8ff",
        CardSign.Work => "e8d06a",
        CardSign.Tank => "68c3e6",
        CardSign.Cold => "d2e8ff",
        CardSign.Guard => "b9c3d4",
        CardSign.Regen => "7fe0ae",
        CardSign.Speed => "ff9a5c",
        CardSign.Light => "ffd88a",
        _ => "c3cbd8",
    };

    /// <summary>The i18n key of <paramref name="group"/>'s title.</summary>
    public static string TitleKey(CardGroup group) => $"card.group.{group.ToString().ToLowerInvariant()}";

    /// <summary>The line width the pick drew in, on the 24 px square.</summary>
    private const string Stroke = "1.6";

    /// <summary>
    /// <paramref name="sign"/> as an SVG of <paramref name="size"/> px: its lines in its hue; <paramref name="off"/> for a number the
    /// tool lacks, dimmed and struck through, the strike cut out of the card's ground first.
    /// </summary>
    public static string Svg(CardSign sign, int size, bool off = false)
    {
        var d = Of(sign);
        string hue = "#" + Hue(sign), px = size.ToString(CultureInfo.InvariantCulture);
        string paths = $"<path d=\"{d.Shape}\"/>" + (d.Cut.Length > 0 ? $"<path d=\"{d.Cut}\"/>" : "") + (d.Marks.Length > 0 ? $"<path d=\"{d.Marks}\"/>" : "");
        string strike = off ? "<path d=\"M3.5 21.5L20.5 2.5\" stroke=\"#0f1729\" stroke-width=\"4.4\"/><path d=\"M3.5 21.5L20.5 2.5\" stroke-width=\"1.7\"/>" : "";
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" width=\"{px}\" height=\"{px}\">"
            + $"<g fill=\"none\" stroke=\"{hue}\" stroke-width=\"{Stroke}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{(off ? " opacity=\"0.5\"" : "")}>"
            + paths + strike + "</g></svg>";
    }
}
