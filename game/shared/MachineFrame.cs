using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The one frame every machine's window opens in, under the inventory (0058): the same panel for a crate, a
/// furnace, a smelter, a generator, an analyzer, a charging station and a queued station; a header with the
/// machine's name in bold and, for one on the power grid, a bolt and its power, lit while it works and dark at
/// 0 while it does not; under it the machine's own body. Used by the items feature and the crafting feature. It is the
/// wrist computer's module under the forearm display (0074): its screen greyer and greener than the bag's, a seam between.
/// </summary>
public partial class MachineFrame : WristScreen
{
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 13, Style.Slate100);
    /// <summary>A bolt with no power through it.</summary>
    private static readonly Color Unlit = new(0.35f, 0.35f, 0.4f);

    private readonly VBoxContainer _column = Style.Gap(new VBoxContainer(), 8);
    private readonly Label _name = new() { LabelSettings = NameText, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextureRect _bolt = new()
    {
        Texture = AsciiArt.Bolt(false),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        CustomMinimumSize = new Vector2(10, 13),
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    private readonly Label _power = new() { LabelSettings = Style.Text(Style.Mono, 12, Style.Amber400), SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private int? _shown;

    /// <param name="name">The machine's name, as the build menu calls it.</param>
    /// <param name="power">What it draws or makes while it works; null for a machine off the power grid: no bolt.</param>
    public MachineFrame(string name, int? power) : base(WristPieces.Dock, WristArt.MachineScreen, 8)
    {
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        Head = Style.Gap(new HBoxContainer(), 6);
        Head.AddChild(_name);
        Head.AddChild(_bolt);
        Head.AddChild(_power);
        _column.AddChild(Head);
        AddChild(_column);
        Describe(name, power);
        Light(false);
    }

    /// <summary>The header's row: a station adds its screen at its end.</summary>
    public HBoxContainer Head { get; }

    /// <summary>Puts <paramref name="body"/> under the header, after what is there.</summary>
    public void Add(Control body) => _column.AddChild(body);

    /// <summary>Names another machine in the same frame: a queued station's panel serves every one.</summary>
    public void Describe(string name, int? power)
    {
        _name.Text = name;
        _shown = power;
        _bolt.Visible = _power.Visible = power != null;
    }

    /// <summary>The bolt lit with its power while the machine works, dark at 0 while it does not.</summary>
    public void Light(bool on)
    {
        _power.Text = (on ? _shown ?? 0 : 0).ToString();
        _bolt.Modulate = _power.Modulate = on ? Colors.White : Unlit;
    }
}
