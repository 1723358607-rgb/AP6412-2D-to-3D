using Godot;
using AP6412Match3.App;
using AP6412Match3.App.Integration;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using AP6412Match3.Presentation;
using AP6412Match3.Ui;

namespace AP6412Match3.Tests.Gameplay;

public static class StepFourSmokeCheck
{
    private static readonly MatchResolver Resolver=new();
    public static async Task<int> Run(AppController app)
    {
        try
        {
            CheckRecovery(); CheckMoveOracle();
            for(int id=1;id<=3;id++)
                GD.Print("[BALANCE_FINAL] "+System.Text.Json.JsonSerializer.Serialize(BalanceStudy.Measure(BalanceStudy.Load(id),128)));
            GD.Print("[BALANCE_OLD_LEVEL2_128] "+System.Text.Json.JsonSerializer.Serialize(BalanceStudy.Measure(BalanceStudy.Load(2,641202),128)));
            app.GetWindow().ContentScaleSize=new Vector2I(1920,1080); app.GetWindow().Size=new Vector2I(1920,1080);
            app.MountOptionalUi();
            var ui=app.GetNode<IntegratedUi>("UiMount/IntegratedUi"); var session=app.GetNode<GameSession>("GameSession");
            await Frames(app,3); Require(ui.CurrentPage=="start","Home did not mount."); await Capture(app,"home");
            Press(ui.CurrentScreen(),"PlayButton");
            Require(ui.CurrentPage=="select" && ((LevelSelectionScreen)ui.CurrentScreen()).IsLevelLocked(2),"Production selection retained demo unlocks.");
            await Capture(app,"levels");
            Press(ui.CurrentScreen(),"LevelCards/Level1Card/ClickTarget"); await Frames(app,2);
            Require(ui.CurrentPage=="game" && Moves(session)==15,"Actual level1 did not start."); await Capture(app,"gameplay");
            BoardView2D view=ui.FindChild("BoardView2D",true,false) as BoardView2D ?? throw new InvalidOperationException("BoardHost not wired.");
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)Require(view.TryGetCellAtLocalPosition(view.CellCenter(new(x,y)),out var p)&&p==new GridPosition(x,y),"Measured grid center mapping failed.");
            // Real viewport dispatch exercises Control mouse filtering, not direct _UnhandledInput calls.
            Click(view,new(0,0));Click(view,new(1,0));await Settled(app,session);
            Require(Moves(session)==15,"Invalid GUI swap consumed a move or GUI swallowed the input.");
            Require(session.LastResolution?.Status==SwapAttemptStatus.NoMatch,"Viewport input did not reach board.");
            Press(ui.CurrentScreen(),"BackButton");Require(((GameplayScreen)ui.CurrentScreen()).IsLeaveDialogOpen,"Leave modal did not open.");
            Click(view,new(1,0));Click(view,new(1,1));await Frames(app,2);Require(Moves(session)==15&&!session.IsBusy,"Modal leaked input to the board.");
            Press(ui.CurrentScreen(),"CancelButton");Require(!((GameplayScreen)ui.CurrentScreen()).IsLeaveDialogOpen&&Moves(session)==15,"Cancel destroyed current game.");
            Press(ui.CurrentScreen(),"SettingsButton");Require(((GameplayScreen)ui.CurrentScreen()).IsSettingsOpen,"Settings failed.");
            Press(ui.CurrentScreen(),"CloseSettingsButton");
            for(int id=1;id<=3;id++)
            {
                var level=BalanceStudy.Load(id);var replay=BalanceStudy.Play(level,997,true);Require(replay.won,"Greedy validation path did not win.");
                foreach(var move in replay.moves)
                {
                    view=(BoardView2D)ui.FindChild("BoardView2D",true,false);
                    int before=Moves(session);Click(view,new(move.from_col,move.from_row));Click(view,new(move.to_col,move.to_row));
                    Require(Moves(session)==before-1,"GUI legal swap failed to deduct exactly once.");
                    Click(view,new(0,0));Click(view,new(1,0));Require(Moves(session)==before-1,"Busy GUI allowed repeated move.");
                    await Settled(app,session);
                    Require(Moves(session)==move.remaining && session.GetSnapshot()["target_remaining"].AsInt32()==move.targets,"Integrated replay diverged.");
                }
                Require(ui.CurrentPage=="result"&&Phase(session)=="won","Real win did not show result page.");
                Require(ui.CurrentScreen().GetNode<Label>("ResultDetail").Text.Contains($"THIS RUN {replay.stars}/3"),"Earned stars were not displayed.");
                GD.Print("[STEP4_REPLAY] "+System.Text.Json.JsonSerializer.Serialize(new { level_id=id,seed=level.Seed,replay }));
                if(id==1) await Capture(app,"result");
                if(id<3) {Press(ui.CurrentScreen(),"NextButton");await Frames(app,2);Require(Moves(session)==BalanceStudy.Load(id+1).InitialMoves,"Next level retained old data.");}
                else Require(ui.CurrentScreen().GetNodeOrNull<Button>("NextButton") is null,"Final level displayed level4.");
            }
            Press(ui.CurrentScreen(),"LevelsButton");await Frames(app,3);
            Require(!((LevelSelectionScreen)ui.CurrentScreen()).IsLevelLocked(3),"Actual wins did not unlock level3.");
            Press(ui.CurrentScreen(),"LevelCards/Level1Card/ClickTarget");await Frames(app,2);
            var loss=Enumerable.Range(1,128).Select(i=>BalanceStudy.Play(BalanceStudy.Load(1),(uint)(1009+i*137),false)).First(r=>!r.won);
            foreach(var move in loss.moves)
            {
                view=(BoardView2D)ui.FindChild("BoardView2D",true,false);
                Click(view,new(move.from_col,move.from_row));Click(view,new(move.to_col,move.to_row));await Settled(app,session);
            }
            Require(ui.CurrentPage=="result" && Phase(session)=="lost" && ui.CurrentScreen().GetNode<Label>("ResultDetail").Text.Contains("THIS RUN 0/3"),"Failure result not shown.");
            Press(ui.CurrentScreen(),"RetryButton");await Frames(app,2);Require(Moves(session)==15&&Phase(session)=="playing","Failure retry failed.");
            Press(ui.CurrentScreen(),"BackButton");Press(ui.CurrentScreen(),"ConfirmLeaveButton");await Frames(app,3);
            Require(ui.CurrentPage=="select"&&session.CurrentBoard is null&&app.GetNodeOrNull<BoardView2D>("BoardView2D") is not null,"Leave lost the board presenter.");
            Press(ui.CurrentScreen(),"BackButton");Require(ui.CurrentPage=="start","Back to home failed.");
            GD.Print("[STEP4_UI_PASS] Reused B home/select/gameplay and art; actual viewport swaps, modal filtering, wins, stars, all three levels, failure/retry/leave and presenter lifetime. Session-only progress, no persistence claim.");
            GD.Print("[STEP4_SMOKE_PASS] Deterministic dead-board recovery, bounded errors, move oracle, balance diagnostics and integrated UI.");
            return 0;
        }
        catch(Exception e) { GD.PushError("[STEP4_SMOKE_FAIL] "+e);return 1; }
    }
    private static void CheckRecovery()
    {
        var cells=new BoardCell[7,7];for(int y=0;y<7;y++)for(int x=0;x<7;x++)cells[x,y]=BoardCell.Piece((PieceColor)((x+y)%4));
        cells[6,6]=BoardCell.Box(2);var board=new BoardModel(cells);Require(Resolver.FindMatches(board).Count==0&&BoardMoves.FindLegal(board).Count==0,"Fixture is not a stable dead board.");
        var generator=new BoardGenerator();var shuffled=generator.EnsurePlayable(board,12345);var replay=generator.EnsurePlayable(board,12345);
        Require(shuffled.Attempts>0&&Dump(shuffled.Board)==Dump(replay.Board)&&shuffled.NextRandomState==replay.NextRandomState,"Shuffle is not deterministic.");
        Require(Resolver.FindMatches(shuffled.Board).Count==0&&BoardMoves.FindLegal(shuffled.Board).Count>0,"Shuffle not stable/playable.");
        int[] Colors(BoardModel b)=>Enumerable.Range(0,4).Select(c=>Enumerable.Range(0,7).Sum(y=>Enumerable.Range(0,7).Count(x=>b.GetCell(new(x,y)) is {Kind:CellKind.Piece} p&&(int)p.Color==c))).ToArray();
        Require(Colors(board).SequenceEqual(Colors(shuffled.Board))&&shuffled.Board.GetCell(new(6,6))==BoardCell.Box(2),"Shuffle changed inventory or box layers.");
        var ready=generator.EnsurePlayable(shuffled.Board,shuffled.NextRandomState);Require(ready.Attempts==0&&ready.NextRandomState==shuffled.NextRandomState,"Playable board consumed shuffle RNG.");
        bool bounded=false;try{generator.EnsurePlayable(new BoardModel(new BoardCell[,]{{BoardCell.Piece(PieceColor.Red),BoardCell.Piece(PieceColor.Blue)},{BoardCell.Piece(PieceColor.Green),BoardCell.Piece(PieceColor.Yellow)}}),123,8);}catch(InvalidOperationException){bounded=true;}
        Require(bounded,"Impossible shuffle was not bounded.");
        var session=new GameSession();session.BeginLevel(new LevelDefinition {LevelId=1,Name="ISOLATED DEAD BOARD",Width=7,Height=7,InitialMoves=15,Seed=12345,Boxes=[new BoxDefinition{Column=6,Row=6,Layers=2}]},board,12345);
        Require(Moves(session)==15&&session.GetSnapshot()["target_remaining"].AsInt32()==1&&BoardMoves.FindLegal(session.CurrentBoard!).Count>0,"Start recovery charged moves or targets.");session.Free();
        // Find a small transaction that becomes dead after refill, then prove recovery is in its step list.
        var smallCells=new BoardCell[4,4];for(int y=0;y<4;y++)for(int x=0;x<4;x++)smallCells[x,y]=BoardCell.Piece((PieceColor)((x+y)%4));
        smallCells[0,0]=smallCells[2,0]=smallCells[1,1]=BoardCell.Piece(PieceColor.Red);smallCells[1,0]=BoardCell.Piece(PieceColor.Blue);smallCells[3,3]=BoardCell.Box(2);
        var small=new BoardModel(smallCells);bool covered=false;
        for(uint state=1;state<=2000&&!covered;state++)
        {
            var raw=Resolver.ResolveSwap(small,new(1,0),new(1,1),state,ensurePlayable:false);
            if(LevelRules.CountBoxes(raw.FinalBoard)==0||BoardMoves.FindLegal(raw.FinalBoard).Count!=0)continue;
            var fixedResult=Resolver.ResolveSwap(small,new(1,0),new(1,1),state);
            Require(fixedResult.Reshuffles==1&&fixedResult.Steps[^1].Kind==BoardStepKind.Reshuffle&&BoardMoves.FindLegal(fixedResult.FinalBoard).Count>0,"Transaction did not recover a dead terminal board.");
            GD.Print($"[STEP4_POST_REFILL_RECOVERY] isolated4x4 rng={state}; reshuffles=1; cascades={fixedResult.Cascades}; stable playable final board.");covered=true;
        }
        Require(covered,"No post-refill recovery coverage found in bounded fixture search.");
        GD.Print($"[STEP4_RECOVERY_PASS] stable dead board; deterministic shuffle attempts={shuffled.Attempts}; colors/box layers/targets/moves preserved; impossible fixture bounded at8.");
    }
    private static void CheckMoveOracle()
    {
        for(int id=1;id<=3;id++)for(uint seed=1;seed<=100;seed++)
        {
            var level=BalanceStudy.Load(id,seed);var board=new BoardGenerator().CreateInitialBoard(level,out uint rng);
            Require(Resolver.FindMatches(board).Count==0,"Opening had a match.");var actual=BoardMoves.FindLegal(board).ToHashSet();Require(actual.Count>0,"Opening had no move.");
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)foreach(var d in new[]{new GridPosition(1,0),new GridPosition(0,1)})
            {
                GridPosition a=new(x,y),b=new(x+d.Column,y+d.Row);if(board.ValidateSwap(a,b)!=SwapValidation.Accepted)continue;
                var cells=board.CopyCells();(cells[a.Column,a.Row],cells[b.Column,b.Row])=(cells[b.Column,b.Row],cells[a.Column,a.Row]);
                var matches=Resolver.FindMatches(new BoardModel(cells));Require(actual.Contains(new(a,b))==(matches.Contains(a)||matches.Contains(b)),"Move finder disagreed with independent swap/triple oracle.");
            }
        }
        GD.Print("[STEP4_OPENINGS_PASS] 300 openings stable/playable; all adjacent move candidates agree with independent swap+match oracle.");
    }
    private static void Click(BoardView2D view,GridPosition p)
    {
        Vector2 point=view.GetGlobalTransformWithCanvas()*view.CellCenter(p);
        view.GetViewport().PushInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=point,GlobalPosition=point},true);
        view.GetViewport().PushInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false,Position=point,GlobalPosition=point},true);
    }
    private static void Press(Control root,string name)
    {
        var b=(BaseButton)(name.Contains('/')?root.GetNode(name):root.FindChild(name,true,false));Require(!b.Disabled,"Button disabled: "+name);b.EmitSignal(BaseButton.SignalName.Pressed);
    }
    private static async Task Settled(Node app,GameSession s)
    {
        for(int frame=0;frame<2400&&s.IsBusy;frame++)await Frames(app,1);Require(!s.IsBusy,"Animation/transaction timed out.");await Frames(app,1);
    }
    private static async Task Frames(Node n,int count){for(int i=0;i<count;i++)await n.ToSignal(n.GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static async Task Capture(Node app,string name)
    {
        if(!OS.GetCmdlineUserArgs().Contains("--capture-step4"))return;
        await Frames(app,2);RenderingServer.ForceDraw();await Frames(app,1);
        string dir=ProjectSettings.GlobalizePath("res://../docs/evidence/member-a/screenshots");DirAccess.MakeDirRecursiveAbsolute(dir);
        Error error=app.GetViewport().GetTexture().GetImage().SaveJpg(dir+"/step4-"+name+".jpg",0.88f);Require(error==Error.Ok,"Screenshot save failed.");
        GD.Print("[STEP4_CAPTURE] "+name);
    }
    private static string Dump(BoardModel b)=>string.Join("/",Enumerable.Range(0,b.Height).Select(y=>string.Join("",Enumerable.Range(0,b.Width).Select(x=>b.GetCell(new(x,y)).ToString()))));
    private static int Moves(GameSession s)=>s.GetSnapshot()["moves_remaining"].AsInt32();
    private static string Phase(GameSession s)=>s.GetSnapshot()["phase"].AsString();
    private static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
}
