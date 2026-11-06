namespace AP6412Match3.Core;

/// <summary>Post-cascade outcome and integer star thresholds shared by 2D and future 3D.</summary>
public static class LevelRules
{
    public static int CountBoxes(BoardModel board)
    {
        int count = 0;
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
            if (board.GetCell(new GridPosition(x, y)).Kind == CellKind.Box) count++;
        return count;
    }

    public static string PhaseAfterCascades(int remainingBoxes, int remainingMoves)
    {
        if (remainingBoxes < 0 || remainingMoves < 0) throw new ArgumentOutOfRangeException();
        // This order is intentional: completing the target on the last move wins.
        return remainingBoxes == 0 ? "won" : remainingMoves == 0 ? "lost" : "playing";
    }

    public static int Stars(int initialMoves, int remainingMoves, bool won)
    {
        if (initialMoves <= 0 || remainingMoves < 0 || remainingMoves > initialMoves) throw new ArgumentOutOfRangeException();
        if (!won) return 0;
        if ((long)remainingMoves * 5 >= (long)initialMoves * 2) return 3;
        if ((long)remainingMoves * 5 >= initialMoves) return 2;
        return 1;
    }
}
