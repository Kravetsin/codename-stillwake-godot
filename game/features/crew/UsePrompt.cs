using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What E does at the pointer, in a line at the bottom middle, above the build menu (CrewPanel.tsx's
/// UsePrompt); red, with the reason, when the sim would refuse it. Nothing when there is nothing to do
/// within reach.
/// </summary>
public partial class UsePrompt : CanvasLayer
{
    private static readonly Color Red700 = new("b91c1c"), Red400 = new("f87171");
    private static readonly StyleBoxFlat Plain = Edge(Style.Slate600), Refused = Edge(Red700);

    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Label _action = new(), _problem = new();
    private Prompt? _shown;

    public UsePrompt()
    {
        Layer = 10;
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", 0);
        line.AddChild(_action);
        line.AddChild(_problem);
        _panel.AddChild(line);
        // bottom-28, centred: its middle on the screen's, 112 pixels above the bottom edge.
        (_panel.AnchorLeft, _panel.AnchorRight, _panel.AnchorTop, _panel.AnchorBottom) = (0.5f, 0.5f, 1, 1);
        _panel.OffsetBottom = -112;
        AddChild(_panel);
    }

    private static StyleBoxFlat Edge(Color border)
    {
        var box = Style.Box(Style.Slate900 with { A = 0.9f }, border);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (12, 12, 6, 6);
        return box;
    }

    /// <summary>Shows <paramref name="prompt"/>, or nothing; changes only when it says something else.</summary>
    public void Show(Prompt? prompt)
    {
        if (prompt == _shown) return;
        _shown = prompt;
        _panel.Visible = prompt != null;
        if (prompt == null) return;
        bool refused = prompt.Problem != null;
        _panel.AddThemeStyleboxOverride("panel", refused ? Refused : Plain);
        _action.LabelSettings = Style.Text(Style.Mono, 14, refused ? Style.Red300 : Style.Slate100);
        _action.Text = T(prompt.Action);
        _problem.LabelSettings = Style.Text(Style.Mono, 14, Red400);
        _problem.Text = refused ? $" · {T(prompt.Problem!)}" : "";
        _problem.Visible = refused;
        // Sized to the new line, centred on its anchor.
        var size = _panel.GetCombinedMinimumSize();
        (_panel.OffsetLeft, _panel.OffsetRight) = (-size.X / 2, size.X / 2);
        _panel.OffsetTop = _panel.OffsetBottom - size.Y;
    }
}
