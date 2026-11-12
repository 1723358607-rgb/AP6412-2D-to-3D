using Godot;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.App.Contracts;

/// <summary>
/// Member A owns this connection contract. Member B implements GameUiRoot in game/ui/.
/// Use inherited Godot signals; do not redeclare them with incompatible C# delegates.
/// </summary>
public abstract partial class GameUiBridge : Control
{
    [Signal] public delegate void StartRequestedEventHandler(int levelId);
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void NextRequestedEventHandler();
    [Signal] public delegate void LeaveRequestedEventHandler();
    [Signal] public delegate void LevelSelectRequestedEventHandler();
    [Signal] public delegate void StartScreenRequestedEventHandler();
    [Signal] public delegate void ModeRequestedEventHandler(string mode);

    public virtual void SetDisplayMode(string mode) { }
    public abstract void ShowStart();
    public abstract void ShowLevelSelect(GDictionary progress);
    public abstract void ShowGame(GDictionary snapshot);
    public abstract void UpdateHud(GDictionary snapshot);
    public abstract void ShowResult(GDictionary result, int bestStars);
}
