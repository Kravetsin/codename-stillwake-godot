using System;
using System.Collections.Generic;
using System.Text;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The garbage the market shows in place of what is not open ([market-decrypt](../../../../docs/decisions/2026-11-02-market-decrypt.md)): the same at the same place on
/// every read, and only a step of a decipher moves it. Its own hash, never <c>GetHashCode</c>, which differs run to run.
/// </summary>
public static class Cipher
{
    private const string Glyphs = "0123456789abcdef#%&/\\|<>{}=+*~^";

    /// <summary>A number naming a place: a thing's id, or a row's words.</summary>
    public static uint Place(string name)
    {
        uint hash = 11;
        foreach (char c in name) hash = hash * 33 + c;
        return hash;
    }

    /// <summary>The character of garbage at <paramref name="index"/> of <paramref name="place"/>, on <paramref name="step"/> of a decipher.</summary>
    public static char Garble(uint place, int index, int step = 0)
    {
        uint hash = unchecked(place * 31 + (uint)index * 17 + (uint)step * 101 + 7);
        hash = unchecked((hash ^ (hash >> 3)) * 2654435761);
        return Glyphs[(int)((hash >> 7) % (uint)Glyphs.Length)];
    }

    /// <summary><paramref name="text"/> with its first <paramref name="shown"/> characters clear and the rest garbage in place; blanks stay, so a line's shape never jumps.</summary>
    public static string Of(string text, uint place, int shown, int step = 0)
    {
        var made = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++) made.Append(i < shown || text[i] == ' ' ? text[i] : Garble(place, i, step));
        return made.ToString();
    }

    /// <summary>
    /// <paramref name="text"/> broken at blanks into lines of at most <paramref name="columns"/>, at most <paramref name="most"/> of them: clear and garbage wrap alike. A word
    /// too long for its line, or what is left over the last line, is cut with an ellipsis.
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, int columns, int most = int.MaxValue)
    {
        var lines = new List<string>();
        string line = "";
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string next = line.Length == 0 ? word : line + ' ' + word;
            if (next.Length <= columns || line.Length == 0)
            {
                line = next;
                continue;
            }
            lines.Add(line);
            line = word;
        }
        if (line.Length > 0) lines.Add(line);
        if (lines.Count > most)
        {
            lines[most - 1] += ' ' + string.Join(' ', lines.GetRange(most, lines.Count - most));
            lines.RemoveRange(most, lines.Count - most);
        }
        for (int i = 0; i < lines.Count; i++) lines[i] = Trim(lines[i], columns);
        return lines;
    }

    /// <summary><paramref name="text"/>, cut with an ellipsis if it is longer than <paramref name="columns"/>.</summary>
    public static string Trim(string text, int columns) =>
        text.Length <= columns ? text : columns <= 1 ? "…" : text[..(columns - 1)] + '…';
}
