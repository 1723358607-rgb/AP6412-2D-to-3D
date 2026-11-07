using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;
namespace AP6412Match3.App.Integration;

/// <summary>Actual earned progress for this process only, until B provides the persistent store scene.</summary>
public partial class RuntimeProgressBridge : ProgressStoreBridge
{
    private readonly int[] _best = new int[3];
    private string _mode = "2d";
    public override void LoadProgress(string mode)
    {
        if (mode is not ("2d" or "3d")) throw new ArgumentException(nameof(mode));
        if (_mode != mode) Array.Clear(_best);
        _mode = mode;
    }
    public override void RecordResult(GDictionary result)
    {
        int level=result["level_id"].AsInt32(), stars=result["stars"].AsInt32();
        if (level is <1 or >3 || stars is <0 or >3 || result["mode"].AsString()!=_mode) throw new ArgumentException("Invalid result.");
        if (result["won"].AsBool() && stars>0) _best[level-1]=Math.Max(_best[level-1],stars);
    }
    public override int GetBestStars(int levelId) => levelId is >=1 and <=3 ? _best[levelId-1] : 0;
    public override bool IsUnlocked(int levelId) => levelId is >=1 and <=3 && (levelId==1 || _best[levelId-2]>0);
    public override GDictionary GetProgress() => new()
    {
        ["schema_version"]=1,["mode"]=_mode,
        ["best_stars"]=new GDictionary { ["1"]=_best[0],["2"]=_best[1],["3"]=_best[2] },
    };
}
