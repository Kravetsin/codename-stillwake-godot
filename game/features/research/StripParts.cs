using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>What every page of the research tablet writes with: the strip's controls, and its words and pictures of a thing.</summary>
public sealed class StripParts(TipSpots tips)
{
    public static readonly Color Line = new("1a1e25"), Faint = new("7a8090"), Warm = new("f0c050"), Bought = new("6fe08a"),
        Picked = new("ffffff"), From = new("5eead4"), MadeAt = new("f28fd0"), UsedIn = new("7dd3fc");

    /// <summary>Pixels the pointer may move in a press still meant as a click.</summary>
    public const float ClickSlack = 4;

    /// <summary>A thing not known yet: its picture's shape alone, a dark grey that comes up as <c>pencil</c> goes from 0 to 1.</summary>
    private static readonly Shader PencilShape = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float pencil = 0.0;
            void fragment() {
                COLOR = vec4(vec3(mix(0.16, 0.6, pencil)), texture(TEXTURE, UV).a * COLOR.a * 0.9);
            }
            """,
    };

    private const int Big = 64, Entry = 24, UsedLarge = 32, UsedSmall = 24, UsedManyFrom = 20;

    private readonly Dictionary<string, Placement?> _placements = new(StringComparer.Ordinal);

    /// <summary>What the pointer says over what a page drew.</summary>
    public TipSpots Tips { get; } = tips;

    /// <summary>The strip's columns about what is picked.</summary>
    public HBoxContainer Facts { get; } = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true }, 24);

    /// <summary>The strip's button, what a page has it do.</summary>
    public Button Action { get; } = new() { FocusMode = Control.FocusModeEnum.None };

    /// <summary>What the button says when it buys: the word and <see cref="ActionPrice"/>.</summary>
    public HBoxContainer ActionLine { get; } = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center }, 8);

    /// <summary>The price on the button.</summary>
    public CreditLine ActionPrice { get; } = new(Tablet.FontSize + 3);

    /// <summary>Learns what each blueprint places: what its picture and name are drawn from.</summary>
    public void Learn(IResearchQueries research)
    {
        foreach (var recipe in research.Recipes()) _placements[recipe.Id] = recipe.Placement;
    }

    /// <summary>What blueprint <paramref name="id"/> places; null for anything else.</summary>
    public Placement? PlacementOf(string id) => _placements.GetValueOrDefault(id);

    /// <summary>Empties the strip's columns.</summary>
    public void Clear()
    {
        foreach (var child in Facts.GetChildren())
        {
            Facts.RemoveChild(child);
            child.QueueFree();
        }
    }

    /// <summary>A label in the tree's font: its words keep sharp at any size.</summary>
    public static Label Text(string text, Color colour, int size = Tablet.FontSize + 1)
    {
        var label = Tablet.Text(text, colour, size);
        label.LabelSettings.Font = Style.Clear;
        return label;
    }

    /// <summary>What to show of <paramref name="id"/>: a blueprint as the build menu pictures it, a craft or a material as its item.</summary>
    public TextureRect Picture(string id, int size) => new()
    {
        Texture = Content.Upgrades.Of(id) is { } upgrade ? AsciiArt.Upgrade(upgrade.Kind) : _placements.GetValueOrDefault(id) is { } p ? BlueprintArt.Thumbnail(p) : ItemIcons.Icon(id, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        CustomMinimumSize = new Vector2(size, size),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>Draws <paramref name="picture"/> as a shape, <paramref name="pencil"/> of the way to being understood.</summary>
    public static void Pale(TextureRect picture, double pencil)
    {
        if (picture.Material is not ShaderMaterial material) picture.Material = material = new ShaderMaterial { Shader = PencilShape };
        material.SetShaderParameter("pencil", (float)pencil);
    }

    /// <summary>The pale picture of what is not known: no name by it.</summary>
    public TextureRect Unseen(string id)
    {
        var picture = Picture(id, Entry);
        Pale(picture, 0);
        return picture;
    }

    public string NameOf(string id) =>
        Content.Upgrades.Of(id) is { } upgrade ? T($"upgrade.{upgrade.Kind}") : _placements.ContainsKey(id) ? ItemCards.NameOf(id, _placements[id]) : T($"item.{id}");

    /// <summary>The pictures of <paramref name="ids"/>, smaller the more of them there are, each named by the pointer through <paramref name="tip"/>, those <paramref name="pale"/> says pale.</summary>
    public Control Pictures(IReadOnlyList<string> ids, Func<string, Tip> tip, Func<string, bool>? pale = null)
    {
        var flow = Flow(4);
        int size = ids.Count > UsedManyFrom ? UsedSmall : UsedLarge;
        foreach (string id in ids)
        {
            var one = Picture(id, size);
            if (pale?.Invoke(id) == true) Pale(one, 0);
            Tips.Add(one, () => tip(id));
            flow.AddChild(one);
        }
        return flow;
    }

    /// <summary>The strip's first column: the large picture, in <paramref name="pencil"/> if it is a shape, the name and what it is.</summary>
    public Control Title(string name, string about, string? id, Func<string, Tip> tip, double? pencil = null)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(250, 0) }, 10);
        if (id != null)
        {
            var picture = Picture(id, Big);
            picture.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            picture.MouseFilter = Control.MouseFilterEnum.Ignore;
            if (pencil is { } done) Pale(picture, done);
            Tips.Add(picture, () => tip(id));
            row.AddChild(picture);
        }
        var words = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, 2);
        words.AddChild(Text(name, Style.Slate100, 15));
        var line = Text(about, Style.Slate500, Tablet.HintSize + 1);
        line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        words.AddChild(line);
        row.AddChild(words);
        return row;
    }

    public static HFlowContainer Flow(int gap)
    {
        var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        flow.AddThemeConstantOverride("h_separation", gap);
        flow.AddThemeConstantOverride("v_separation", 4);
        return flow;
    }

    /// <summary>A picture with words by it.</summary>
    public Control Named(string id, string words, Color colour)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 5);
        row.AddChild(Picture(id, Entry));
        row.AddChild(Text(words, colour));
        return row;
    }

    /// <summary>A column of the strip, taking <paramref name="share"/> of what width is left: its heading in the colour its nodes are lit in, and what it says.</summary>
    public static Control Fact(string heading, Color colour, Control what, float share)
    {
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = share }, 4);
        column.AddChild(Text(heading, colour, Tablet.HintSize + 1));
        column.AddChild(what);
        return column;
    }
}
