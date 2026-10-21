using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A machine's socket for one module (0086), a slot wide: empty, a faint ghost of the module it takes in a dark slot with
/// a violet edge; fitted, the module's picture in a slot lit violet-white. While a module that would go in is carried, it
/// glows. A press on it fits what is carried, or begins taking the module out.
/// </summary>
public partial class ModuleSocket : Panel
{
    private static readonly Color Violet = new(ItemCards.ModuleColour), Deep = new(ItemCards.ModuleGround);
    private static readonly StyleBoxFlat Empty = Style.Box(Style.Slate800 with { A = 0.7f }, Style.Slate600.Lerp(Violet, 0.35f) with { A = 0.8f });
    private static readonly StyleBoxFlat Fitted = Style.Box(Deep, Violet with { A = 0.85f });
    private static readonly StyleBoxFlat Hover = Style.Box(Deep, Violet);
    private static readonly StyleBoxFlat Takes = Glowing();
    private const float GhostAlpha = 0.2f;

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

    public ModuleSocket(int machine, string module, int size)
    {
        (Machine, Module) = (machine, module);
        CustomMinimumSize = new Vector2(size, size);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        GuiInput += OnGuiInput;
        Show(false, false, false);
    }

    /// <summary>Whether the module is fitted; whether one carried would go in; whether the pointer is over it.</summary>
    public void Show(bool fitted, bool takes, bool hovered)
    {
        AddThemeStyleboxOverride("panel", takes ? Takes : hovered ? Hover : fitted ? Fitted : Empty);
        _icon.Texture = fitted ? ItemIcons.Icon(Module) : ItemIcons.Silhouette(Module);
        _icon.Modulate = fitted ? Colors.White : Violet with { A = GhostAlpha };
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
