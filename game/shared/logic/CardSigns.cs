using System;
using System.Globalization;

namespace Stillwake.Game.Shared;

/// <summary>
/// A tool card's signs (2026-11-07-tool-card-rows): each number's icon, drawn in lines on a 24 px square as the pick drew it, in
/// its own hue, so a row is found by its colour and shape before its words; and each group's title.
/// </summary>
public static class CardSigns
{
    /// <summary>An icon's lines: its shape, the lines cut into it, the marks beside it; each an SVG path, stroked alike.</summary>
    public sealed record Drawing(string Shape, string Cut = "", string Marks = "");

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
        _ => throw new ArgumentOutOfRangeException(nameof(sign), sign, "a number with no sign has no icon"),
    };

    /// <summary>The hue of <paramref name="sign"/>, "rrggbb": a wreck's hot metal, rock's stone, time's pale cyan, reach's steel, power's and loot's greens.</summary>
    public static string Hue(CardSign sign) => sign switch
    {
        CardSign.Damage or CardSign.Pierce => "f08c4a",
        CardSign.Block => "c7b08c",
        CardSign.Pace => "8fd6e3",
        CardSign.Arc => "93a8ea",
        CardSign.Reach or CardSign.Reel => "6ca2d6",
        CardSign.Battery => "b9d46a",
        CardSign.Loot => "74d19c",
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
