using Godot;
using AP6412Match3.App;
using AP6412Match3.App.Contracts;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Rule fixtures and real level replays. No test data is saved as player progress.</summary>
public static class StepThreeSmokeCheck
{
    private static readonly MatchResolver Resolver = new();
    private sealed record Move(GridPosition From, GridPosition To, SwapResolution Resolution);
    private sealed record Fixture(LevelDefinition Level, BoardModel Board, uint Random, GridPosition From, GridPosition To);

    public static async Task<int> Run(AppController app)
    {
        GameSession? session = null;
        try
        {
            CheckBoxWaves();
            CheckStars();
            session = new GameSession(); app.AddChild(session);
            int results = 0;
            GDictionary? latest = null;
            session.LevelFinished += result => { results++; latest = result; };
            foreach (int id in new[] { 1, 2, 3 })
            {
                LevelDefinition level = Load(id);
                session.StartLevel(id);
                BoardModel opening = session.CurrentBoard!;
                int expectedBoxes = id == 1 ? 5 : id == 2 ? 10 : 14;
                int expectedMoves = id == 1 ? 15 : id == 2 ? 20 : 24;
                Require(level.InitialMoves == expectedMoves && LevelRules.CountBoxes(opening) == expectedBoxes, "Formal level budget/target changed.");
                for (int y = 0; y < 7; y++)
                for (int x = 0; x < 7; x++)
                {
                    var configured = level.Boxes.SingleOrDefault(b => b.Column == x && b.Row == y);
                    bool box = configured is not null;
                    BoardCell cell = opening.GetCell(new(x, y));
                    Require((cell.Kind == CellKind.Box) == box, $"Wrong level-{id} box position ({x},{y}).");
                    if (box) Require(cell.BoxLayers == configured!.Layers, "Wrong box layers.");
                }
                Require(Resolver.FindMatches(opening).Count == 0 && ValidMoves(opening, InitialRandom(level)).Count > 0, "Initial board has matches or no legal move.");
                LayoutStudy.CheckGeometry(level);
                int beforeId = Id(session);
                Move first = ValidMoves(opening, InitialRandom(level))[0];
                await session.RequestSwapAsync(first.From, first.To);
                session.RetryLevel();
                Require(Id(session) > beforeId && Dump(session.CurrentBoard!) == Dump(opening) && Moves(session) == expectedMoves, "Retry failed to restore formal board/boxes/budget.");
                // Bounded preliminary search supplies actual replay evidence, not balance claims.
                List<Move> replay = FindWinningReplay(level);
                int previousResultCount = results;
                for (int index = 0; index < replay.Count; index++)
                {
                    Move move = replay[index];
                    Require(await session.RequestSwapAsync(move.From, move.To) == SwapAttemptStatus.Accepted, "Replay move rejected.");
                    Require(Moves(session) == expectedMoves - index - 1 && Dump(session.CurrentBoard!) == Dump(move.Resolution.FinalBoard), "Session diverged from pure replay.");
                }
                Require(results == previousResultCount + 1 && Phase(session) == "won" && latest!["target_remaining"].AsInt32() == 0,
                    "Formal replay did not produce exactly one win.");
                Require(await session.RequestSwapAsync(first.From, first.To) == SwapAttemptStatus.Busy && results == previousResultCount + 1,
                    "Ended session accepted another move/result.");
                int stars = latest!["stars"].AsInt32();
                GD.Print($"[STEP3_FORMAL_PASS] level={id}; seed={level.Seed}; moves_used={replay.Count}/{expectedMoves}; remaining={Moves(session)}; stars={stars}; targets=0/{expectedBoxes}.");
                GD.Print("[STEP3_REPLAY_JSON] " + System.Text.Json.JsonSerializer.Serialize(new
                {
                    level_id = id, seed = level.Seed, initial_moves = expectedMoves, initial_board = Dump(opening), stars,
                    moves = replay.Select((m, i) => new { from_col = m.From.Column, from_row = m.From.Row, to_col = m.To.Column,
                        to_row = m.To.Row, remaining = expectedMoves - i - 1, cascades = m.Resolution.Cascades,
                        target_remaining = LevelRules.CountBoxes(m.Resolution.FinalBoard), board = Dump(m.Resolution.FinalBoard) }),
                }));
            }

            var win = MakeFixture(true);
            results = 0;
            var gate = new RefillGate();
            session.AttachPresenter(gate);
            session.BeginLevel(win.Level, win.Board, win.Random);
            Task<SwapAttemptStatus> pending = session.RequestSwapAsync(win.From, win.To);
            Require(!pending.IsCompleted && Phase(session) == "resolving" && Moves(session) == 0 &&
                session.GetSnapshot()["target_remaining"].AsInt32() == 0 && results == 0 && !gate.Interactive,
                "Target completion emitted a premature win before refill/cascades finished.");
            gate.Release();
            Require(await pending == SwapAttemptStatus.Accepted && results == 1 && Phase(session) == "won" &&
                latest!["won"].AsBool() && latest["stars"].AsInt32() == 1 && Resolver.FindMatches(session.CurrentBoard!).Count == 0,
                "Last-move win priority, one-star result or final stability failed.");
            Require(gate.Presented == session.LastResolution!.Steps.Count, "Result skipped presentation steps.");
            GD.Print($"[STEP3_LAST_MOVE_PASS] ISOLATED TEST FIXTURE seed={win.Level.Seed}; initial_moves=1; last target removed; waited for refill; won=true; remaining=0; stars=1; results=1.");
            session.AttachPresenter(null);
            var lose = MakeFixture(false);
            session.BeginLevel(lose.Level, lose.Board, lose.Random);
            results = 0;
            await session.RequestSwapAsync(lose.From, lose.To);
            Require(results == 1 && Phase(session) == "lost" && !latest!["won"].AsBool() && latest["stars"].AsInt32() == 0,
                "One-move failure must be zero stars.");

            // Cancel an old session after its final target disappears, before its result may be emitted.
            gate = new RefillGate(); session.AttachPresenter(gate);
            session.BeginLevel(win.Level, win.Board, win.Random); results = 0;
            pending = session.RequestSwapAsync(win.From, win.To);
            int oldId = Id(session);
            session.StartLevel(3);
            Require(await pending == SwapAttemptStatus.Cancelled && Id(session) > oldId && Moves(session) == 24 &&
                LevelRules.CountBoxes(session.CurrentBoard!) == 14 && results == 0, "Old win polluted a new level.");
            session.BeginLevel(win.Level, win.Board, win.Random);
            pending = session.RequestSwapAsync(win.From, win.To);
            session.LeaveLevel();
            Require(await pending == SwapAttemptStatus.Cancelled && Phase(session) == "idle" && results == 0, "Leave emitted stale win.");
            session.AttachPresenter(null);
            void NavigateOnWon(GDictionary snapshot) { if (snapshot["phase"].AsString() == "won") session.StartLevel(2); }
            session.StateChanged += NavigateOnWon;
            session.BeginLevel(win.Level, win.Board, win.Random);
            await session.RequestSwapAsync(win.From, win.To);
            session.StateChanged -= NavigateOnWon;
            Require(Phase(session) == "playing" && Moves(session) == 20 && results == 0, "Terminal listener re-entry emitted stale result.");
            GD.Print("[STEP3_LIFETIME_PASS] retry/new-level/leave cancellation; target-zero intermediate state; stale result suppression; terminal request rejection.");

            await CheckAppBridge(app);
            GD.Print("[STEP3_SMOKE_PASS] Box damage, three formal levels, integer stars, last-move win, real replays and production result handoff. GUI and real B persistence remain unverified.");
            return 0;
        }
        catch (Exception error)
        {
            GD.PushError($"[STEP3_SMOKE_FAIL] {error}");
            return 1;
        }
        finally { session?.Free(); }
    }

