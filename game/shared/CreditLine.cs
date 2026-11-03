using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>An amount of credits as the game writes it everywhere: the coin and the number.</summary>
public partial class CreditLine : HBoxContainer
{
    private readonly Label _amount;
    private readonly TextureRect _coin;
    private int? _shown;

    /// <param name="size">The number's font size; the coin is drawn larger (<see cref="CoinOf"/>).</param>
    public CreditLine(int size)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 3);
        AddChild(_coin = new TextureRect
        {
            Texture = Signs.Credit(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(CoinOf(size), CoinOf(size)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddChild(_amount = new Label { LabelSettings = new LabelSettings { Font = Style.Clear, FontSize = size, FontColor = new Color("f0c050") }, MouseFilter = MouseFilterEnum.Ignore });
    }

    /// <summary>The coin's side beside a number of <paramref name="size"/>: the upright chip reads only this large (credit-counter).</summary>
    public static int CoinOf(int size) => size + 7;

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
