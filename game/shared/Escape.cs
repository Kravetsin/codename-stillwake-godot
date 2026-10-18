namespace Stillwake.Game.Shared;

/// <summary>Which page of the game menu is shown, if any.</summary>
public enum MenuPage
{
    Closed,
    Main,
    Settings,
}

/// <summary>What one press of Esc does.</summary>
public enum EscapeDoes
{
    /// <summary>Closes the inventory, the crate open and the Handbook, and puts the plan away (0022).</summary>
    PutAway,
    /// <summary>Opens the game menu.</summary>
    OpenMenu,
    /// <summary>Goes back from a page of the menu to its first.</summary>
    Back,
    /// <summary>Closes the game menu.</summary>
    CloseMenu,
}

/// <summary>
/// Esc, decided in one place: the menu shown takes it first, a page at a time; else anything in hand
/// or open is put away, the inventory and its crate and the plan together; only with nothing to put
/// away does it open the menu.
/// </summary>
public static class Escape
{
    /// <param name="page">The menu's page shown.</param>
    /// <param name="busy">Whether the inventory, a crate or the Handbook is open, or the plan out.</param>
    public static EscapeDoes Of(MenuPage page, bool busy) => page switch
    {
        MenuPage.Settings => EscapeDoes.Back,
        MenuPage.Main => EscapeDoes.CloseMenu,
        _ => busy ? EscapeDoes.PutAway : EscapeDoes.OpenMenu,
    };
}