    private static void CheckBoxWaves()
    {
        BoardModel ring = Parse("RRR", "R2R", "RRR");
        var matches = Resolver.FindMatches(ring);
        Require(matches.Count == 8, "Ring fixture must hit the box from four matching directions.");
        BoardStep first = Resolver.ClearMatches(ring, matches);
        Require(first.After.GetCell(new(1, 1)) == BoardCell.Box(1) && first.DamagedBoxes!.Count == 1 && LevelRules.CountBoxes(first.After) == 1,
            "Double-layer box must take one damage and still count as one target.");
        BoardStep second = Resolver.ClearMatches(Parse("RRR", "R#R", "RRR"), matches);
        Require(LevelRules.CountBoxes(second.After) == 0 && ring.GetCell(new(1, 1)) == BoardCell.Box(2), "Second wave did not finish box or mutated source.");
        BoardStep diagonal = Resolver.ClearMatches(Parse("R..", ".2.", "..."), new HashSet<GridPosition> { new(0, 0) });
        Require(diagonal.After.GetCell(new(1, 1)) == BoardCell.Box(2), "Diagonal damage is forbidden.");
        var chain = Resolver.ClearMatches(Parse("RRR", "#..", "#.."), new HashSet<GridPosition> { new(0, 0), new(1, 0), new(2, 0) });
        Require(chain.After.GetCell(new(0, 1)).Kind == CellKind.Empty && chain.After.GetCell(new(0, 2)) == BoardCell.Box(1), "Destroyed boxes must not damage other boxes.");
        var connected = Parse("RBG", "GYB", "RRR", "#BG", "BYG");
        var clear = Resolver.ClearMatches(connected, Resolver.FindMatches(connected));
        var fall = Resolver.ApplyGravity(clear.After);
        Require(fall.After.GetCell(new(0, 3)) == BoardCell.Piece(PieceColor.Green), "Removed box did not reconnect vertical gravity segments.");
        GD.Print("[STEP3_BOX_PASS] four-direction hit=one layer/wave; target count 1->1->0; no diagonal/box-chain damage; removed-box gravity reconnection.");
    }

