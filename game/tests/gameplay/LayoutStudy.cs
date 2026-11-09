using Godot;
using System.Text.Json;
using AP6412Match3.Core;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Test-only geometry and immediate-reward diagnostics for distributed box layouts.</summary>
public static class LayoutStudy
{
    public static int Run()
    {
        try
        {
            if (OS.GetCmdlineUserArgs().Contains("--layout-seed-scan")) return ScanSeeds();
            string oldPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"),
                "..", "docs", "evidence", "member-a", "layout-v2-previous-levels.json"));
            using var old = JsonDocument.Parse(System.IO.File.ReadAllText(oldPath));
            foreach (var json in old.RootElement.EnumerateArray())
            {
                var before = LevelDefinition.FromJson(json.GetRawText());
                var level = BalanceStudy.Load(before.LevelId);
                CheckGeometry(level);
                var feedback = JsonSerializer.SerializeToElement(Feedback(level));
                Require(feedback.GetProperty("bands").EnumerateArray().All(b => b.GetProperty("immediate_box_hit_moves").GetInt32() > 0),
                    "A starting region lacks an immediate box-damaging swap.");
                Require(BalanceStudy.CountOneMoveWins(level) == 0, "Opening can end in a single move.");
                GD.Print("[LAYOUT_BEFORE] " + JsonSerializer.Serialize(new { level_id = before.LevelId,
                    feedback = Feedback(before), balance = BalanceStudy.Measure(before, 128) }));
                GD.Print("[LAYOUT_AFTER] " + JsonSerializer.Serialize(new { level_id = level.LevelId,
                    feedback = Feedback(level), balance = BalanceStudy.Measure(level, 128) }));
                var replay = BalanceStudy.Play(level, 997, true);
                Require(replay.won, $"Level {level.LevelId} diagnostic path cannot win within budget.");
                GD.Print("[LAYOUT_REPLAY] " + JsonSerializer.Serialize(new { level_id = level.LevelId, seed = level.Seed, replay }));
            }
            GD.Print("[LAYOUT_STUDY_PASS] All three layouts occupy upper/middle/lower bands, retain counts/layers/budgets and have geometric damage routes; seeded diagnostics recorded.");
            return 0;
        }
        catch (Exception e) { GD.PushError("[LAYOUT_STUDY_FAIL] " + e); return 1; }
    }

    public static void CheckGeometry(LevelDefinition level)
    {
        Require(level.Boxes.Length == (level.LevelId == 1 ? 5 : level.LevelId == 2 ? 10 : 14), "Box count changed.");
        Require(level.InitialMoves == (level.LevelId == 1 ? 15 : level.LevelId == 2 ? 20 : 24), "Move budget changed.");
        Require(level.Boxes.Count(b => b.Layers == 2) == (level.LevelId == 3 ? 3 : 0), "Double-layer count changed.");
        int Band(int row) => row < 2 ? 0 : row < 5 ? 1 : 2;
        Require(Enumerable.Range(0, 3).All(band => level.Boxes.Any(b => Band(b.Row) == band)), "Missing target region.");
        bool Piece(int x, int y) => x >= 0 && x < 7 && y >= 0 && y < 7 && !level.Boxes.Any(b => b.Column == x && b.Row == y);
        foreach (var box in level.Boxes)
        {
            bool reachable = false;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            foreach (var (ax, ay) in new[] { (1, 0), (0, 1) })
            for (int offset = -2; offset <= 0; offset++)
                if (Enumerable.Range(0, 3).All(i => Piece(box.Column + dx + (offset + i) * ax, box.Row + dy + (offset + i) * ay))) reachable = true;
            Require(reachable, "A box has no adjacent geometric match route.");
        }
        Require(Enumerable.Range(0, 7).All(i => level.Boxes.Count(b => b.Row == i) < 7 && level.Boxes.Count(b => b.Column == i) < 7), "Solid box wall.");
    }

    private static object Feedback(LevelDefinition level)
    {
        var board = new BoardGenerator().CreateInitialBoard(level, out uint random);
        var legal = BoardMoves.FindLegal(board);
        var bands = Enumerable.Range(0, 3).Select(band =>
        {
            var moves = legal.Where(m => Band(Math.Min(m.From.Row, m.To.Row)) == band).ToArray();
            var damages = moves.Select(m => new MatchResolver().ResolveSwap(board, m.From, m.To, random)
                .Steps.First(s => s.Kind == BoardStepKind.Clear).DamagedBoxes!.Count).ToArray();
            return new { region = new[] { "upper_rows_1_2", "middle_rows_3_5", "lower_rows_6_7" }[band],
                legal_moves = moves.Length, immediate_box_hit_moves = damages.Count(n => n > 0), max_boxes_hit_first_wave = damages.DefaultIfEmpty().Max() };
        }).ToArray();
        return new { boxes_by_band = Enumerable.Range(0, 3).Select(band => level.Boxes.Count(b => Band(b.Row) == band)).ToArray(), bands };
    }
    private static int Band(int row) => row < 2 ? 0 : row < 5 ? 1 : 2;
    private static int ScanSeeds()
    {
        for (int id = 1; id <= 3; id++)
        {
            int found = 0;
            for (uint seed = 641201; seed < 641457 && found < 12; seed++)
            {
                var level = BalanceStudy.Load(id, seed);
                var feedback = JsonSerializer.SerializeToElement(Feedback(level));
                if (feedback.GetProperty("bands")[0].GetProperty("immediate_box_hit_moves").GetInt32() == 0) continue;
                if (BalanceStudy.CountOneMoveWins(level) > 0) continue;
                var play = BalanceStudy.Play(level, 997, true);
                int min = id == 1 ? 3 : id == 2 ? 5 : 7;
                int max = id == 1 ? 6 : id == 2 ? 9 : 12;
                if (!play.won || play.moves.Count < min || play.moves.Count > max) continue;
                GD.Print("[LAYOUT_SEED_CANDIDATE] " + JsonSerializer.Serialize(new { level_id = id, seed,
                    feedback, balance = BalanceStudy.Measure(level, 32) }));
                found++;
            }
            Require(found > 0, $"No bounded seed candidate for level {id}.");
        }
        return 0;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
