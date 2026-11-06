namespace AP6412Match3.Core;

/// <summary>Pure swap/match/fall/refill transaction. Box damage is deferred to step 3.</summary>
public sealed class MatchResolver
{
    public HashSet<GridPosition> FindMatches(BoardModel board)
    {
        var matches = new HashSet<GridPosition>();
        for (int row = 0; row < board.Height; row++)
        {
            int start = 0;
            while (start < board.Width)
            {
                BoardCell cell = board.GetCell(new GridPosition(start, row));
                int end = start + 1;
                while (end < board.Width && Same(cell, board.GetCell(new GridPosition(end, row)))) end++;
                if (cell.Kind == CellKind.Piece && end - start >= 3)
                    for (int column = start; column < end; column++) matches.Add(new GridPosition(column, row));
                start = end;
            }
        }
        for (int column = 0; column < board.Width; column++)
        {
            int start = 0;
            while (start < board.Height)
            {
                BoardCell cell = board.GetCell(new GridPosition(column, start));
                int end = start + 1;
                while (end < board.Height && Same(cell, board.GetCell(new GridPosition(column, end)))) end++;
                if (cell.Kind == CellKind.Piece && end - start >= 3)
                    for (int row = start; row < end; row++) matches.Add(new GridPosition(column, row));
                start = end;
            }
        }
        return matches;
    }

    public SwapResolution ResolveSwap(BoardModel board, GridPosition from, GridPosition to, uint randomState, int maxCascades = 64)
    {
        if (maxCascades < 1) throw new ArgumentOutOfRangeException(nameof(maxCascades));
        if (board.ValidateSwap(from, to) != SwapValidation.Accepted)
            return new SwapResolution(SwapAttemptStatus.Invalid, board, randomState, [], 0, 0);
        BoardModel swapped = Swap(board, from, to);
        var steps = new List<BoardStep>
        {
            new(BoardStepKind.Swap, board, swapped, [],
                new[] { new PieceMotion(from, to, board.GetCell(from)), new PieceMotion(to, from, board.GetCell(to)) }),
        };
        var matched = FindMatches(swapped);
        if (matched.Count == 0 || (!matched.Contains(from) && !matched.Contains(to)))
        {
            steps.Add(new BoardStep(BoardStepKind.SwapBack, swapped, board, [],
                new[] { new PieceMotion(to, from, board.GetCell(from)), new PieceMotion(from, to, board.GetCell(to)) }));
            return new SwapResolution(SwapAttemptStatus.NoMatch, board, randomState, steps.AsReadOnly(), 0, 0);
        }
        int cascades = 0, total = 0;
        uint state = randomState;
        BoardModel current = swapped;
        var generator = new BoardGenerator();
        while (matched.Count != 0)
        {
            if (++cascades > maxCascades)
                throw new InvalidOperationException("Cascade limit reached; transaction was not committed.");
            var cells = current.CopyCells();
            foreach (var position in matched) cells[position.Column, position.Row] = default;
            var cleared = new BoardModel(cells);
            var positions = matched.OrderBy(p => p.Row).ThenBy(p => p.Column).ToArray();
            steps.Add(new BoardStep(BoardStepKind.Clear, current, cleared, Array.AsReadOnly(positions), []));
            total += positions.Length;
            var fall = ApplyGravity(cleared);
            steps.Add(fall);
            var refill = generator.Refill(fall.After, ref state);
            steps.Add(refill);
            current = refill.After;
            matched = FindMatches(current);
        }
        return new SwapResolution(SwapAttemptStatus.Accepted, current, state, steps.AsReadOnly(), cascades, total);
    }

    public BoardStep ApplyGravity(BoardModel board)
    {
        var cells = board.CopyCells();
        var motions = new List<PieceMotion>();
        for (int column = 0; column < board.Width; column++)
        {
            int start = 0;
            while (start < board.Height)
            {
                if (cells[column, start].Kind == CellKind.Box) { start++; continue; }
                int end = start;
                while (end + 1 < board.Height && cells[column, end + 1].Kind != CellKind.Box) end++;
                int destination = end;
                for (int source = end; source >= start; source--)
                {
                    var cell = cells[column, source];
                    if (cell.Kind != CellKind.Piece) continue;
                    cells[column, source] = default;
                    cells[column, destination] = cell;
                    if (destination != source)
                        motions.Add(new PieceMotion(new GridPosition(column, source), new GridPosition(column, destination), cell));
                    destination--;
                }
                for (int row = start; row <= destination; row++) cells[column, row] = default;
                start = end + 1;
            }
        }
        return new BoardStep(BoardStepKind.Fall, board, new BoardModel(cells), [], motions.AsReadOnly());
    }

    private static BoardModel Swap(BoardModel board, GridPosition from, GridPosition to)
    {
        var cells = board.CopyCells();
        (cells[from.Column, from.Row], cells[to.Column, to.Row]) = (cells[to.Column, to.Row], cells[from.Column, from.Row]);
        return new BoardModel(cells);
    }

    private static bool Same(BoardCell a, BoardCell b) => a.Kind == CellKind.Piece && b.Kind == CellKind.Piece && a.Color == b.Color;
}
