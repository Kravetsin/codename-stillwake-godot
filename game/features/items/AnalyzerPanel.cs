using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// An analyzer's panel's body (0027), in the machine frame: only about the item in its slot, the slot and its screen; empty, a hint;
/// at work, a bar and how far it is in violet; done, in green, what that item goes into and under it
/// every one, opened with a green edge, still closed greyed out, each saying in the one tooltip what it
/// still waits for. Without power the screen is dark and the panel says so; holding an item in no cost, it
/// stays dark and says there is nothing to learn.
/// </summary>
public partial class AnalyzerPanel : VBoxContainer
{
    /// <summary>How many pictures fit a row, and the space round the column.</summary>
    private const int PerRow = 5;

    private static readonly LabelSettings Hint = Style.Text(Style.Mono, 11, Style.Slate400);
    private static readonly LabelSettings Working = Style.Text(Style.Mono, 11, Style.Violet400);
    private static readonly LabelSettings Done = Style.Text(Style.Mono, 11, Style.Green400);
    private static readonly LabelSettings Trouble = Style.Text(Style.Bold, 10, Style.Red300);
    private static readonly StyleBoxFlat Closed = Style.Box(Style.Slate800 with { A = 0.6f }, Style.Slate800, radius: 3);
    private static readonly StyleBoxFlat Opened = Style.Box(Style.Slate800 with { A = 0.6f }, Style.Green400, radius: 3);
    /// <summary>A closed one's picture: grey, and faint.</summary>
    private static readonly ShaderMaterial Greyed = new()
    {
        Shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                void fragment() {
                    vec4 c = texture(TEXTURE, UV) * COLOR;
                    COLOR = vec4(vec3(dot(c.rgb, vec3(0.299, 0.587, 0.114))), c.a * 0.3);
                }
                """,
        },
    };

    private readonly TipSpots _tips;
    private readonly AnalyzerScreen _screen = new() { SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private readonly Label _hint = Words(Hint), _state = Words(Hint), _problem = Words(Trouble);
    private readonly ProgressBar _bar = Style.Bar(Style.Amber400, 0);
    private readonly HFlowContainer _recipes = Style.Gap(new HFlowContainer(), Slots.Gap);
    private readonly int _size;
    private string? _shown;

    /// <param name="slot">Its slot, made and wired by the inventory.</param>
    /// <param name="tips">Where its pictures say what they wait for.</param>
    /// <param name="size">A slot's side, <see cref="Ui.Slot"/>: its pictures are as large, five a row.</param>
    public AnalyzerPanel(SlotBox slot, TipSpots tips, int size)
    {
        (_tips, _size) = (tips, size);
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(PerRow * size + (PerRow - 1) * Slots.Gap, 0);
        Style.Gap(this, 6);
        var top = Style.Gap(new HBoxContainer(), 8);
        top.AddChild(slot);
        top.AddChild(_screen);
        AddChild(top);
        _hint.Text = T("research.empty");
        _bar.CustomMinimumSize = new Vector2(0, 4);
        foreach (var each in (Control[])[_hint, _bar, _state, _problem, _recipes]) AddChild(each);
    }

    /// <summary>Whether it is analysing now: the frame's bolt is lit.</summary>
    public bool Works { get; private set; }

    private static Label Words(LabelSettings settings) => new() { LabelSettings = settings, Visible = false, MouseFilter = MouseFilterEnum.Ignore };

    /// <summary>Shows <paramref name="lessons"/>; the pictures are built again only when they change.</summary>
    public void Show(Lessons lessons)
    {
        var stage = lessons.Stage;
        _screen.Stage = stage;
        Works = stage == Stage.Working;
        _hint.Visible = stage == Stage.Empty;
        _bar.Visible = stage is Stage.Working or Stage.Unpowered;
        _bar.Value = lessons.Percent / 100.0;
        _state.Visible = stage is Stage.Working or Stage.Done or Stage.NothingToLearn;
        _state.LabelSettings = stage switch { Stage.Done => Done, Stage.NothingToLearn => Hint, _ => Working };
        _state.Text = stage switch
        {
            Stage.Done => T("research.goesInto", ("item", T($"item.{lessons.Item}"))),
            Stage.NothingToLearn => T("research.nothingToLearn", ("item", T($"item.{lessons.Item}"))),
            _ => T("research.working", ("n", lessons.Percent)),
        };
        _problem.Visible = stage == Stage.Unpowered;
        _problem.Text = T("research.unpowered");
        if (lessons.Key == _shown) return;
        _shown = lessons.Key;
        foreach (var old in _recipes.GetChildren())
        {
            _recipes.RemoveChild(old);
            old.QueueFree();
        }
        foreach (var lesson in lessons.Recipes) _recipes.AddChild(Picture(lesson));
        _recipes.Visible = _recipes.GetChildCount() > 0;
        ResetSize();
    }

    /// <summary>A blueprint as the build menu pictures it, a craft as the item it makes: green-edged once opened, greyed out until then.</summary>
    private Control Picture(Lesson lesson)
    {
        var box = new Panel { CustomMinimumSize = new Vector2(_size, _size), MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeStyleboxOverride("panel", lesson.Opened ? Opened : Closed);
        var placement = lesson.Recipe.Placement;
        var (at, side) = placement != null ? (Slots.Inset, _size - 2 * Slots.Inset) : Slots.IconBox(lesson.Recipe.Id, _size);
        var picture = new TextureRect
        {
            Texture = placement is { } p ? BlueprintArt.Thumbnail(p) : ItemIcons.Icon(lesson.Recipe.Id, side),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = new Vector2(at, at),
            Size = new Vector2(side, side),
            MouseFilter = MouseFilterEnum.Ignore,
            Material = lesson.Opened ? null : Greyed,
        };
        box.AddChild(picture);
        var tip = lesson.Tip;
        _tips.Add(box, () => tip);
        return box;
    }
}
