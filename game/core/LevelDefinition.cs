using System.Text.Json;
using System.Text.Json.Serialization;

namespace AP6412Match3.Core;

/// <summary>Validated, engine-independent level data. It does not unlock or start a session.</summary>
public sealed class LevelDefinition
{
    [JsonPropertyName("level_id")] public int LevelId { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("width")] public int Width { get; init; }
    [JsonPropertyName("height")] public int Height { get; init; }
    [JsonPropertyName("initial_moves")] public int InitialMoves { get; init; }
    [JsonPropertyName("seed")] public uint Seed { get; init; }
    [JsonPropertyName("boxes")] public BoxDefinition[] Boxes { get; init; } = [];

    public static LevelDefinition FromJson(string json)
    {
        var level = JsonSerializer.Deserialize<LevelDefinition>(json)
            ?? throw new JsonException("Missing level definition.");
        level.Validate();
        return level;
    }

    public void Validate()
    {
        if (LevelId is < 1 or > 3 || Width != 7 || Height != 7 || InitialMoves < 1 || Seed == 0)
            throw new ArgumentException("Expected a 7x7 level, ID 1–3, positive moves and nonzero seed.");
        if (Boxes is null || Boxes.Length == 0)
            throw new ArgumentException("The level must define at least one box.");
        var positions = new HashSet<GridPosition>();
        foreach (var box in Boxes)
        {
            if (box.Column < 0 || box.Column >= Width || box.Row < 0 || box.Row >= Height || box.Layers is < 1 or > 2)
                throw new ArgumentException("Invalid box position or layer count.");
            if (!positions.Add(new GridPosition(box.Column, box.Row)))
                throw new ArgumentException("Duplicate box position.");
        }
    }
}

public sealed class BoxDefinition
{
    [JsonPropertyName("column")] public int Column { get; init; }
    [JsonPropertyName("row")] public int Row { get; init; }
    [JsonPropertyName("layers")] public int Layers { get; init; }
}
