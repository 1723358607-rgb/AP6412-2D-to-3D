using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using AP6412Match3.Presentation;
using AP6412Match3.App;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Real rules, cancellation and scene integration. Headless checks do not prove rendered appearance.</summary>
public static class StepTwoSmokeCheck
{
    private static readonly MatchResolver Resolver = new();
    private static readonly GridPosition OpeningFrom = new(1, 0), OpeningTo = new(1, 1);

    public static async Task<int> Run(Node host)
    {
        GameSession? session = null;
        GameplayLoopRunner? harness = null;
        try
        {
            CheckFixtures();
            var level = LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString(BoardPrototypeRunner.LevelPath));
            var generator = new BoardGenerator();
            BoardModel opening = generator.CreateInitialBoard(level, out uint random);
            string initial = Dump(opening);
            var accepted = Resolver.ResolveSwap(opening, OpeningFrom, OpeningTo, random);
            Require(accepted.Status == SwapAttemptStatus.Accepted, "Guaranteed opening rejected.");
            Require(Dump(opening) == initial, "Resolver mutated its input.");
            var replay = Resolver.ResolveSwap(opening, OpeningFrom, OpeningTo, random);
            Require(Dump(accepted.FinalBoard) == Dump(replay.FinalBoard) && accepted.NextRandomState == replay.NextRandomState,
                "Same board, swap and RNG must reproduce final state.");
            CheckStable(accepted.FinalBoard, opening);
            GD.Print($"[STEP2_OPENING] seed={level.Seed}; from=(1,0); to=(1,1); moves=15->14; cascades={accepted.Cascades}; cleared={accepted.TotalCleared}; before={initial}; after={Dump(accepted.FinalBoard)}");

            int chainSamples = 0;
            for (uint seed = 1; seed <= 100; seed++)
            {
                var sample = new LevelDefinition { LevelId = 1, Width = 7, Height = 7, InitialMoves = 15, Seed = seed, Boxes = level.Boxes };
                var board = generator.CreateInitialBoard(sample, out uint state);
                var resolved = Resolver.ResolveSwap(board, OpeningFrom, OpeningTo, state);
                Require(resolved.Status == SwapAttemptStatus.Accepted, $"Seed {seed} opening rejected.");
                CheckStable(resolved.FinalBoard, board);
                if (resolved.Cascades > 1) chainSamples++;
            }
            Require(chainSamples > 0, "No cascade coverage in 100 samples.");
            GD.Print($"[STEP2_CORE_PASS] fixtures and 100 deterministic openings; multi-wave samples={chainSamples}.");

            session = new GameSession();
            host.AddChild(session);
            var states = new List<string>();
            int results = 0;
            GDictionary? lastResult = null;
            session.StateChanged += snapshot => states.Add(snapshot["phase"].AsString());
            session.LevelFinished += result => { results++; lastResult = result; };
            session.StartLevel(1);
            var pair = FindPair(session.CurrentBoard!, wantMatch: false);
            string beforeInvalid = Dump(session.CurrentBoard!);
            Require(await session.RequestSwapAsync(pair.From, pair.To) == SwapAttemptStatus.NoMatch, "Invalid match accepted.");
            Require(Moves(session) == 15 && Dump(session.CurrentBoard!) == beforeInvalid, "Invalid swap changed board/moves.");
            GD.Print($"[STEP2_INVALID] from={pair.From}; to={pair.To}; moves=15->15; board unchanged; RNG not consumed.");
            Require(await session.RequestSwapAsync(new(-1, 0), new(0, 0)) == SwapAttemptStatus.Invalid, "Outside swap accepted.");
            var box = level.Boxes[0];
            Require(await session.RequestSwapAsync(new(box.Column, box.Row), new(box.Column, box.Row + 1)) == SwapAttemptStatus.Invalid, "Box swap accepted.");
            Require(await session.RequestSwapAsync(new(0, 0), new(1, 1)) == SwapAttemptStatus.Invalid, "Diagonal accepted.");
            Require(await session.RequestSwapAsync(OpeningFrom, OpeningTo) == SwapAttemptStatus.Accepted, "Session rejected opening.");
            Require(Moves(session) == 14 && Dump(session.CurrentBoard!) == Dump(accepted.FinalBoard), "Session/RNG differs after invalid swap.");
            Require(states.Contains("resolving") && states[^1] == "playing" && results == 0, "Phase or result lifecycle wrong.");

            // Hold presentation open: re-entrant clicks must not consume a second move.
            session.RetryLevel();
            var gate = new GatedPresenter();
            session.AttachPresenter(gate);
            Task<SwapAttemptStatus> pending = session.RequestSwapAsync(OpeningFrom, OpeningTo);
            Require(!pending.IsCompleted && session.IsBusy && !gate.Interactive && Moves(session) == 14, "Transaction was not locked.");
            Require(await session.RequestSwapAsync(OpeningFrom, OpeningTo) == SwapAttemptStatus.Busy && Moves(session) == 14, "Rapid click double-charged.");
            int oldId = Id(session);
            session.RetryLevel();
            Require(await pending == SwapAttemptStatus.Cancelled, "Retry did not cancel old animation.");
            Require(Id(session) > oldId && Moves(session) == 15 && Dump(session.CurrentBoard!) == initial && gate.Interactive,
                "Stale transaction changed retry board/moves/input.");
            pending = session.RequestSwapAsync(OpeningFrom, OpeningTo);
            oldId = Id(session);
            session.LeaveLevel();
            Require(await pending == SwapAttemptStatus.Cancelled, "Leave did not cancel pending animation.");
            Require(Id(session) > oldId && session.CurrentBoard is null && Moves(session) == 0 && results == 0 && !gate.Interactive,
                "Old transaction polluted idle/new state.");
            GD.Print("[STEP2_LIFETIME_PASS] busy guard; one deduction; retry/leave cancellation; monotonic session IDs; no stale result.");

            session.AttachPresenter(null);
            session.StartLevel(1);
            for (int move = 0; move < 15 && session.GetSnapshot()["phase"].AsString() == "playing"; move++)
            {
                pair = FindPair(session.CurrentBoard!, true);
                int remaining = Moves(session);
                Require(await session.RequestSwapAsync(pair.From, pair.To) == SwapAttemptStatus.Accepted, "Valid move rejected.");
                Require(Moves(session) == remaining - 1, "A cascade charged more than one move.");
                CheckStable(session.CurrentBoard!, opening);
                GD.Print($"[STEP2_PLAYTHROUGH] move={move + 1}; from={pair.From}; to={pair.To}; cascades={session.LastResolution!.Cascades}; remaining={Moves(session)}");
            }
            Require(results == 1 && lastResult is not null && session.GetSnapshot()["phase"].AsString() is "won" or "lost", "Terminal result missing.");
            Require(lastResult!["stars"].AsInt32() == LevelRules.Stars(15, Moves(session), lastResult["won"].AsBool()), "Earned stars inconsistent.");
            Require(await session.RequestSwapAsync(OpeningFrom, OpeningTo) == SwapAttemptStatus.Busy && results == 1, "Ended session accepted another move/result.");

            // Run actual .tscn, input mapping, signal -> session, and frame-based board presentation.
            harness = GD.Load<PackedScene>("res://presentation/2d/GameplayLoop.tscn").Instantiate<GameplayLoopRunner>();
            host.AddChild(harness);
            await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            Click(harness.BoardView, OpeningFrom);
            Click(harness.BoardView, OpeningTo);
            Require(harness.Session.IsBusy && Moves(harness.Session) == 14, "Scene input did not reach session.");
            Click(harness.BoardView, OpeningFrom); Click(harness.BoardView, OpeningTo);
            Require(Moves(harness.Session) == 14 && harness.BoardView.Interaction!.Selected is null, "Animated view allowed extra clicks.");
            for (int frame = 0; frame < 1500 && harness.Session.IsBusy; frame++)
                await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!harness.Session.IsBusy && Dump(harness.Session.CurrentBoard!) == Dump(accepted.FinalBoard), "Animated scene did not reach calculated board.");
            Click(harness.BoardView, FindPair(harness.Session.CurrentBoard!, true).From);
            var next = FindPair(harness.Session.CurrentBoard!, true);
            Click(harness.BoardView, next.To);
            harness.Session.RetryLevel();
            for (int frame = 0; frame < 3; frame++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(Moves(harness.Session) == 15 && Dump(harness.Session.CurrentBoard!) == initial, "Real presentation overwrote retry.");
            GD.Print("[STEP2_SCENE_PASS] actual .tscn; mapped mouse handlers; signal wiring; awaited animation; stable board; retry while animating.");
            // Exercise the production AppController through a clearly isolated UI contract probe.
            var app = (AppController)host;
            var probe = new StepTwoUiProbe();
            var boardHost = new Control { Name = "BoardHost", Size = new Vector2(560, 500), Position = new Vector2(360, 130) };
            probe.AddChild(boardHost);
            app.ConnectUi(probe);
            Require(probe.Page == "start", "App did not show start page.");
            probe.EmitSignal(GameUiBridge.SignalName.LevelSelectRequested);
            Require(probe.Page == "select", "Select request not connected.");
            probe.EmitSignal(GameUiBridge.SignalName.StartRequested, 1);
            var appSession = app.GetNode<GameSession>("GameSession");
            var appBoard = boardHost.GetNode<BoardView2D>("BoardView2D");
            Require(probe.Page == "game" && probe.Snapshot["moves_remaining"].AsInt32() == 15 && appBoard.IsVisibleInTree(), "Real start/HUD/board-slot hookup failed.");
            Click(appBoard, OpeningFrom); Click(appBoard, OpeningTo);
            Require(probe.Snapshot["phase"].AsString() == "resolving" && probe.Snapshot["moves_remaining"].AsInt32() == 14, "Real HUD did not immediately update.");
            probe.EmitSignal(GameUiBridge.SignalName.LeaveRequested);
            for (int frame = 0; frame < 3; frame++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(probe.Page == "select" && appSession.CurrentBoard is null && !appBoard.Visible, "Navigation left stale board/animation.");
            probe.EmitSignal(GameUiBridge.SignalName.StartRequested, 1);
            Require(probe.Snapshot["moves_remaining"].AsInt32() == 15, "New game retained old HUD.");
            probe.EmitSignal(GameUiBridge.SignalName.RetryRequested);
            Require(probe.Page == "game" && Moves(appSession) == 15, "Retry request not wired.");
            probe.EmitSignal(GameUiBridge.SignalName.StartScreenRequested);
            Require(probe.Page == "start" && appSession.CurrentBoard is null && !appBoard.Visible, "Start navigation failed to end old session.");
            GD.Print("[STEP2_APP_BRIDGE_PASS] Production AppController; inherited request signals; real snapshots; optional BoardHost; navigation cancellation. Probe is not teammate UI.");
            GD.Print("[STEP2_SMOKE_PASS] Real swap/rollback/clear/gravity/refill/cascade/session/scene checks. GUI appearance and teammate UI remain unverified.");
            return 0;
        }
        catch (Exception error)
        {
            GD.PushError($"[STEP2_SMOKE_FAIL] {error}");
            return 1;
        }
        finally
        {
            harness?.Free();
            session?.Free();
        }
    }

