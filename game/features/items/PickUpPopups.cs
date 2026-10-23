using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;
using HeroSpec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What the hero just picked up, over its head, as Terraria shows it (crew/pickup-popups.ts):
/// "Scrap (12)" rising and fading; another of the same item while it shows adds to it and starts it
/// again. Drawn on the screen over the hero's head, so the text stays sharp at any zoom: a soft shadow round it, and a band of
/// light sweeping across a new line once (0075).
/// </summary>
public partial class PickUpPopups : Control
{
    /// <summary>How long a line stays, rising and fading, in ms; another of its item meanwhile adds to it.</summary>
    public const double PopupMs = 1200;
    /// <summary>How far a line rises over its life, in cells.</summary>
    private const float Rise = 2.4f;
    /// <summary>Lines stacked above the head, newest lowest, this far apart in cells.</summary>
    private const float Gap = 1.4f;

    private sealed class Line(string item, int count, Label text)
    {
        public string Item { get; } = item;
        public int Count { get; set; } = count;
        public double Age { get; set; }
        public Label Text { get; } = text;
        /// <summary>The band of light sweeping across it once as it appears: a wide faint one and a narrow bright one, each a copy of its words clipped to the band.</summary>
        public (Control Clip, Label Shine)[] Bands { get; } = [Band(SweepWide), Band(SweepNarrow)];
        public string Words => $"{T($"item.{Item}")} ({Count})";
    }

    private static readonly LabelSettings Look = new()
    {
        Font = Style.Mono,
        FontSize = 14,
        FontColor = new Color("f2f5fa"),
        // A soft shadow round the words, no hard outline (0075).
        ShadowColor = new Color(4 / 255f, 7 / 255f, 12 / 255f, 0.9f),
        ShadowSize = 4,
        ShadowOffset = Vector2.Zero,
    };

    /// <summary>The sweep's words: light added over the line's, pale sky.</summary>
    private static readonly LabelSettings Shining = new() { Font = Style.Mono, FontSize = 14, FontColor = new Color("c8f0ff") };
    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>How long the band of light takes across a new line, in ms; its two bands' widths, shares of the line's, and brightness.</summary>
    private const double SweepMs = 450;
    private static readonly (float Width, float Alpha) SweepWide = (0.36f, 0.35f), SweepNarrow = (0.14f, 0.55f);

    private static (Control, Label) Band((float Width, float Alpha) band)
    {
        var clip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        var shine = new Label { LabelSettings = Shining, Material = Added, MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, band.Alpha) };
        clip.AddChild(shine);
        return (clip, shine);
    }

    private List<Line> _lines = [];

    public PickUpPopups() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary>Shows <paramref name="count"/> of <paramref name="item"/> picked up just now.</summary>
    public void Add(string item, int count)
    {
        if (_lines.Find(l => l.Item == item) is { } same)
        {
            same.Count += count;
            same.Age = 0;
            same.Text.Text = same.Words;
            foreach (var (_, shine) in same.Bands) shine.Text = same.Words;
            return;
        }
        var line = new Line(item, count, new Label { LabelSettings = Look, MouseFilter = MouseFilterEnum.Ignore });
        line.Text.Text = line.Words;
        _lines = [line, .. _lines];
        AddChild(line.Text);
        foreach (var (clip, shine) in line.Bands)
        {
            shine.Text = line.Words;
            AddChild(clip);
        }
    }

    /// <summary>Ages the lines and places them over the hero's head, its feet at (x, y) in thousandths of a cell.</summary>
    public void Update(double elapsedMs, double x, double y)
    {
        foreach (var line in _lines) line.Age += elapsedMs;
        foreach (var gone in _lines.Where(l => l.Age >= PopupMs))
        {
            gone.Text.QueueFree();
            foreach (var (clip, _) in gone.Bands) clip.QueueFree();
        }
        _lines = _lines.Where(l => l.Age < PopupMs).ToList();
        var toScreen = GetViewport().GetCanvasTransform();
        double head = (y - HeroSpec.Height) / 1000;
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            double life = line.Age / PopupMs;
            var world = new Vector2((float)(x / 1000), (float)(head - i * Gap - life * Rise)) * ShipCamera.Cell;
            var size = line.Text.GetMinimumSize();
            line.Text.Size = size;
            line.Text.Position = toScreen * world - new Vector2(size.X / 2, size.Y);
            line.Text.Modulate = new Color(1, 1, 1, (float)(1 - life * life));
            Sweep(line, size);
        }
    }

    /// <summary>A new line's band of light, across it once in <see cref="SweepMs"/>, eased out, fading as it goes; gone after.</summary>
    private static void Sweep(Line line, Vector2 size)
    {
        double s = line.Age / SweepMs;
        double middle = size.X * (-0.3 + 1.6 * (1 - (1 - Math.Min(1, s)) * (1 - Math.Min(1, s))));
        for (int b = 0; b < line.Bands.Length; b++)
        {
            var (clip, shine) = line.Bands[b];
            clip.Visible = s < 1;
            if (!clip.Visible) continue;
            var (width, alpha) = b == 0 ? SweepWide : SweepNarrow;
            float left = (float)(middle - size.X * width / 2);
            clip.Position = line.Text.Position + new Vector2(left, 0);
            clip.Size = new Vector2(size.X * width, size.Y);
            shine.Position = new Vector2(-left, 0);
            shine.Size = size;
            shine.Modulate = new Color(1, 1, 1, (float)(alpha * (1 - s)));
        }
    }
}
