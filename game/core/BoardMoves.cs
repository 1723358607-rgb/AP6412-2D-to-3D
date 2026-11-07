namespace AP6412Match3.Core;

/// <summary>Move availability without RNG consumption or speculative cascades.</summary>
public static class BoardMoves
{
    public static IReadOnlyList<SwapRequest> FindLegal(BoardModel board)
    {
        var moves = new List<SwapRequest>();
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        foreach (GridPosition offset in new[] { new GridPosition(1, 0), new GridPosition(0, 1) })
        {
            GridPosition from = new(x, y), to = new(x + offset.Column, y + offset.Row);
            if (board.ValidateSwap(from, to) != SwapValidation.Accepted || board.GetCell(from).Color == board.GetCell(to).Color) continue;
            BoardCell At(GridPosition p) => board.GetCell(p == from ? to : p == to ? from : p);
            bool MakesMatch(GridPosition p)
            {
                PieceColor color = At(p).Color;
                int Count(int dx, int dy)
                {
                    int count = 0; GridPosition next = new(p.Column + dx, p.Row + dy);
                    while (board.Contains(next) && At(next) is { Kind: CellKind.Piece } cell && cell.Color == color)
                    { count++; next = new(next.Column + dx, next.Row + dy); }
                    return count;
                }
                return 1 + Count(-1, 0) + Count(1, 0) >= 3 || 1 + Count(0, -1) + Count(0, 1) >= 3;
            }
            if (MakesMatch(from) || MakesMatch(to)) moves.Add(new(from, to));
        }
        return moves.AsReadOnly();
    }
}
