using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The chips of <see cref="HeadTally"/> over the hero's head (2026-11-06-head-tally): each its item's icon and "+n", a soft
/// shadow round them and no outline (0075), swelling and flashing pale sky as it grows. A body's chip says so in words, there
/// being no one picture of all it held. Drawn on the screen over the anchor, so it stays sharp at any zoom.
/// </summary>
public partial class GainTally : Control
{
    /// <summary>The icon's side on the screen, px; how much a bump swells a chip; rows this far apart, px.</summary>
    private const float IconPx = 20, Swell = 0.26f, RowPx = 26;
    private static readonly Color Plain = new("f2f5fa"), Lit = new("c8f0ff");

    private static readonly LabelSettings Look = new()
    {
        Font = Style.Mono,
        FontSize = 15,
        FontColor = Colors.White,
        ShadowColor = new Color(4 / 255f, 7 / 255f, 12 / 255f, 0.9f),
        ShadowSize = 5,
        ShadowOffset = Vector2.Zero,
    };

    private sealed class Row
    {
        public required Control Box { get; init; }
        public required TextureRect Icon { get; init; }
        public required Label Text { get; init; }
    }

    private readonly List<Row> _rows = [];

    public GainTally() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary>Draws <paramref name="tally"/>'s chips where its anchor stands, in cells, on the world as the camera shows it now.</summary>
    public void Show(HeadTally tally)
    {
        var chips = tally.Chips;
        while (_rows.Count < chips.Count) _rows.Add(NewRow());
        var toScreen = GetViewport().GetCanvasTransform();
        var (ax, ay) = tally.Anchor;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            row.Box.Visible = i < chips.Count;
            if (!row.Box.Visible) continue;
            var chip = chips[i];
            float bump = (float)tally.Bump(chip);
            bool body = chip.Key == HeadTally.Body;
            row.Icon.Visible = !body;
            if (!body) row.Icon.Texture = ItemIcons.Icon(chip.Key);
            row.Text.Text = body ? $"{T(chip.Key)} +{chip.Count}" : $"+{chip.Count}";
            row.Text.Modulate = Plain.Lerp(Lit, bump);
            var text = row.Text.GetMinimumSize();
            float icon = body ? 0 : IconPx + 4;
            row.Text.Position = new Vector2(icon, 0);
            row.Text.Size = text;
            var size = new Vector2(icon + text.X, Math.Max(IconPx, text.Y));
            row.Icon.Position = new Vector2(0, (size.Y - IconPx) / 2);
            row.Box.Size = size;
            row.Box.PivotOffset = size / 2;
            row.Box.Scale = Vector2.One * (1 + Swell * bump);
            var at = toScreen * (new Vector2((float)ax, (float)ay) * ShipCamera.Cell) - new Vector2(0, (float)chip.Rise * RowPx);
            row.Box.Position = at - size / 2;
            row.Box.Modulate = new Color(1, 1, 1, (float)tally.Alpha(chip));
        }
    }

    private Row NewRow()
    {
        var box = new Control { MouseFilter = MouseFilterEnum.Ignore };
        var icon = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(IconPx, IconPx),
            MouseFilter = MouseFilterEnum.Ignore,
            TextureFilter = TextureFilterEnum.Linear,
        };
        var text = new Label { LabelSettings = Look, MouseFilter = MouseFilterEnum.Ignore };
        box.AddChild(icon);
        box.AddChild(text);
        AddChild(box);
        return new Row { Box = box, Icon = icon, Text = text };
    }
}
