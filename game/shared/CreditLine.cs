using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>An amount of credits as the game writes it everywhere: the coin and the number.</summary>
public partial class CreditLine : HBoxContainer
{
    private readonly Label _amount;
    private int? _shown;

    /// <param name="size">The number's font size; the coin is drawn a little larger.</param>
    public CreditLine(int size)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 3);
        AddChild(new TextureRect
        {
            Texture = AsciiArt.Credit(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            CustomMinimumSize = new Vector2(size + 4, size + 4),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddChild(_amount = new Label { LabelSettings = new LabelSettings { Font = Style.Mono, FontSize = size, FontColor = new Color("f0c050") }, MouseFilter = MouseFilterEnum.Ignore });
    }

    /// <summary>Shows <paramref name="amount"/>.</summary>
    public void Set(int amount)
    {
        if (amount == _shown) return;
        _shown = amount;
        _amount.Text = amount.ToString();
    }

    /// <summary>Colours the number: dim for a price out of reach.</summary>
    public void Tint(Color colour) => _amount.LabelSettings.FontColor = colour;
}
