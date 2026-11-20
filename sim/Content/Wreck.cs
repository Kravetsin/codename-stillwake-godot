namespace Stillwake.Content;

/// <summary>
/// The ship a new world starts in: the torn-off scoop of a scavenger, a character a cell. Its jaws and hold to the left, then a lock, the iron room
/// with the lifepod, a lock and a landing to the right, where the debris comes from; the alien core that wrecked it fell steeply through the roof over
/// the landing, its torn end left hanging on the back wall, ploughed the landing and stopped in its floor against the outer lock (drone-companion);
/// in the hold the piles its last crew gathered (docs/decisions/2026-10-31-the-scoop.md).
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
        "                                               ###  #  #  #  #        #####....##",
        "                         ###      ################  ##########    ########....#######",
        "                   #############################################  #######....########",
        "               ##############.....######################################....#########",
        "            #################.....#####################################....##########",
        "          ##################....ii##....##................##....##",
        "        ##########  ### .###....ii##....##................##....##",
        "      #######           ..........DD....DD..........OOOOOODD....DD",
        "      ##  ###           ..........DD....DD..........OOOOOODD....DD",
        "                        ..........DD....DD..........OOOOOODD....DD",
        "                        .....eee..DD....DD..........OOOOOODD....DD",
        "                        .....eeeffDD....DD..........OOOOOODD....DD",
        "                        .....eeeffDD....DD..........OOOOOODD....DD??",
        "                        ..=HH=====################################??  ##",
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
        "",
        "",
        "",
        "",
        "",
        "......................................................................wwwwwwwwwww",
        ".........................www......wwwwwwwwwwwwwwww..wwwwwwwwww....wwwwwwwwwwwwwwwwwww",
        "...................wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww..wwwwwwwwwwwwwwwwwww",
        "...............wwwwwwwwwwwwww.....wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
        "............................w.....wnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn.wwwwwwwwwwwwwwwwwww",
        "...........................w......nn....nn................nn....nn",
        "..................................nn....nn................nn....nn",
        "......wwww",
        "......ww",
        "",
        "",
        "",
        "",
        "...................................nnnnnnnnnnnnnnnnnnnnnnnnnnnnnn.....ww",
        "......................................................................ww",
        ".....................................................................w",
        "..................................................................www",
        "",
        "......w",
        "......www",
        "......www",
        "",
        "",
        "",
        "",
        "..............wwwwwwwwwwwwww",
        "..................wwwwwwwwww",
        "",
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
        "                                                                      rrrrT    Tr",
        "                         RRT      TRRRRRRRRRRRRRRR  RRRRRRRRRR    rrrrrrrT    Trrrrrr",
        "                   RRRRRRRRRTTTTTTRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR  rrrrrrT    Trrrrrrr",
        "               rrrrrrrrrrrrrT     TrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrT    Trrrrrrrr",
        "                            T     T                               rrrrT    Trrrrrrrrr",
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

    /// <summary>
    /// The channel the alien core punched through the roof over the landing, a row of the drawing each with its first and last column, drawn
    /// <c>.</c> in <see cref="Rows"/>: where the client scorches the plates round it. Read by the client only (drone-companion).
    /// </summary>
    public static readonly IReadOnlyList<(int Row, int First, int Last)> CorePath =
    [
        (5, 75, 78),
        (6, 74, 77),
        (7, 73, 76),
        (8, 72, 75),
        (9, 71, 74),
    ];

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
        "                                                        --",
    ]);

    private static string[] Placed(string[] drawing)
    {
        var rows = new string[Height];
        for (int y = 0; y < Height; y++)
            rows[y] = (y >= Top && y < Top + drawing.Length ? new string(' ', Left) + drawing[y - Top] : "").PadRight(Width);
        return rows;
    }
}
