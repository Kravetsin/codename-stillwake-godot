using System;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The columns of <see cref="GainSpots"/> beside the places that gave (2026-11-06-gain-spots): each line its item's icon and "+n", a
/// soft shadow round them and no outline (0075), swelling and flashing pale sky as it grows. Set a little up and to the right of
/// its place, so the cell dug or the piece caught stays in sight. A body's line says so in words. Drawn on the screen over the
/// world, so it stays sharp and the same size at any zoom.
/// </summary>
public partial class GainSpotsView : Control
{
    /// <summary>The icon's side, the rows' spacing, the gap after an icon, the text's size, px on the screen; how much a flare swells a line.</summary>
    private const float IconPx = 20, RowPx = 24, Gap = 4, Swell = 0.26f;
    private const int TextSize = 15;
    /// <summary>Where a column's foot stands from its place, px on the screen: beside it, not over it.</summary>
    private static readonly Vector2 Aside = new(12, -12);
    private static readonly Color Plain = new("f2f5fa"), Lit = new("c8f0ff"), Shade = new(4 / 255f, 7 / 255f, 12 / 255f, 0.9f);
    private static readonly Font TextFont = Style.Mono;

    private GainSpots? _spots;

    public GainSpotsView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Draws <paramref name="spots"/> as they stand, on the world as the camera shows it now.</summary>
    public void Show(GainSpots spots)
    {
        _spots = spots;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_spots == null) return;
        var toScreen = GetViewport().GetCanvasTransform();
        foreach (var spot in _spots.Spots)
        {
            var foot = toScreen * (new Vector2((float)spot.X, (float)spot.Y) * ShipCamera.Cell) + Aside;
            var lines = spot.Lines;
            for (int i = 0; i < lines.Lines.Count; i++)
            {
                var line = lines.Lines[i];
                float a = (float)lines.Alpha(line), pop = (float)lines.Pop(line);
                if (a <= 0.01f) continue;
                float row = double.IsNaN(line.Row) ? i : (float)line.Row;
                var left = new Vector2(Mathf.Round(foot.X), Mathf.Round(foot.Y - row * RowPx));
                float s = 1 + Swell * pop;
                DrawSetTransformMatrix(Transform2D.Identity.Translated(-left).Scaled(Vector2.One * s).Translated(left));
                bool body = line.Key == GainLines.Body;
                float x = left.X;
                if (!body)
                {
                    var icon = new Rect2(x, left.Y - IconPx / 2, IconPx, IconPx);
                    DrawTextureRect(ItemIcons.Icon(line.Key), icon.Grow(1.5f), false, Shade with { A = 0.5f * a });
                    DrawTextureRect(ItemIcons.Icon(line.Key), icon, false, Colors.White with { A = a });
                    x += IconPx + Gap;
                }
                string text = body ? $"{T(line.Key)} +{line.Count}" : $"+{line.Count}";
                var at = new Vector2(x, left.Y + TextSize * 0.36f);
                DrawStringOutline(TextFont, at, text, HorizontalAlignment.Left, -1, TextSize, 5, Shade with { A = Shade.A * a });
                DrawString(TextFont, at, text, HorizontalAlignment.Left, -1, TextSize, Plain.Lerp(Lit, pop) with { A = a });
            }
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }
}
