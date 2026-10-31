namespace Stillwake.Content;

/// <summary>
/// The ship a new world starts in: the torn-off scoop of a scavenger, a character a cell. Its jaws and hold to the left, then a lock, the iron room
/// with the lifepod, a lock and a landing to the right, where the debris comes from; in the hold the piles its last crew gathered (docs/decisions/2026-10-31-the-scoop.md).
/// </summary>
public static class Wreck
{
    /// <summary>The box a world centres, and where the drawing stands in it.</summary>
    public const int Width = 384, Height = 192, Left = 50, Top = 28;

    public static readonly IReadOnlyList<string> Rows = Placed(
    [
        "                                              #####",
        "                                               ###",
        "                                               ###  #  #  #  #",
        "                                               ###  #  #  #  #",
        "                                               ###  #  #  #  #",
        "                                               ###  #  #  #  #",
        "                         ###      ################  ##########",
        "                   #############################################",
        "               ##############.....################################",
        "            #################.....################################",
        "          ##################....ii##....##................##....##",
        "        ##########  ### .###....ii##....##................##....##",
        "      #######           ..........DD....DD..........OOOOOODD....DD",
        "      ##  ###           ..........DD....DD..........OOOOOODD....DD",
        "                        ..........DD....DD..........OOOOOODD....DD",
        "                        .....eee..DD....DD..........OOOOOODD....DD",
        "                        .....eeeffDD....DD..........OOOOOODD....DD",
        "                        .....eeeffDD....DD..........OOOOOODD....DD",
        "                        ..=HH=====######################################",
        "                        ...HH...ii######################################",
        "                        ...HH...ii####################################",
        "                        ...HH.....###################################",
        "                        ...HH.....##",
        "      #                 ...HH.....##",
        "      #######           ...HHaaaa###",
        "      #######           bbbHHaaaa###",
        "       ###########  ### bbbHHaaaa##",
        "         #########  ### bbbHHaaaa##",
        "          #########################",
        "            #######################",
        "              ####################",
        "                  ################",
        "                           ########",
    ]);

    /// <summary>What its walls are of: real iron round the room and its locks, the original plates that show their age, the default hull elsewhere.</summary>
    public static readonly IReadOnlyList<string> Materials = Placed(
    [
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        ".........................www......wwwwwwwwwwwwwwww..wwwwwwwwww............",
        "...................wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww..........",
        "...............wwwwwwwwwwwwww.....wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww........",
        "............................w.....wnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn.........",
        "...........................w......nn....nn................nn....nn........",
        "..................................nn....nn................nn....nn........",
        "......wwww................................................................",
        "......ww..................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "...................................nnnnnnnnnnnnnnnnnnnnnnnnnnnnnn.....ww..",
        "......................................................................ww..",
        ".....................................................................w....",
        "..................................................................www.....",
        "..........................................................................",
        "......w...................................................................",
        "......www.................................................................",
        "......www.................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..........................................................................",
        "..............wwwwwwwwwwwwww..............................................",
        "..................wwwwwwwwww..............................................",
        "..........................................................................",
    ]);

    /// <summary>
    /// How its original walls look, a cell of the drawing each, on walls drawn <c>w</c> in <see cref="Materials"/>: <c>R</c> heavy rust, <c>r</c> thin
    /// rust, <c>H</c> hazard stripes, <c>T</c> torn. Read by the client only (2026-10-31-the-scoop).
    /// </summary>
    public static readonly IReadOnlyList<string> Dress = Placed(
    [
        "",
        "",
        "",
        "",
        "",
        "",
        "                         RRT      TRRRRRRRRRRRRRRR  RRRRRRRRRR",
        "                   RRRRRRRRRTTTTTTRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
        "               rrrrrrrrrrrrrT     Trrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr",
        "                            T     T",
        "                           T",
        "",
        "      HHHH",
        "      HH",
        "",
        "",
        "",
        "",
        "                                                                      HH",
        "                                                                      HH",
        "                                                                     T",
        "                                                                  TTT",
        "",
        "      H",
        "      HHH",
        "      HHH",
        "",
        "",
        "",
        "",
        "              rrrrrrrrrrrrrr",
        "                  RRRRRRRRRR",
        "",
    ]);

    /// <summary>The pipe inside the pod, from its port to a free end: what makes it a first generator.</summary>
    public static readonly IReadOnlyList<string> Under = Placed(
    [
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "                                                    --",
    ]);

    private static string[] Placed(string[] drawing)
    {
        var rows = new string[Height];
        for (int y = 0; y < Height; y++)
            rows[y] = (y >= Top && y < Top + drawing.Length ? new string(' ', Left) + drawing[y - Top] : "").PadRight(Width);
        return rows;
    }
}