    private static void CheckFixtures()
    {
        foreach (int length in new[] { 3, 4, 5 })
        {
            var cells = new BoardCell[length, 1];
            for (int x = 0; x < length; x++) cells[x, 0] = BoardCell.Piece(PieceColor.Red);
            Require(Resolver.FindMatches(new BoardModel(cells)).Count == length, $"Run {length} not fully cleared.");
        }
        var cross = Parse("BRB", "RRR", "YRY");
        Require(Resolver.FindMatches(cross).Count == 5, "Cross intersection counted twice.");
        Require(Resolver.FindMatches(Parse("###", "###", "###")).Count == 0, "Boxes matched as pieces.");
        var small = Parse("RB", "GY");
        var noMatch = Resolver.ResolveSwap(small, new(0, 0), new(1, 0), 123);
        Require(noMatch.Status == SwapAttemptStatus.NoMatch && noMatch.Steps.Count == 2 &&
            noMatch.Steps[^1].Kind == BoardStepKind.SwapBack && Dump(noMatch.FinalBoard) == Dump(small) && noMatch.NextRandomState == 123,
            "No-match rollback or RNG preservation failed.");
        var column = Parse("R", ".", "B", "#", ".", "G", ".");
        var fall = Resolver.ApplyGravity(column);
        Require(Dump(fall.After) == "./R/B/#/././G", "Gravity crossed a box or changed piece order.");
        Require(column.GetCell(new(0, 3)).BoxLayers == fall.After.GetCell(new(0, 3)).BoxLayers, "Box layers changed.");
        var splitCells = column.CopyCells(); splitCells[0, 3] = BoardCell.Box(2);
        var split = new BoardModel(splitCells);
        var splitFall = Resolver.ApplyGravity(split).After;
        uint state = 567;
        var filled = new BoardGenerator().Refill(splitFall, ref state).After;
        Require(filled.GetCell(new(0, 3)) == BoardCell.Box(2), "Refill damaged double-layer box.");
        Require(Enumerable.Range(0, 7).All(y => filled.GetCell(new(0, y)).Kind != CellKind.Empty), "Segment refill left holes.");
        // Explicit bounded-cascade failure must not partially commit a caller's model.
        var infiniteStart = Parse("RBR", "BRB", "YGY");
        string original = Dump(infiniteStart);
        bool bounded = false;
        for (uint seed = 1; seed <= 100 && !bounded; seed++)
        {
            try { Resolver.ResolveSwap(infiniteStart, new(1, 0), new(1, 1), seed, 1); }
            catch (InvalidOperationException) { bounded = true; }
        }
        Require(bounded && Dump(infiniteStart) == original, "Cascade limit did not protect source board.");
    }

