using Godot;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Gameplay;

/// <summary>
/// Stable gameplay boundary for the UI. Step 0 deliberately implements no match-3 rules.
/// The idle snapshot is real bootstrap state, not a fake playable level.
/// </summary>
public partial class GameSession : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(GDictionary snapshot);

    [Signal]
    public delegate void LevelFinishedEventHandler(GDictionary result);

    public bool IsGameplayImplemented => false;
    public string LastRequestNotice { get; private set; } = "Step 0: gameplay is not implemented.";

    private GDictionary _snapshot = CreateIdleSnapshot();

    public GDictionary GetSnapshot() => _snapshot.Duplicate(true);

    public void StartLevel(int levelId)
    {
        if (levelId is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(levelId), "Valid level IDs are 1, 2 and 3.");

        LastRequestNotice = $"Step 0: level {levelId} requested; gameplay is not implemented yet.";
        GD.Print($"[STEP0_NOT_IMPLEMENTED] {LastRequestNotice}");
        // Do not enter playing, deduct moves, or emit a fabricated result.
    }

    public void RetryLevel()
    {
        LastRequestNotice = "Step 0: no active playable session to retry.";
        GD.Print($"[STEP0_NOT_IMPLEMENTED] {LastRequestNotice}");
    }

    public void LeaveLevel()
    {
        _snapshot = CreateIdleSnapshot();
        EmitSignal(SignalName.StateChanged, GetSnapshot());
    }

    private static GDictionary CreateIdleSnapshot() => new()
    {
        ["session_id"] = 0,
        ["level_id"] = 0,
        ["phase"] = "idle",
        ["initial_moves"] = 0,
        ["moves_remaining"] = 0,
        ["target_total"] = 0,
        ["target_remaining"] = 0,
        ["mode"] = "2d",
    };
}
