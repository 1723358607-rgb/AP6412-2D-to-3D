using Godot;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Records actual GameSession/presenter states for future 3D comparison.
/// Test-only: reads the selected archived moves from the complete repository, writes no player files.</summary>
public static class RuleReferenceCheck
{
    public static async Task<int> Run()
    {
        try
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
            using var index = JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "docs/evidence/member-a/current-rule-reference.json")));
            string path = System.IO.Path.Combine(root, index.RootElement.GetProperty("replays_path").GetString()!);
            using var source = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var levels = new List<object>();
            int accepted = 0, presented = 0;
            foreach (var replay in source.RootElement.GetProperty("replays").EnumerateArray())
            {
                int id = replay.GetProperty("level_id").GetInt32();
                var level = BalanceStudy.Load(id);
                Require(level.Seed == replay.GetProperty("seed").GetUInt32(), "Archived seed differs from level config.");
                var session = new GameSession();
                try
                {
                    var recorder = new RecordingPresenter();
                    session.AttachPresenter(recorder);
                    int results = 0, earnedStars = -1;
                    session.LevelFinished += result => { results++; earnedStars = result["stars"].AsInt32(); };
                    session.StartLevel(id);
                    var generated = new BoardGenerator().CreateInitialBoard(level, out uint random);
                    Require(Rows(generated).SequenceEqual(Rows(session.CurrentBoard!)), "Session opening differs from generator.");
                    object initial = State(session, random);
                    var moves = new List<object>();
                    foreach (var move in replay.GetProperty("replay").GetProperty("moves").EnumerateArray())
                    {
                        var from = new GridPosition(move.GetProperty("from_col").GetInt32(), move.GetProperty("from_row").GetInt32());
                        var to = new GridPosition(move.GetProperty("to_col").GetInt32(), move.GetProperty("to_row").GetInt32());
                        recorder.Steps.Clear();
                        Require(await session.RequestSwapAsync(from, to) == SwapAttemptStatus.Accepted, "Archived move was rejected.");
                        var resolution = session.LastResolution!;
                        var snapshot = session.GetSnapshot();
                        Require(snapshot["moves_remaining"].AsInt32() == move.GetProperty("remaining").GetInt32()
                            && snapshot["target_remaining"].AsInt32() == move.GetProperty("targets").GetInt32()
                            && resolution.Cascades == move.GetProperty("cascades").GetInt32()
                            && resolution.Reshuffles == move.GetProperty("reshuffles").GetInt32(), "Archived replay diverged.");
                        Require(recorder.Steps.Count == resolution.Steps.Count, "Presenter missed a rule step.");
                        moves.Add(new { from = Position(from), to = Position(to), resolution.Cascades, resolution.Reshuffles,
                            state = State(session, resolution.NextRandomState), steps = recorder.Steps.ToArray() });
                        accepted++; presented += recorder.Steps.Count;
                    }
                    var expected = replay.GetProperty("replay");
                    Require(results == 1 && session.GetSnapshot()["phase"].AsString() == "won"
                        && earnedStars == expected.GetProperty("stars").GetInt32()
                        && session.GetSnapshot()["moves_remaining"].AsInt32() == expected.GetProperty("remaining").GetInt32(),
                        "Terminal result differs from archive or was duplicated.");
                    levels.Add(new { level_id = id, seed = level.Seed, initial_moves = level.InitialMoves,
                        initial, moves, result = new { won = true, stars = earnedStars } });
                }
                finally { session.LeaveLevel(); session.Free(); }
            }
            Require(levels.Count == 3, "Expected all three levels.");
            GD.Print("[RULE_REFERENCE_JSON] " + JsonSerializer.Serialize(new {
                schema_version = 1, status = "provisional-not-frozen", coordinates = "zero-based column,row",
                cell_legend = "R/B/G/Y = Core PieceColor; 1/2 = box layers; . = empty (not artwork colors)",
                grid_hash = "SHA256 UTF-8 rows joined by LF, no trailing LF", levels }));
            GD.Print($"[RULE_REFERENCE_PASS] 3 levels; {accepted} accepted swaps; {presented} presenter steps; actual session states/results agree with archived replay. No player saves touched.");
            return 0;
        }
        catch (Exception e) { GD.PushError("[RULE_REFERENCE_FAIL] " + e); return 1; }
    }

    private static object State(GameSession session, uint random)
    {
        var snapshot = session.GetSnapshot(); var rows = Rows(session.CurrentBoard!);
        return new { phase = snapshot["phase"].AsString(), moves_remaining = snapshot["moves_remaining"].AsInt32(),
            target_remaining = snapshot["target_remaining"].AsInt32(), random_state = random, grid = rows, grid_sha256 = Hash(rows) };
    }
    private static int[] Position(GridPosition p) => [p.Column, p.Row];
    private static string[] Rows(BoardModel board) => Enumerable.Range(0, board.Height).Select(y => new string(
        Enumerable.Range(0, board.Width).Select(x => board.GetCell(new(x, y)) switch {
            { Kind: CellKind.Piece } p => "RBGY"[(int)p.Color],
            { Kind: CellKind.Box } b => (char)('0' + b.BoxLayers), _ => '.' }).ToArray())).ToArray();
    private static string Hash(string[] rows) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows)))).ToLowerInvariant();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class RecordingPresenter : IBoardPresenter
    {
        public List<object> Steps { get; } = [];
        public void SetBoard(BoardModel board) { }
        public void SetInteractive(bool enabled) { }
        public void ClearBoard() { }
        public Task PresentAsync(BoardStep step, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); var rows = Rows(step.After);
            Steps.Add(new { kind = step.Kind.ToString(), grid = rows, grid_sha256 = Hash(rows),
                cleared = step.Cleared.Select(Position).ToArray(), damaged_boxes = (step.DamagedBoxes ?? []).Select(Position).ToArray() });
            return Task.CompletedTask;
        }
    }
}
