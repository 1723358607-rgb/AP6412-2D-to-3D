namespace AP6412Match3.Core;

/// <summary>Step 1 deterministic initial board only. Refill/cascades/reshuffling are later work.</summary>
public sealed class BoardGenerator
{
    public BoardModel CreateInitialBoard(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);
        level.Validate();
        var cells = new BoardCell[level.Width, level.Height];
        foreach (var box in level.Boxes)
            cells[box.Column, box.Row] = BoardCell.Box(box.Layers);

        uint state = level.Seed;
        for (int row = 0; row < level.Height; row++)
        for (int column = 0; column < level.Width; column++)
        {
            if (cells[column, row].Kind == CellKind.Box)
                continue;

            // Reserved first-level opening: R B R above a red center.
            // Swapping (1,0) with (1,1) can later create R R R; no opening match exists.
            PieceColor? fixedColor = level.LevelId == 1 ? (column, row) switch
            {
                (0, 0) or (2, 0) or (1, 1) => PieceColor.Red,
                (1, 0) => PieceColor.Blue,
                _ => null,
            } : null;

            if (fixedColor.HasValue)
            {
                cells[column, row] = BoardCell.Piece(fixedColor.Value);
                continue;
            }

            // xorshift32 has explicit uint arithmetic, independent of .NET Random versions.
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            int first = (int)(state % 4);
            bool placed = false;
            for (int offset = 0; offset < 4; offset++)
            {
                var color = (PieceColor)((first + offset) % 4);
                bool horizontal = column >= 2 && IsSame(cells[column - 1, row], color) && IsSame(cells[column - 2, row], color);
                bool vertical = row >= 2 && IsSame(cells[column, row - 1], color) && IsSame(cells[column, row - 2], color);
                if (horizontal || vertical)
                    continue;
                cells[column, row] = BoardCell.Piece(color);
                placed = true;
                break;
            }
            if (!placed)
                throw new InvalidOperationException("No valid initial piece color available.");
        }
        return new BoardModel(cells);
    }

    private static bool IsSame(BoardCell cell, PieceColor color) =>
        cell.Kind == CellKind.Piece && cell.Color == color;
}
