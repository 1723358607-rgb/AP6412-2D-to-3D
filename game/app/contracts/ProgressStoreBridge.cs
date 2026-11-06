using Godot;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.App.Contracts;

/// <summary>Member B implements this in game/progress/. No storage is implemented in step 0.</summary>
public abstract partial class ProgressStoreBridge : Node
{
    public abstract void LoadProgress(string mode);
    public abstract void RecordResult(GDictionary result);
    public abstract int GetBestStars(int levelId);
    public abstract bool IsUnlocked(int levelId);
    public abstract GDictionary GetProgress();
}
