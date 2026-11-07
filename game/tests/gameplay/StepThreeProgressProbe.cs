using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>In-memory integration test double only. It is not B's persistent ProgressStore.</summary>
public partial class StepThreeProgressProbe : ProgressStoreBridge
{
    private readonly int[] _best = new int[3];
    public int RecordCalls { get; private set; }
    public string LoadedMode { get; private set; } = "";
    public override void LoadProgress(string mode) => LoadedMode = mode;
    public override void RecordResult(GDictionary result)
    {
        RecordCalls++;
        if (result["won"].AsBool())
        {
            int level = result["level_id"].AsInt32();
            _best[level - 1] = Math.Max(_best[level - 1], result["stars"].AsInt32());
        }
    }
    public override int GetBestStars(int levelId) => _best[levelId - 1];
    public override bool IsUnlocked(int levelId) => levelId is >= 1 and <= 3 && (levelId == 1 || _best[levelId - 2] > 0);
    public override GDictionary GetProgress() => new()
    {
        ["schema_version"] = 1, ["mode"] = LoadedMode,
        ["best_stars"] = new GDictionary { ["1"] = _best[0], ["2"] = _best[1], ["3"] = _best[2] },
    };
}