    // Independent triple oracle used for stable-board assertions and legal-pair selection.
    private static bool HasTriple(BoardModel board)
    {
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        {
            var c = board.GetCell(new(x, y));
            if (c.Kind != CellKind.Piece) continue;
            bool Same(int a, int b) { var next = board.GetCell(new(a, b)); return next.Kind == CellKind.Piece && next.Color == c.Color; }
            if (x + 2 < board.Width && Same(x + 1, y) && Same(x + 2, y) || y + 2 < board.Height && Same(x, y + 1) && Same(x, y + 2)) return true;
        }
        return false;
    }

    private static (GridPosition From, GridPosition To) FindPair(BoardModel board, bool wantMatch)
    {
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        foreach (var offset in new[] { new GridPosition(1, 0), new GridPosition(0, 1) })
        {
            var from = new GridPosition(x, y); var to = new GridPosition(x + offset.Column, y + offset.Row);
            if (board.ValidateSwap(from, to) != SwapValidation.Accepted) continue;
            var cells = board.CopyCells();
            (cells[x, y], cells[to.Column, to.Row]) = (cells[to.Column, to.Row], cells[x, y]);
            if (HasTriple(new BoardModel(cells)) == wantMatch) return (from, to);
        }
        throw new InvalidOperationException("No requested pair; dead-board reshuffle is pending step 4.");
    }

