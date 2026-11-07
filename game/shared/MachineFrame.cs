using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The one frame every machine's window opens in, under the inventory (0058): the same panel for a crate, a
/// furnace, a smelter, a generator, a trade station, a charging station and a queued station; a header with the
/// machine's name in bold and, for one on the power grid, a bolt and its power, lit while it works and dark at
/// 0 while it does not; under it the machine's own body. Used by the items feature and the crafting feature. It has no
/// ground of its own: its owner's <see cref="MachineHolo"/> draws the hologram under it, and the frame's name, headings
/// and dim words take the station's <see cref="Tone"/> (machine-holograms).
/// </summary>
public partial class MachineFrame : PanelContainer
{
    /// <summary>A bolt with no power through it.</summary>
    private static readonly Color Unlit = new(0.35f, 0.35f, 0.4f);

    private readonly LabelSettings _nameText = Style.Text(Style.Bold, 13, Style.Slate100);
    private readonly VBoxContainer _column = Style.Gap(new VBoxContainer(), 8);
    private readonly Label _name;
    private readonly TextureRect _bolt = new()
    {
        Texture = Signs.Bolt(false),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(10, 13),
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    private readonly Label _power = new() { LabelSettings = Style.Text(Style.Mono, 12, Style.Amber400), SizeFlagsVertical = SizeFlags.ShrinkCenter };
    /// <summary>The words in the tone: each a heading's, or else a dim word's.</summary>
    private readonly List<(Label Label, bool Heading)> _tinted = [];
    private HoloTone _tone;
    private int? _shown;

    /// <param name="name">The machine's name, as the build menu calls it.</param>
    /// <param name="power">What it draws or makes while it works; null for a machine off the power grid: no bolt.</param>
    public MachineFrame(string name, int? power)
    {
        _name = new Label { LabelSettings = _nameText, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        MouseFilter = MouseFilterEnum.Stop;
        // The wrist's screen's margins, so every slot keeps its place (0074).
        var margins = new StyleBoxEmpty();
        margins.SetContentMarginAll(8);
        margins.ContentMarginLeft = margins.ContentMarginRight = Slots.ScreenPadding;
        AddThemeStyleboxOverride("panel", margins);
        Head = Style.Gap(new HBoxContainer(), 6);
        Head.AddChild(_name);
        Head.AddChild(_bolt);
        Head.AddChild(_power);
        _column.AddChild(Head);
        AddChild(_column);
        Describe(name, power);
        Light(false);
        Paint();
    }

    /// <summary>The header's row: a station adds its screen at its end.</summary>
    public HBoxContainer Head { get; }

    /// <summary>The station's colour: its name, headings and dim words in it; the hologram under it in its light.</summary>
    public HoloTone Tone
    {
        get => _tone;
        set
        {
            if (value == _tone) return;
            _tone = value;
            Paint();
        }
    }

    /// <summary>Puts <paramref name="body"/> under the header, after what is there.</summary>
    public void Add(Control body) => _column.AddChild(body);

    /// <summary><paramref name="label"/>, a section's heading, in the tone's heading colour from now on.</summary>
    public T Heading<T>(T label) where T : Label => Tint(label, heading: true);

    /// <summary><paramref name="label"/>, a word aside, in the tone's dim colour from now on.</summary>
    public T Dim<T>(T label) where T : Label => Tint(label, heading: false);

    /// <summary>Names another machine in the same frame: a queued station's panel serves every one.</summary>
    public void Describe(string name, int? power)
    {
        _name.Text = name;
        _shown = power;
        _bolt.Visible = _power.Visible = power != null;
    }

    /// <summary>The bolt and its power, lit while the machine works, dark while it does not: what it would draw is known before it runs (tip-signs).</summary>
    public void Light(bool on)
    {
        _power.Text = (_shown ?? 0).ToString();
        _bolt.Modulate = _power.Modulate = on ? Colors.White : Unlit;
    }

    /// <summary>What it shows at <paramref name="power"/> of lit: 0 dark, 1 on, as the bag's screen comes on (<see cref="TabletMotion.Power"/>).</summary>
    public void Lit(float power) => _column.Modulate = Colors.White with { A = power };

    private T Tint<T>(T label, bool heading) where T : Label
    {
        // A label with no settings of its own is a spacer: nothing to tint.
        if (label.LabelSettings == null) return label;
        // Its own settings: the shared ones are other panels' too.
        label.LabelSettings = (LabelSettings)label.LabelSettings.Duplicate();
        _tinted.Add((label, heading));
        Paint();
        return label;
    }

    private void Paint()
    {
        var tint = HoloLook.Of(_tone);
        _nameText.FontColor = new Color(tint.Name);
        foreach (var (label, heading) in _tinted)
            if (IsInstanceValid(label)) label.LabelSettings.FontColor = new Color(heading ? tint.Heading : tint.Dim);
    }
}
