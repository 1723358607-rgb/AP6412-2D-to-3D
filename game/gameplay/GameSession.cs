using Godot;

namespace AP6412Match3.Gameplay;

/// <summary>
/// Public gameplay boundary consumed by the UI. The exact snapshot/result fields
/// remain governed by docs/alignment/CONTRACT.md.
/// </summary>
public partial class GameSession : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(Godot.Collections.Dictionary snapshot);

    [Signal]
    public delegate void LevelFinishedEventHandler(Godot.Collections.Dictionary result);

    public void StartLevel(int levelId)
    {
        GD.PushWarning("StartLevel is reserved for a later gameplay step.");
    }

    public void RetryLevel()
    {
        GD.PushWarning("RetryLevel is reserved for a later gameplay step.");
    }

    public void LeaveLevel()
    {
        GD.PushWarning("LeaveLevel is reserved for a later gameplay step.");
    }

    public Godot.Collections.Dictionary GetSnapshot()
    {
        return new Godot.Collections.Dictionary
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
}
