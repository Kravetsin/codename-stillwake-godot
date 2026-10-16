using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stillwake.Sim;

/// <summary>
/// A layer of cells saved as runs, <c>[value, count, value, count, …]</c>: a save, its hash and a
/// welcome then cost what is built, not the area, since space is one long run.
/// </summary>
public sealed class Runs : JsonConverter<int[]>
{
    public override int[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Runs are an array.");
        var cells = new List<int>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            int value = reader.GetInt32();
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number) throw new JsonException("A run without its count.");
            int count = reader.GetInt32();
            if (count <= 0) throw new JsonException($"A run of {count}.");
            for (int i = 0; i < count; i++) cells.Add(value);
        }
        return [.. cells];
    }

    public override void Write(Utf8JsonWriter writer, int[] cells, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int start = 0; start < cells.Length;)
        {
            int end = RunEnd(cells, start);
            writer.WriteNumberValue(cells[start]);
            writer.WriteNumberValue(end - start);
            start = end;
        }
        writer.WriteEndArray();
    }

    /// <summary>A layer of a save read as JSON, for code that edits a save: a migration step, a dev world.</summary>
    public static int[] Cells(JsonNode runs)
    {
        var flat = runs.AsArray().Select(n => (int)n!).ToArray();
        var cells = new List<int>();
        for (int i = 0; i + 1 < flat.Length; i += 2) cells.AddRange(Enumerable.Repeat(flat[i], flat[i + 1]));
        return [.. cells];
    }

    /// <summary>A layer as a save holds it, for code that edits a save.</summary>
    public static JsonArray Node(int[] cells)
    {
        var runs = new JsonArray();
        for (int start = 0; start < cells.Length;)
        {
            int end = RunEnd(cells, start);
            runs.Add(cells[start]);
            runs.Add(end - start);
            start = end;
        }
        return runs;
    }

    /// <summary>Where the run starting at <paramref name="start"/> ends: on a span, since a hash a second walks every cell.</summary>
    private static int RunEnd(ReadOnlySpan<int> cells, int start)
    {
        int at = cells[(start + 1)..].IndexOfAnyExcept(cells[start]);
        return at < 0 ? cells.Length : start + 1 + at;
    }
}
