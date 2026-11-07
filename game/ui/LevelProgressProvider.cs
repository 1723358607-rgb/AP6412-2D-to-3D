namespace AP6412Match3.Ui;

public readonly record struct LevelUiState(int LevelId, int BestStars, bool IsUnlocked);

public interface ILevelProgressProvider
{
    LevelUiState GetLevel(int levelId);
}

/// <summary>
/// Visual-validation data only. Replace with a ProgressStore-backed provider when
/// persistence is implemented; no demo value is baked into the artwork.
/// </summary>
public sealed class DemoLevelProgressProvider : ILevelProgressProvider
{
    public LevelUiState GetLevel(int levelId) => levelId switch
    {
        1 => new LevelUiState(1, 3, true),
        2 => new LevelUiState(2, 2, true),
        3 => new LevelUiState(3, 0, false),
        _ => throw new ArgumentOutOfRangeException(nameof(levelId)),
    };
}