    private static void CheckStable(BoardModel board, BoardModel original)
    {
        Require(!HasTriple(board), "A final board still has matches.");
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        {
            var p = new GridPosition(x, y); var cell = board.GetCell(p); var previous = original.GetCell(p);
            Require(cell.Kind != CellKind.Empty, "Stable board has holes.");
            Require(cell.Kind != CellKind.Box || previous.Kind == CellKind.Box && cell.BoxLayers <= previous.BoxLayers,
                "A surviving box moved, gained layers or appeared outside its original position.");
        }
    }

    private static BoardModel Parse(params string[] rows)
    {
        var cells = new BoardCell[rows[0].Length, rows.Length];
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[0].Length; x++)
            cells[x, y] = rows[y][x] switch { 'R' => BoardCell.Piece(PieceColor.Red), 'B' => BoardCell.Piece(PieceColor.Blue),
                'G' => BoardCell.Piece(PieceColor.Green), 'Y' => BoardCell.Piece(PieceColor.Yellow), '#' => BoardCell.Box(1), _ => default };
        return new BoardModel(cells);
    }

    private static string Dump(BoardModel board) => string.Join("/", Enumerable.Range(0, board.Height).Select(y =>
        new string(Enumerable.Range(0, board.Width).Select(x => { var c = board.GetCell(new(x, y)); return c.Kind switch
        { CellKind.Box => '#', CellKind.Empty => '.', _ => "RBGY"[(int)c.Color] }; }).ToArray())));
    private static int Moves(GameSession session) => session.GetSnapshot()["moves_remaining"].AsInt32();
    private static int Id(GameSession session) => session.GetSnapshot()["session_id"].AsInt32();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Click(BoardView2D view, GridPosition p) => view._UnhandledInput(new InputEventMouseButton
    { Pressed = true, ButtonIndex = MouseButton.Left, Position = view.GetGlobalTransformWithCanvas() *
        view.CellCenter(p) });

    private sealed class GatedPresenter : IBoardPresenter
    {
        public bool Interactive { get; private set; }
        public void SetBoard(BoardModel _) { }
        public void SetInteractive(bool enabled) => Interactive = enabled;
        public void ClearBoard() { }
        public async Task PresentAsync(BoardStep _, CancellationToken token)
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await gate.Task.WaitAsync(token);
        }
    }
}
