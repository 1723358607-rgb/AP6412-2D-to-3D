namespace AP6412Match3.Core;

/// <summary>Engine-independent immutable board snapshot. Views cannot mutate cell state.</summary>
public sealed class BoardModel
{
    private readonly BoardCell[,] _cells;
    public int Width => _cells.GetLength(0);
    public int Height => _cells.GetLength(1);

    public BoardModel(BoardCell[,] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.GetLength(0) == 0 || cells.GetLength(1) == 0)
            throw new ArgumentException("A board must contain cells.", nameof(cells));
        _cells = (BoardCell[,])cells.Clone();
    }

    public bool Contains(GridPosition position) =>
        position.Column >= 0 && position.Column < Width && position.Row >= 0 && position.Row < Height;

    public BoardCell GetCell(GridPosition position)
    {
        if (!Contains(position))
            throw new ArgumentOutOfRangeException(nameof(position));
        return _cells[position.Column, position.Row];
    }

    public SwapValidation ValidateSwap(GridPosition from, GridPosition to)
    {
        if (!Contains(from) || !Contains(to))
            return SwapValidation.OutOfBounds;
        if (Math.Abs(from.Column - to.Column) + Math.Abs(from.Row - to.Row) != 1)
            return SwapValidation.NotAdjacent;
        if (!GetCell(from).IsMovable || !GetCell(to).IsMovable)
            return SwapValidation.Blocked;
        return SwapValidation.Accepted;
    }

    public BoardCell[,] CopyCells() => (BoardCell[,])_cells.Clone();
}
