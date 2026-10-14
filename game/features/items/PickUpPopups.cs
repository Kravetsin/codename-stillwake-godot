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
/// again. Drawn on the screen over the hero's head, so the text stays sharp at any zoom.
/// </summary>
public partial class PickUpPopups : Control
{
    /// <summary>How long a line stays, rising and fading, in ms; another of its item meanwhile adds to it.</summary>
    public const double PopupMs = 1200;
    /// <summary>How far a line rises over its life, in cells.</summary>
    private const float Rise = 1.2f;
    /// <summary>Lines stacked above the head, newest lowest, this far apart in cells.</summary>
    private const float Gap = 0.7f;

    private sealed class Line(string item, int count, Label text)
    {
        public string Item { get; } = item;
        public int Count { get; set; } = count;
        public double Age { get; set; }
        public Label Text { get; } = text;
        public string Words => $"{T($"item.{Item}")} ({Count})";
    }

    private static readonly LabelSettings Look = new()
    {
        Font = Style.Mono,
        FontSize = 14,
        FontColor = new Color("f2f5fa"),
        OutlineColor = new Color("0b0f17"),
        OutlineSize = 5,
    };

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
            return;
        }
        var line = new Line(item, count, new Label { LabelSettings = Look, MouseFilter = MouseFilterEnum.Ignore });
        line.Text.Text = line.Words;
        _lines = [line, .. _lines];
        AddChild(line.Text);
    }

    /// <summary>Ages the lines and places them over the hero's head, its feet at (x, y) in thousandths of a cell.</summary>
    public void Update(double elapsedMs, double x, double y)
    {
        foreach (var line in _lines) line.Age += elapsedMs;
        foreach (var gone in _lines.Where(l => l.Age >= PopupMs)) gone.Text.QueueFree();
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
        }
    }
}
