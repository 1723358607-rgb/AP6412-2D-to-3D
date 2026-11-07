namespace AP6412Match3.Core;

/// <summary>Pure swap/match/box-damage/fall/refill transaction; presentation never decides results.</summary>
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

    public SwapResolution ResolveSwap(BoardModel board, GridPosition from, GridPosition to, uint randomState, int maxCascades = 64, bool ensurePlayable = true)
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
            BoardStep clear = ClearMatches(current, matched);
            steps.Add(clear);
            total += clear.Cleared.Count;
            var fall = ApplyGravity(clear.After);
            steps.Add(fall);
            var refill = generator.Refill(fall.After, ref state);
            steps.Add(refill);
            current = refill.After;
            matched = FindMatches(current);
        }
        int reshuffles = 0;
        if (ensurePlayable && LevelRules.CountBoxes(current) > 0)
        {
            ShuffleResult ready = generator.EnsurePlayable(current, state);
            if (ready.Attempts > 0)
            {
                steps.Add(new BoardStep(BoardStepKind.Reshuffle, current, ready.Board, [], []));
                current = ready.Board; state = ready.NextRandomState; reshuffles = 1;
            }
        }
        return new SwapResolution(SwapAttemptStatus.Accepted, current, state, steps.AsReadOnly(), cascades, total, reshuffles);
    }

    public BoardStep ClearMatches(BoardModel board, IReadOnlySet<GridPosition> matched)
    {
        var damaged = new HashSet<GridPosition>();
        foreach (GridPosition position in matched)
        {
            if (!board.Contains(position) || !board.GetCell(position).IsMovable)
                throw new ArgumentException("Only ordinary pieces can be cleared.", nameof(matched));
            foreach (var offset in new[] { new GridPosition(-1, 0), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(0, 1) })
            {
                var adjacent = new GridPosition(position.Column + offset.Column, position.Row + offset.Row);
                if (board.Contains(adjacent) && board.GetCell(adjacent).Kind == CellKind.Box) damaged.Add(adjacent);
            }
        }
        var cells = board.CopyCells();
        foreach (GridPosition position in matched) cells[position.Column, position.Row] = default;
        foreach (GridPosition box in damaged)
        {
            int layers = board.GetCell(box).BoxLayers;
            cells[box.Column, box.Row] = layers == 1 ? default : BoardCell.Box(layers - 1);
        }
        var clearedPositions = matched.OrderBy(p => p.Row).ThenBy(p => p.Column).ToArray();
        var damagedPositions = damaged.OrderBy(p => p.Row).ThenBy(p => p.Column).ToArray();
        return new BoardStep(BoardStepKind.Clear, board, new BoardModel(cells),
            Array.AsReadOnly(clearedPositions), [], Array.AsReadOnly(damagedPositions));
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
