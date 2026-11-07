using Godot;
using AP6412Match3.App.Contracts;
using AP6412Match3.Ui;
using GDictionary = Godot.Collections.Dictionary;
namespace AP6412Match3.App.Integration;

/// <summary>Application-owned adapter: reuses B's scenes/art without editing their files.
/// The formal B UI/store supersede this session-only fallback when ProgressStore.tscn exists.</summary>
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
    }
    public Control CurrentScreen()=>_host.GetChild<Control>(0);
    public override void ShowStart() { _garden.ShowStart(); CurrentPage="start"; Notice(CurrentScreen()); }
    public override void ShowLevelSelect(GDictionary progress)
    {
        var page=GD.Load<PackedScene>("res://ui/LevelSelectionScreen.tscn").Instantiate<LevelSelectionScreen>();
        page.SetProgressProvider(new SnapshotProgress(progress));
        page.BackRequested+=()=>EmitSignal(SignalName.StartScreenRequested);
        page.LevelRequested+=id=>EmitSignal(SignalName.StartRequested,id);
        Replace(page); CurrentPage="select"; Notice(page);
    }
    public override void ShowGame(GDictionary snapshot)
    {
        _garden.ShowGame(snapshot); CurrentPage="game";
        var page=CurrentScreen(); page.MouseFilter=MouseFilterEnum.Ignore;
        page.GetNode<Control>("PauseButton").Hide(); // no agreed pause API: do not ship an inert action
        _fill=new Control { Name="ActualGoalFill",Position=new Vector2(682,912),Size=Vector2.Zero,ClipContents=true,MouseFilter=MouseFilterEnum.Ignore };
        page.AddChild(_fill);
        _fill.AddChild(new TextureRect { Texture=GD.Load<Texture2D>("res://assets/CrystalGarden_Gameplay_Assets/progress_fill_full.png"),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            Size=new Vector2(650,52),MouseFilter=MouseFilterEnum.Ignore });
        Notice(page); UpdateHud(snapshot);
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
        bool won=result["won"].AsBool(); int level=result["level_id"].AsInt32(),stars=result["stars"].AsInt32();
        var page=new Control { Name="ResultPage",MouseFilter=MouseFilterEnum.Ignore }; Replace(page); CurrentPage="result";
        CrystalGardenUi.FullRect(page);
        CrystalGardenUi.Texture("GardenBackground","backgrounds/garden_background_16x9.png",page,new Rect2(0,0,1920,1080))
            .StretchMode=TextureRect.StretchModeEnum.KeepAspectCovered;
        var panel=new Panel { Position=new Vector2(510,190),Size=new Vector2(900,700),MouseFilter=MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat { BgColor=new Color("543375"),BorderColor=new Color("d8b980"),
            CornerRadiusTopLeft=34,CornerRadiusTopRight=34,CornerRadiusBottomLeft=34,CornerRadiusBottomRight=34,
            BorderWidthLeft=3,BorderWidthRight=3,BorderWidthTop=3,BorderWidthBottom=3 }); page.AddChild(panel);
        CrystalGardenUi.Label("ResultTitle",won ? level==3 ? "GARDEN COMPLETE!" : "LEVEL COMPLETE!" : "TRY AGAIN",page,new Rect2(560,235,800,100),52);
        for(int i=0;i<3;i++) CrystalGardenUi.Texture($"EarnedStar{i+1}",i<stars?"levels/star_earned.png":"levels/star_unearned.png",page,new Rect2(746+i*150,355,130,130));
        CrystalGardenUi.Label("ResultDetail",$"LEVEL {level}   ·   MOVES LEFT {result["moves_remaining"]}\nTHIS RUN {stars}/3   ·   BEST {bestStars}/3",page,new Rect2(560,505,800,120),30);
        Button(page,"RetryButton","RETRY",new Rect2(600,660,330,78),()=>EmitSignal(SignalName.RetryRequested));
        Button(page,"LevelsButton","LEVELS",new Rect2(990,660,330,78),()=>EmitSignal(SignalName.LevelSelectRequested));
        if(won && level<3) Button(page,"NextButton","NEXT LEVEL",new Rect2(795,770,330,78),()=>EmitSignal(SignalName.NextRequested));
        Notice(page);
    }
    private void Replace(Control page)
    {
        _fill=null;
        foreach(Node child in _host.GetChildren()) { _host.RemoveChild(child); child.QueueFree(); }
        _host.AddChild(page);
    }
    private static void Notice(Control page)=>CrystalGardenUi.Label("RuntimeProgressNotice","SESSION ONLY · PROGRESS RESETS WHEN CLOSED",page,new Rect2(510,1025,900,35),18);
    private static void Button(Control page,string name,string text,Rect2 rect,Action action)
    {
        var button=new Button { Name=name,Text=text,Position=rect.Position,Size=rect.Size,FocusMode=FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size",25); button.Pressed+=action; page.AddChild(button);
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
