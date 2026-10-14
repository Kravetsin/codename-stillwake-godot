using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The moment something is learned (ADR 0024, Learned.tsx): every peer sees the pictures of the
/// blueprints the world has just come to know, for a few seconds, under the words "New blueprints".
/// What was known when this game was opened, or loaded, is no news.
/// </summary>
public partial class LearnedView : CanvasLayer
{
    /// <summary>The world's knowledge is read this often: a panel reads on a timer.</summary>
    private const double RefreshMs = 250;
    /// <summary>How long the blueprints just learned are shown.</summary>
    private const double ShownMs = 6000;

    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly HBoxContainer _pictures = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 8);
    private IReadOnlyList<string>? _seen;
    private double _sinceRead = RefreshMs, _shownFor;

    /// <summary>The blueprints shown just now, for scripted runs and tests.</summary>
    public IReadOnlyList<string> Showing { get; private set; } = [];

    public override void _Ready()
    {
        var box = Style.Box(Style.Slate900 with { A = 0.95f }, Style.Sky600, 0);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (12, 12, 8, 8);
        (box.ShadowColor, box.ShadowSize) = (Style.Sky700 with { A = 0.5f }, 16);
        _panel.AddThemeStyleboxOverride("panel", box);
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        column.AddChild(new Label
        {
            Text = T("research.learned"),
            LabelSettings = Style.Text(Style.Mono, 12, Style.Sky200),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        column.AddChild(_pictures);
        _panel.AddChild(column);
        AddChild(_panel);
    }

    /// <summary>Reads what the world knows through the current registry's research, on a timer, and shows what is new.</summary>
    public void Frame(IResearchQueries research, double elapsedMs)
    {
        _sinceRead += elapsedMs;
        if (Showing.Count > 0 && (_shownFor += elapsedMs) >= ShownMs) Show([], research);
        if (_sinceRead < RefreshMs) return;
        _sinceRead = 0;
        var known = research.Known.ToList();
        var fresh = News.LearnedSince(_seen, known);
        _seen = known;
        if (fresh.Count > 0) Show(fresh, research);
    }

    private void Show(IReadOnlyList<string> ids, IResearchQueries research)
    {
        (Showing, _shownFor) = (ids, 0);
        foreach (var old in _pictures.GetChildren()) old.QueueFree();
        foreach (var id in ids)
        {
            if (research.Blueprints.FirstOrDefault(b => b.Id == id) is not { } blueprint) continue;
            _pictures.AddChild(new TextureRect
            {
                Texture = BlueprintArt.Thumbnail(blueprint.Placement),
                CustomMinimumSize = new Vector2(40, 40),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        _panel.Visible = ids.Count > 0;
        _panel.ResetSize();
        // top-24, centred across the screen.
        var screen = _panel.GetViewportRect().Size;
        _panel.Position = new Vector2((screen.X - _panel.GetCombinedMinimumSize().X) / 2, 96);
    }
}
