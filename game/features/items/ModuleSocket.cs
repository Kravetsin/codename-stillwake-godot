using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A machine's socket for one module (0086), a slot wide: empty, the ghost of the icon of the technology it takes, in a dark
/// slot with a violet edge; fitted, the module's picture in a slot lit violet-white. While a module that would go in is
/// carried, it glows, the icon lit and crackling. A press on it fits what is carried, or begins taking the module out.
/// </summary>
public partial class ModuleSocket : Panel
{
    private static readonly Color Violet = new(ItemCards.ModuleColour), Deep = new(ItemCards.ModuleGround);
    private static readonly StyleBoxFlat Empty = Style.Box(Style.Slate800 with { A = 0.7f }, Style.Slate600.Lerp(Violet, 0.35f) with { A = 0.8f });
    private static readonly StyleBoxFlat Fitted = Style.Box(Deep, Violet with { A = 0.85f });
    private static readonly StyleBoxFlat Hover = Style.Box(Deep, Violet);
    private static readonly StyleBoxFlat Takes = Glowing();

    /// <summary>The machine it is a socket of, and the module it takes.</summary>
    public int Machine { get; }
    public string Module { get; }

    /// <summary>A press on it: shift held.</summary>
    public event Action<ModuleSocket, bool>? Pressed;

    private readonly TextureRect _icon = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    /// <summary>The tech's icon, filling the inset: its ghost, or lit and crackling while what is carried goes in.</summary>
    private readonly TechIcon _tech;
    private readonly string _techId;
    private (bool Fitted, bool Takes, bool Hovered) _shown;
    private bool _awaiting;

    public ModuleSocket(int machine, Content.ModuleSpec module, int size)
    {
        (Machine, Module, _techId) = (machine, module.Item, module.Tech);
        CustomMinimumSize = new Vector2(size, size);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        _icon.Texture = ItemIcons.Icon(Module);
        _tech = new TechIcon(_techId, size - 2 * Slots.Inset, TechShow.Ghost) { Position = Vector2.One * Slots.Inset };
        AddChild(_tech);
        GuiInput += OnGuiInput;
        Show(false, false, false);
    }

    /// <summary>Whether the module is fitted; whether one carried would go in; whether the pointer is over it.</summary>
    public void Show(bool fitted, bool takes, bool hovered)
    {
        _shown = (fitted, takes, hovered);
        bool full = fitted && !_awaiting;
        AddThemeStyleboxOverride("panel", takes ? Takes : hovered ? Hover : full ? Fitted : Empty);
        _icon.Visible = full;
        _tech.Visible = !full;
        _tech.Set(_techId, takes ? TechShow.Hot : TechShow.Ghost);
    }

    /// <summary>Whether a module shifted here still flies to it: shown empty till it lands.</summary>
    public void Awaiting(bool flying)
    {
        if (flying == _awaiting) return;
        _awaiting = flying;
        Show(_shown.Fitted, _shown.Takes, _shown.Hovered);
    }

    /// <summary>Lit violet-white with a glow round and inside its edge: what is carried goes in here.</summary>
    private static StyleBoxFlat Glowing()
    {
        var box = Style.Box(Deep, new Color("f4eeff"));
        box.ShadowColor = Violet with { A = 0.55f };
        box.ShadowSize = 4;
        return box;
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } b) return;
        AcceptEvent();
        Pressed?.Invoke(this, b.ShiftPressed);
    }
}
