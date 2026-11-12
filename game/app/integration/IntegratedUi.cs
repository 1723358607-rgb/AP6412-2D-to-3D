using Godot;
using AP6412Match3.App.Contracts;
using AP6412Match3.Ui;
using GDictionary = Godot.Collections.Dictionary;
namespace AP6412Match3.App.Integration;

/// <summary>Application-owned adapter that reuses B's scenes/art and existing result page.
/// AppController supplies either the persistent ProgressStore or the legacy session-only fallback.</summary>
public partial class IntegratedUi : GameUiBridge
{
    private GameUiRoot _garden=null!;
    private Control _host=null!;
    private Control? _fill;
    public string CurrentPage { get; private set; }="";
    public override void _Ready()
    {
        _garden=GetNode<GameUiRoot>("GardenUi"); _host=_garden.GetNode<Control>("ScreenHost");
        MouseFilter=_garden.MouseFilter=_host.MouseFilter=MouseFilterEnum.Ignore;
        _garden.StartRequested+=id=>EmitSignal(SignalName.StartRequested,id);
        _garden.LevelSelectRequested+=()=>EmitSignal(SignalName.LevelSelectRequested);
        _garden.StartScreenRequested+=()=>EmitSignal(SignalName.StartScreenRequested);
        _garden.LeaveRequested+=()=>EmitSignal(SignalName.LeaveRequested);
        _garden.ModeRequested+=mode=>EmitSignal(SignalName.ModeRequested,mode);
    }
    public Control CurrentScreen()=>_host.GetChild<Control>(0);
    public override void SetDisplayMode(string mode)=>_garden.SetDisplayMode(mode);
    public override void ShowStart() { _garden.ShowStart(); CurrentPage="start"; }
    public override void ShowLevelSelect(GDictionary progress)
    {
        var page=GD.Load<PackedScene>("res://ui/LevelSelectionScreen.tscn").Instantiate<LevelSelectionScreen>();
        page.SetProgressProvider(new SnapshotProgress(progress));
        page.BackRequested+=()=>EmitSignal(SignalName.StartScreenRequested);
        page.LevelRequested+=id=>EmitSignal(SignalName.StartRequested,id);
        Replace(page); CurrentPage="select";
    }
    public override void ShowGame(GDictionary snapshot)
    {
        _garden.ShowGame(snapshot); CurrentPage="game";
        var page=CurrentScreen(); page.MouseFilter=MouseFilterEnum.Ignore;
        page.GetNode<Control>("PauseButton").Hide(); // no agreed pause API: do not ship an inert action
        // Both presenters use the one GameplayScreen progress HUD. Do not create a 3D copy:
        // the shared frame, stars and track remain visible while this is the sole live fill.
        _fill=new Control { Name="ActualGoalFill",Position=new Vector2(682,912),Size=Vector2.Zero,ClipContents=true,MouseFilter=MouseFilterEnum.Ignore };
        page.AddChild(_fill);
        _fill.AddChild(new TextureRect { Texture=GD.Load<Texture2D>("res://assets/CrystalGarden_Gameplay_Assets/progress_fill_full.png"),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            Size=new Vector2(650,52),MouseFilter=MouseFilterEnum.Ignore });
        UpdateHud(snapshot);
    }
    public override void UpdateHud(GDictionary snapshot)
    {
        _garden.UpdateHud(snapshot);
        if (CurrentPage=="game" && _fill is not null && GodotObject.IsInstanceValid(_fill))
        {
            int total=snapshot["target_total"].AsInt32(), remaining=snapshot["target_remaining"].AsInt32();
            _fill.Size=new Vector2(total>0 ? 650f*(total-remaining)/total : 0,52);
        }
    }
    public override void ShowResult(GDictionary result,int bestStars)
    {
        var page=GD.Load<PackedScene>("res://ui/ResultScreen.tscn").Instantiate<ResultScreen>();
        page.Configure(result,bestStars);
        page.RetryRequested+=()=>EmitSignal(SignalName.RetryRequested);
        page.LevelsRequested+=()=>EmitSignal(SignalName.LevelSelectRequested);
        page.NextRequested+=()=>EmitSignal(SignalName.NextRequested);
        Replace(page); CurrentPage="result";
    }
    private void Replace(Control page)
    {
        _fill=null;
        foreach(Node child in _host.GetChildren()) { _host.RemoveChild(child); child.QueueFree(); }
        _host.AddChild(page);
    }
    private sealed class SnapshotProgress : ILevelProgressProvider
    {
        private readonly GDictionary _best;
        public SnapshotProgress(GDictionary progress)=>_best=progress["best_stars"].AsGodotDictionary().Duplicate(true);
        public LevelUiState GetLevel(int levelId)
        {
            int Stars(int id)=>_best.TryGetValue(id.ToString(),out Variant v)?Math.Clamp(v.AsInt32(),0,3):0;
            return new(levelId,Stars(levelId),levelId==1 || Stars(levelId-1)>0);
        }
    }
}
