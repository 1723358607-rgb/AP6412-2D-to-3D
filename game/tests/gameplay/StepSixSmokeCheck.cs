using Godot;
using System.Text.Json;
using AP6412Match3.App;
using AP6412Match3.App.Contracts;
using AP6412Match3.App.Integration;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using AP6412Match3.Presentation;
using AP6412Match3.Progress;
using AP6412Match3.Ui;
namespace AP6412Match3.Tests.Gameplay;

/// <summary>Actual 3D presenter, camera rays, GUI input, archived rules and per-mode saves.</summary>
public static class StepSixSmokeCheck
{
    public static async Task<int> Run(AppController app)
    {
        try
        {
            Require(app.DisplayMode=="3d","Use Main3D.tscn for 3D verification.");
            string directory=OS.GetEnvironment("AP6412_PROGRESS_STORE_DIR");
            Require(!string.IsNullOrWhiteSpace(directory),"3D checks require isolated saves.");
            if(OS.GetCmdlineUserArgs().Contains("--3d-restart-read")) return RestartRead();
            Require(!System.IO.File.Exists(ProgressStore.StoragePath("3d")),"3D test directory must be fresh.");
            var old=new ProgressStore();
            old.LoadProgress("2d");old.RecordResult(new Godot.Collections.Dictionary { ["mode"]="2d",["level_id"]=1,["won"]=true,["stars"]=2 });old.Free();
            string twoD=System.IO.File.ReadAllText(ProgressStore.StoragePath("2d"));
            string root=System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),".."));
            using var index=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root,"docs/evidence/member-a/current-rule-reference.json")));
            using var archive=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root,index.RootElement.GetProperty("reference_path").GetString()!)));
            var session=app.GetNode<GameSession>("GameSession");var view=app.GetNode<BoardView3D>("BoardView3D");
            Require(session.GetSnapshot()["mode"].AsString()=="3d","Session mode was not configured.");
            app.GetWindow().ContentScaleSize=new Vector2I(1920,1080);app.GetWindow().Size=new Vector2I(1920,1080);
            app.MountOptionalUi();await Frames(app,3);
            var ui=app.GetNode<IntegratedUi>("UiMount/IntegratedUi");Press(ui,"PlayButton");await Frames(app,2);
            Require(((LevelSelectionScreen)ui.CurrentScreen()).IsLevelLocked(2),"3D inherited the 2D unlock.");
            Press(ui,"LevelCards/Level1Card/ClickTarget");await Frames(app,3);
            Require(view.RenderedCellCount==49 && view.PresentedBoard is not null,"3D meshes not populated.");
            foreach(var p in Enumerable.Range(0,49).Select(i=>new GridPosition(i%7,i/7)))
            {
                Require(view.TryPick(view.CellCanvasCenter(p),out var picked)&&picked==p,$"3D ray center mismatch at {p}.");
                Vector3 size=view.MeshBounds(p).Size;Require(size.X>.1f&&size.Y>.1f&&size.Z>.1f,"A piece is a flat impostor.");
            }
            Require(!view.TryPick(new Vector2(-20,-20),out _),"3D picked outside its viewport.");
            app.GetWindow().Size=new Vector2I(1280,720);await Frames(app,3);
            foreach(var p in Enumerable.Range(0,49).Select(i=>new GridPosition(i%7,i/7)))
                Require(view.TryPick(view.CellCanvasCenter(p),out var picked)&&picked==p,"Resized 3D ray mapping failed.");
            app.GetWindow().Size=new Vector2I(1920,1080);await Frames(app,3);
            var box=BalanceStudy.Load(1).Boxes[0];Click(view,new(box.Column,box.Row));await Frames(app,1);
            Require(view.Interaction!.Selected is null && Moves(session)==15,"Box selection consumed a move.");
            var legal=BoardMoves.FindLegal(session.CurrentBoard!).ToHashSet();SwapRequest? invalid=null;
            for(int y=0;y<7&&invalid is null;y++)for(int x=0;x<6;x++)
            {var q=new SwapRequest(new(x,y),new(x+1,y));if(session.CurrentBoard!.ValidateSwap(q.From,q.To)==SwapValidation.Accepted&&!legal.Contains(q)){invalid=q;break;}}
            Require(invalid is not null,"No rollback pair.");Click(view,invalid!.Value.From);Click(view,invalid.Value.To);await Settled(app,session);
            Require(Moves(session)==15&&session.LastResolution?.Status==SwapAttemptStatus.NoMatch,"3D invalid swap failed rollback.");
            GD.Print("[STEP6_RAY_PASS] Actual camera projection/ray versus 49 three-dimensional cell volumes, solid meshes, box rejection and viewport rollback.");

            var presented=new List<BoardStep>();view.StepPresented+=presented.Add;int moves=0,steps=0;
            foreach(var level in archive.RootElement.GetProperty("levels").EnumerateArray())
            {
                int id=level.GetProperty("level_id").GetInt32();
                Require(Rows(session.CurrentBoard!).SequenceEqual(Strings(level.GetProperty("initial").GetProperty("grid"))),"3D opening differs from 2D.");
                foreach(var move in level.GetProperty("moves").EnumerateArray())
                {
                    presented.Clear();int before=Moves(session);
                    var from=Position(move.GetProperty("from"));var to=Position(move.GetProperty("to"));
                    Click(view,from);Click(view,to);
                    Require(Moves(session)==before-1&&session.IsBusy,"3D viewport swap did not reach the session.");
                    Click(view,from);Click(view,to);Require(Moves(session)==before-1,"3D busy input charged twice.");
                    await Settled(app,session);
                    var state=move.GetProperty("state");
                    Require(Moves(session)==state.GetProperty("moves_remaining").GetInt32()
                        && session.GetSnapshot()["target_remaining"].AsInt32()==state.GetProperty("target_remaining").GetInt32()
                        && session.LastResolution!.NextRandomState==state.GetProperty("random_state").GetUInt32()
                        && Rows(session.CurrentBoard!).SequenceEqual(Strings(state.GetProperty("grid"))),"3D stable board/moves/target/RNG diverged.");
                    var expected=move.GetProperty("steps").EnumerateArray().ToArray();Require(presented.Count==expected.Length,"3D omitted a presentation step.");
                    for(int i=0;i<expected.Length;i++)
                    {
                        Require(presented[i].Kind.ToString()==expected[i].GetProperty("kind").GetString()
                            && Rows(presented[i].After).SequenceEqual(Strings(expected[i].GetProperty("grid")))
                            && Positions(presented[i].Cleared)==Positions(expected[i].GetProperty("cleared"))
                            && Positions(presented[i].DamagedBoxes??[])==Positions(expected[i].GetProperty("damaged_boxes")),"3D intermediate state or box damage diverged.");
                    }
                    moves++;steps+=presented.Count;
                }
                Require(ui.CurrentPage=="result"&&session.GetSnapshot()["phase"].AsString()=="won","3D result navigation failed.");
                Require(ui.CurrentScreen().GetNode<Label>("RunPanelValue").Text=="3/3","3D result stars differ.");
                if(id<3){Press(ui,"NextButton");await Frames(app,3);}
                else Require(ui.CurrentScreen().GetNodeOrNull<Button>("NextButton") is null,"3D exposed level4.");
            }
            GD.Print($"[STEP6_EQUIVALENCE_PASS] 3 real 3D levels, {moves} swaps and {steps} animated presenter steps agree with 2D grid/damage/moves/target/RNG/results.");

            Press(ui,"LevelsButton");await Frames(app,3);Press(ui,"LevelCards/Level1Card/ClickTarget");await Frames(app,3);
            var loss=Enumerable.Range(0,128).Select(i=>BalanceStudy.Play(BalanceStudy.Load(1),(uint)(1009+i*137),false)).First(p=>!p.won);
            foreach(var move in loss.moves)
            {
                Click(view,new(move.from_col,move.from_row));Click(view,new(move.to_col,move.to_row));await Settled(app,session);
            }
            Require(ui.CurrentPage=="result"&&session.GetSnapshot()["phase"].AsString()=="lost"
                && ui.CurrentScreen().GetNode<Label>("RunPanelValue").Text=="0/3"
                && ui.CurrentScreen().GetNode<Label>("BestValue").Text=="3/3","3D failure/previous best result incorrect.");
            Press(ui,"RetryButton");await Frames(app,3);Require(Moves(session)==15&&!session.IsBusy,"3D failure retry failed.");
            GD.Print("[STEP6_FAILURE_PASS] Actual 3D loss reports zero earned stars, preserves the best three stars and retries the same level.");
            var first=archive.RootElement.GetProperty("levels")[0].GetProperty("moves")[0];
            Click(view,Position(first.GetProperty("from")));Click(view,Position(first.GetProperty("to")));
            ui.EmitSignal(GameUiBridge.SignalName.RetryRequested);await Frames(app,5);
            Require(Moves(session)==15&&!session.IsBusy&&view.RenderedCellCount==49,"3D retry retained cancelled animation.");
            Press(ui,"BackButton");Require(((GameplayScreen)ui.CurrentScreen()).IsLeaveDialogOpen,"3D leave dialog missing.");
            Click(view,Position(first.GetProperty("from")));Click(view,Position(first.GetProperty("to")));await Frames(app,2);
            Require(Moves(session)==15&&!session.IsBusy,"3D modal input leaked.");Press(ui,"CancelButton");
            Click(view,Position(first.GetProperty("from")));Click(view,Position(first.GetProperty("to")));
            Press(ui,"BackButton");Press(ui,"ConfirmLeaveButton");await Frames(app,5);
            Require(ui.CurrentPage=="select"&&session.CurrentBoard is null&&view.PresentedBoard is null,"3D leave did not cancel/clear.");
            Press(ui,"LevelCards/Level1Card/ClickTarget");await Frames(app,3);
            Require(view.RenderedCellCount==49&&view.TryPick(view.CellCanvasCenter(new(0,0)),out _),"Page disposal destroyed the 3D surface.");
            Require(System.IO.File.ReadAllText(ProgressStore.StoragePath("2d"))==twoD,"3D changed the 2D save.");
            GD.Print("[STEP6_LIFETIME_PASS] Retry/leave cancel animations; modal blocks actual viewport clicks; disposable pages preserve 3D surface; 2D save unchanged.");
            GD.Print("[STEP6_SMOKE_PASS] Real meshes/camera/rays/animations/UI and separate 3D progress pass. Rendered image quality and human play remain unverified.");
            return 0;
        }
        catch(Exception e){GD.PushError("[STEP6_SMOKE_FAIL] "+e);return 1;}
    }
    private static int RestartRead()
    {
        var store=new ProgressStore();
        try
        {
            store.LoadProgress("3d");Require(Enumerable.Range(1,3).All(i=>store.GetBestStars(i)==3&&store.IsUnlocked(i)),"3D progress did not survive a process restart.");
            store.LoadProgress("2d");Require(store.GetBestStars(1)==2&&store.GetBestStars(2)==0&&!store.IsUnlocked(3),"3D progress contaminated 2D.");
            GD.Print("[STEP6_RESTART_PASS] Separate process restores 3D bests/unlocks while 2D remains independent.");return 0;
        }
        finally{store.Free();}
    }
    private static int Moves(GameSession s)=>s.GetSnapshot()["moves_remaining"].AsInt32();
    private static GridPosition Position(JsonElement e)=>new(e[0].GetInt32(),e[1].GetInt32());
    private static string[] Strings(JsonElement e)=>e.EnumerateArray().Select(x=>x.GetString()!).ToArray();
    private static string Positions(IEnumerable<GridPosition> ps)=>string.Join(";",ps.Select(p=>$"{p.Column},{p.Row}"));
    private static string Positions(JsonElement e)=>Positions(e.EnumerateArray().Select(Position));
    private static string[] Rows(BoardModel board)=>Enumerable.Range(0,board.Height).Select(y=>new string(Enumerable.Range(0,board.Width).Select(x=>board.GetCell(new(x,y)) switch {
        {Kind:CellKind.Box} b=>(char)('0'+b.BoxLayers),{Kind:CellKind.Piece} p=>"RBGY"[(int)p.Color],_=>'.' }).ToArray())).ToArray();
    private static void Click(BoardView3D view,GridPosition p)=>view.GetViewport().PushInput(new InputEventMouseButton {
        Pressed=true,ButtonIndex=MouseButton.Left,Position=view.CellCanvasCenter(p) },true);
    private static void Press(IntegratedUi ui,string name)
    {
        var root=ui.CurrentScreen();var button=(BaseButton)(name.Contains('/')?root.GetNode(name):root.FindChild(name,true,false));
        Require(!button.Disabled,"Disabled navigation button.");button.EmitSignal(BaseButton.SignalName.Pressed);
    }
    private static async Task Frames(Node node,int count){for(int i=0;i<count;i++)await node.ToSignal(node.GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static async Task Settled(Node node,GameSession session){for(int i=0;i<2400&&session.IsBusy;i++)await Frames(node,1);Require(!session.IsBusy,"3D animation timeout.");await Frames(node,2);}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
