using Godot;
using AP6412Match3.Core;

namespace AP6412Match3.Tests.Gameplay;

public static class BalanceStudy
{
    public sealed record PlayedMove(int from_col, int from_row, int to_col, int to_row, int remaining, int targets, int cascades, int reshuffles);
    public sealed record Playthrough(bool won, int stars, int remaining, IReadOnlyList<PlayedMove> moves);
    public static LevelDefinition Load(int id, uint? seed = null)
    {
        var l = LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString($"res://data/levels/level_{id:00}.json"));
        return seed.HasValue ? new LevelDefinition { LevelId=l.LevelId, Name=l.Name, Width=7, Height=7, InitialMoves=l.InitialMoves, Seed=seed.Value, Boxes=l.Boxes } : l;
    }
    public static Playthrough Play(LevelDefinition level, uint policy, bool greedy)
    {
        var resolver = new MatchResolver();
        BoardModel board = new BoardGenerator().CreateInitialBoard(level, out uint random);
        var path = new List<PlayedMove>();
        int remaining = level.InitialMoves;
        while (remaining > 0 && LevelRules.CountBoxes(board) > 0)
        {
            var legal = BoardMoves.FindLegal(board);
            if (legal.Count == 0) throw new InvalidOperationException("Stable playable session became a dead board.");
            policy ^= policy << 13; policy ^= policy >> 17; policy ^= policy << 5;
            SwapRequest chosen = legal[(int)(policy % (uint)legal.Count)];
            SwapResolution result;
            if (greedy)
            {
                int Layers(BoardModel b) => Enumerable.Range(0,b.Height).Sum(y => Enumerable.Range(0,b.Width).Sum(x => b.GetCell(new(x,y)).BoxLayers));
                var ranked = legal.Select(m => (Move:m, Result:resolver.ResolveSwap(board,m.From,m.To,random,ensurePlayable:remaining>1)))
                    .OrderByDescending(x => (LevelRules.CountBoxes(board)-LevelRules.CountBoxes(x.Result.FinalBoard))*300 +
                        (Layers(board)-Layers(x.Result.FinalBoard))*100 + x.Result.TotalCleared).ToArray();
                chosen = ranked[0].Move; result = ranked[0].Result;
            }
            else result = resolver.ResolveSwap(board, chosen.From, chosen.To, random, ensurePlayable:remaining>1);
            if (result.Status != SwapAttemptStatus.Accepted) throw new InvalidOperationException("Legal-move detector disagreed with resolver.");
            remaining--; board=result.FinalBoard; random=result.NextRandomState;
            path.Add(new(chosen.From.Column,chosen.From.Row,chosen.To.Column,chosen.To.Row,remaining,LevelRules.CountBoxes(board),result.Cascades,result.Reshuffles));
        }
        bool won=LevelRules.CountBoxes(board)==0;
        return new(won,LevelRules.Stars(level.InitialMoves,remaining,won),remaining,path.AsReadOnly());
    }
    public static object Measure(LevelDefinition level, int trials)
    {
        var runs=Enumerable.Range(0,trials).Select(i=>Play(level,(uint)(1009+i*137),false)).ToArray();
        var greedy=Play(level,997,true);
        return new { level_id=level.LevelId, seed=level.Seed, initial_moves=level.InitialMoves, trials,
            random_wins=runs.Count(r=>r.won), random_stars=new[]{0,1,2,3}.Select(s=>runs.Count(r=>r.stars==s)),
            greedy_won=greedy.won, greedy_moves=greedy.moves.Count, greedy_stars=greedy.stars,
            one_move_wins=CountOneMoveWins(level), total_reshuffles=runs.Sum(r=>r.moves.Sum(m=>m.reshuffles)) };
    }
    public static int CountOneMoveWins(LevelDefinition level)
    {
        var board=new BoardGenerator().CreateInitialBoard(level,out uint random);
        return BoardMoves.FindLegal(board).Count(m=>LevelRules.CountBoxes(new MatchResolver().ResolveSwap(board,m.From,m.To,random).FinalBoard)==0);
    }
    public static int Run()
    {
        try
        {
            foreach(int id in new[]{1,2,3}) GD.Print("[BALANCE_BASELINE] "+System.Text.Json.JsonSerializer.Serialize(Measure(Load(id),64)));
            for(uint seed=641202;seed<=641226;seed++) GD.Print("[BALANCE_CANDIDATE] "+System.Text.Json.JsonSerializer.Serialize(Measure(Load(2,seed),32)));
            return 0;
        }
        catch(Exception e) { GD.PushError(e.ToString());return 1; }
    }
}