    private static void CheckStars()
    {
        foreach (var sample in new[] { (Total: 15, Two: 3, Three: 6), (Total: 20, Two: 4, Three: 8), (Total: 24, Two: 5, Three: 10) })
        {
            for (int moves = 0; moves <= sample.Total; moves++)
            {
                int expected = moves >= sample.Three ? 3 : moves >= sample.Two ? 2 : 1;
                Require(LevelRules.Stars(sample.Total, moves, true) == expected && LevelRules.Stars(sample.Total, moves, false) == 0, "Star threshold or loss value wrong.");
            }
            GD.Print($"[STEP3_STAR_PASS] total={sample.Total}; one/two boundary={sample.Two - 1}/{sample.Two}; two/three boundary={sample.Three - 1}/{sample.Three}; last-move win=1; losses=0.");
        }
        Require(LevelRules.PhaseAfterCascades(0, 0) == "won" && LevelRules.PhaseAfterCascades(1, 0) == "lost", "Win must precede failure.");
    }

    private static async Task CheckAppBridge(AppController app)
    {
        var ui = new StepTwoUiProbe { RecreatePages = true };
        ui.AddChild(new Control { Name = "BoardHost", Size = new Vector2(560, 500) });
        var progress = new StepThreeProgressProbe();
        app.ConnectUi(ui, progress);
        var session = app.GetNode<GameSession>("GameSession");
        ui.EmitSignal(GameUiBridge.SignalName.StartRequested, 2);
        Require(session.CurrentBoard is null && progress.LoadedMode == "2d", "Locked level started or mode not loaded.");
        for (int id = 1; id <= 3; id++)
        {
            ui.EmitSignal(GameUiBridge.SignalName.StartRequested, id);
            Require(ui.Page == "game" && session.GetSnapshot()["level_id"].AsInt32() == id, "Unlocked formal level not started.");
            var fixture = MakeFixture(true, id);
            session.BeginLevel(fixture.Level, fixture.Board, fixture.Random);
            await session.RequestSwapAsync(fixture.From, fixture.To);
            Require(ui.Page == "result" && ui.Result["won"].AsBool() && ui.BestShown == 1 && progress.GetBestStars(id) == 1 && progress.RecordCalls == id,
                "App must record real earned stars before showing best stars.");
            ui.EmitSignal(GameUiBridge.SignalName.NextRequested);
            Require(id == 3 ? Phase(session) == "won" && session.GetSnapshot()["level_id"].AsInt32() == 3 :
                Phase(session) == "playing" && session.GetSnapshot()["level_id"].AsInt32() == id + 1, "Next-level/final-level boundary broken.");
        }
        ui.EmitSignal(GameUiBridge.SignalName.LevelSelectRequested);
        Require(ui.Page == "select" && session.CurrentBoard is null, "Return to level select left an active board.");
        for (int frame = 0; frame < 3; frame++) await app.ToSignal(app.GetTree(), SceneTree.SignalName.ProcessFrame);
        var parkedBoard = app.GetNodeOrNull<Node>("BoardView2D");
        Require(parkedBoard is not null && GodotObject.IsInstanceValid(parkedBoard), "UI page disposal destroyed A's persistent board presenter.");
        ui.EmitSignal(GameUiBridge.SignalName.StartRequested, 1);
        Require(ui.Page == "game" && Moves(session) == 15, "Reopening gameplay after page disposal failed.");
        ui.EmitSignal(GameUiBridge.SignalName.RetryRequested);
        ui.EmitSignal(GameUiBridge.SignalName.LeaveRequested);
        for (int frame = 0; frame < 3; frame++) await app.ToSignal(app.GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(app.GetNodeOrNull<Node>("BoardView2D") is not null && session.CurrentBoard is null, "Retry/leave page disposal lost the presenter.");
        GD.Print("[STEP3_APP_PASS] production controller; record-before-result; unlock/next via in-memory progress probe; final level rejects level4; disposable UI pages preserve A's board across result/retry/leave/reopen. Real B persistence not tested.");
    }

    private static Fixture MakeFixture(bool winning, int id = 1)
    {
        for (uint seed = 991; seed < 1191; seed++)
        {
            var level = new LevelDefinition { LevelId = id, Name = "ISOLATED TEST FIXTURE", Width = 7, Height = 7,
                InitialMoves = 1, Seed = seed, Boxes = [new BoxDefinition { Column = 3, Row = 6, Layers = 1 }] };
            BoardModel initial = new BoardGenerator().CreateInitialBoard(level, out uint random);
            GridPosition from, to;
            if (winning)
            {
                var cells = initial.CopyCells();
                cells[2, 5] = BoardCell.Piece(PieceColor.Red); cells[3, 5] = BoardCell.Piece(PieceColor.Blue);
                cells[4, 5] = BoardCell.Piece(PieceColor.Red); cells[3, 4] = BoardCell.Piece(PieceColor.Red);
                initial = new BoardModel(cells); from = new(3, 4); to = new(3, 5);
            }
            else { from = new(1, 0); to = new(1, 1); }
            if (Resolver.FindMatches(initial).Count > 0) continue;
            var resolution = Resolver.ResolveSwap(initial, from, to, random);
            if (resolution.Status == SwapAttemptStatus.Accepted && (LevelRules.CountBoxes(resolution.FinalBoard) == 0) == winning)
                return new Fixture(level, initial, random, from, to);
        }
        throw new InvalidOperationException("Bounded test-fixture generation failed.");
    }

    private static List<Move> FindWinningReplay(LevelDefinition level)
    {
        for (int attempt = 0; attempt < 96; attempt++)
        {
            BoardModel board = new BoardGenerator().CreateInitialBoard(level, out uint random);
            uint policy = (uint)(attempt * 12347 + level.LevelId * 7919 + 1);
            var path = new List<Move>();
            for (int turn = 0; turn < level.InitialMoves; turn++)
            {
                List<Move> choices = ValidMoves(board, random);
                if (choices.Count == 0) break;
                int Layers(BoardModel b) => Enumerable.Range(0, b.Height).Sum(y => Enumerable.Range(0, b.Width).Sum(x => b.GetCell(new(x, y)).BoxLayers));
                var ranked = choices.OrderByDescending(m => (LevelRules.CountBoxes(board) - LevelRules.CountBoxes(m.Resolution.FinalBoard)) * 300 +
                    (Layers(board) - Layers(m.Resolution.FinalBoard)) * 100 + m.Resolution.TotalCleared).ToList();
                // Vary only the search policy. The formal level and refill RNG are never altered.
                policy ^= policy << 13; policy ^= policy >> 17; policy ^= policy << 5;
                int breadth = attempt == 0 ? 1 : Math.Min(1 + attempt % 5, ranked.Count);
                Move chosen = ranked[(int)(policy % breadth)];
                path.Add(chosen); board = chosen.Resolution.FinalBoard; random = chosen.Resolution.NextRandomState;
                if (LevelRules.CountBoxes(board) == 0) return path;
            }
        }
        throw new InvalidOperationException($"No bounded preliminary winning path for level {level.LevelId}; no rules/parameters were changed to hide this.");
    }

    private static List<Move> ValidMoves(BoardModel board, uint random)
    {
        var choices = new List<Move>();
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        foreach (var offset in new[] { new GridPosition(1, 0), new GridPosition(0, 1) })
        {
            var from = new GridPosition(x, y); var to = new GridPosition(x + offset.Column, y + offset.Row);
            if (board.ValidateSwap(from, to) != SwapValidation.Accepted) continue;
            var resolved = Resolver.ResolveSwap(board, from, to, random);
            if (resolved.Status == SwapAttemptStatus.Accepted) choices.Add(new Move(from, to, resolved));
        }
        return choices;
    }

    private static LevelDefinition Load(int id) => LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString($"res://data/levels/level_{id:00}.json"));
    private static uint InitialRandom(LevelDefinition level) { new BoardGenerator().CreateInitialBoard(level, out uint random); return random; }
    private static string Phase(GameSession s) => s.GetSnapshot()["phase"].AsString();
    private static int Moves(GameSession s) => s.GetSnapshot()["moves_remaining"].AsInt32();
    private static int Id(GameSession s) => s.GetSnapshot()["session_id"].AsInt32();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Dump(BoardModel b) => string.Join("/", Enumerable.Range(0, b.Height).Select(y => new string(Enumerable.Range(0, b.Width)
        .Select(x => { var c = b.GetCell(new(x, y)); return c.Kind == CellKind.Box ? (char)('0' + c.BoxLayers) : c.Kind == CellKind.Empty ? '.' : "RBGY"[(int)c.Color]; }).ToArray())));
    private static BoardModel Parse(params string[] rows)
    {
        var cells = new BoardCell[rows[0].Length, rows.Length];
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[0].Length; x++)
            cells[x, y] = rows[y][x] switch { 'R' => BoardCell.Piece(PieceColor.Red), 'B' => BoardCell.Piece(PieceColor.Blue),
                'G' => BoardCell.Piece(PieceColor.Green), 'Y' => BoardCell.Piece(PieceColor.Yellow), '#' => BoardCell.Box(1), '2' => BoardCell.Box(2), _ => default };
        return new BoardModel(cells);
    }

    private sealed class RefillGate : IBoardPresenter
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Interactive { get; private set; }
        public int Presented { get; private set; }
        public void SetBoard(BoardModel _) { }
        public void SetInteractive(bool enabled) => Interactive = enabled;
        public void ClearBoard() { }
        public void Release() => _gate.TrySetResult();
        public async Task PresentAsync(BoardStep step, CancellationToken token)
        {
            Presented++;
            if (step.Kind == BoardStepKind.Refill) await _gate.Task.WaitAsync(token);
        }
    }
}
