using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using AP6412Match3.Presentation;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Core invariants and actual scene/mapping checks. Does not claim GUI or match-3 completion.</summary>
public static class StepOneSmokeCheck
{
    public static int Run(Node host, GameSession session)
    {
        BoardPrototypeRunner? harness = null;
        try
        {
            string json = Godot.FileAccess.GetFileAsString(BoardPrototypeRunner.LevelPath);
            var level = LevelDefinition.FromJson(json);
            var generator = new BoardGenerator();
            BoardModel board = generator.CreateInitialBoard(level);
            Require(board.Width == 7 && board.Height == 7, "Expected 7x7 board.");
            int pieces = 0, boxes = 0;
            var colors = new HashSet<PieceColor>();
            for (int row = 0; row < 7; row++)
            for (int column = 0; column < 7; column++)
            {
                BoardCell cell = board.GetCell(new GridPosition(column, row));
                bool expectedBox = row == 6 && column is >= 1 and <= 5;
                Require((cell.Kind == CellKind.Box) == expectedBox, $"Wrong box at ({column},{row}).");
                if (cell.Kind == CellKind.Box)
                {
                    boxes++;
                    Require(cell.BoxLayers == 1, "First-level boxes must have one layer.");
                }
                else
                {
                    Require(cell.Kind == CellKind.Piece, "Unexpected empty cell.");
                    pieces++;
                    colors.Add(cell.Color);
                }
            }
            Require(pieces == 44 && boxes == 5 && colors.Count == 4, "Incorrect board composition.");
            Require(level.InitialMoves == 15, "Level data changed without design update.");
            Require(Dump(board) == Dump(generator.CreateInitialBoard(level)), "Same seed must reproduce the board.");
            Require(!HasMatch(board), "Opening board contains a ready-made match.");
            Require(board.GetCell(new GridPosition(0, 0)).Color == PieceColor.Red &&
                    board.GetCell(new GridPosition(1, 0)).Color == PieceColor.Blue &&
                    board.GetCell(new GridPosition(2, 0)).Color == PieceColor.Red &&
                    board.GetCell(new GridPosition(1, 1)).Color == PieceColor.Red,
                "Reserved opening no longer provides the future RRR swap.");
            for (uint seed = 1; seed <= 100; seed++)
            {
                var sample = new LevelDefinition { LevelId = 1, Name = level.Name, Width = 7, Height = 7, InitialMoves = 15, Seed = seed, Boxes = level.Boxes };
                Require(!HasMatch(generator.CreateInitialBoard(sample)), $"Opening match for seed {seed}.");
            }

            var a = new GridPosition(0, 0);
            Require(board.ValidateSwap(a, new GridPosition(1, 0)) == SwapValidation.Accepted, "Adjacent ordinary pieces rejected.");
            Require(board.ValidateSwap(a, new GridPosition(1, 1)) == SwapValidation.NotAdjacent, "Diagonal accepted.");
            Require(board.ValidateSwap(a, new GridPosition(6, 0)) == SwapValidation.NotAdjacent, "Distant cells accepted.");
            Require(board.ValidateSwap(a, a) == SwapValidation.NotAdjacent, "Same-cell swap accepted.");
            Require(board.ValidateSwap(a, new GridPosition(-1, 0)) == SwapValidation.OutOfBounds, "Out-of-bounds accepted.");
            Require(board.ValidateSwap(new GridPosition(1, 5), new GridPosition(1, 6)) == SwapValidation.Blocked, "Box swap accepted.");
            var sparse = new BoardModel(new BoardCell[,] { { BoardCell.Piece(PieceColor.Red) }, { default } });
            Require(sparse.ValidateSwap(new GridPosition(0, 0), new GridPosition(1, 0)) == SwapValidation.Blocked, "Empty cell swap accepted.");

            string before = Dump(board);
            var interaction = new BoardInteraction(board);
            Require(interaction.Click(a) == SelectionOutcome.Selected, "Selection failed.");
            Require(interaction.Click(a) == SelectionOutcome.Deselected, "Deselect failed.");
            interaction.Click(a);
            Require(interaction.Click(new GridPosition(1, 1)) == SelectionOutcome.NotAdjacent && interaction.Selected == a, "Invalid click should preserve selection.");
            Require(interaction.Click(new GridPosition(1, 6)) == SelectionOutcome.Blocked && interaction.Selected == a, "Box click should preserve selection.");
            Require(interaction.Click(new GridPosition(-1, 0)) == SelectionOutcome.OutOfBounds, "Outside selection accepted.");
            Require(interaction.Click(new GridPosition(1, 0)) == SelectionOutcome.RequestAccepted, "Request not recorded.");
            Require(interaction.AcceptedRequestCount == 1 && interaction.Selected is null, "Accepted request lifecycle wrong.");
            Require(Dump(board) == before, "Step 1 request must not execute or mutate a swap.");
            var copied = board.CopyCells();
            copied[0, 0] = BoardCell.Box(2);
            Require(Dump(board) == before, "External array mutation leaked into board.");

            harness = GD.Load<PackedScene>("res://presentation/2d/BoardPrototype.tscn").Instantiate<BoardPrototypeRunner>();
            host.AddChild(harness);
            BoardView2D view = harness.BoardView;
            for (int row = 0; row < 7; row++)
            for (int column = 0; column < 7; column++)
            {
                var point = view.CellCenter(new GridPosition(column, row));
                Require(view.TryGetCellAtLocalPosition(point, out var position) && position == new GridPosition(column, row), "Cell center mapping wrong.");
            }
            Vector2 origin = view.CellCenter(new(0, 0)) - Vector2.One * BoardView2D.CellSize / 2;
            Require(!view.TryGetCellAtLocalPosition(origin + new Vector2(-1, 20), out _), "Outside point mapped.");
            Require(!view.TryGetCellAtLocalPosition(origin + new Vector2(BoardView2D.CellSize + 1, 20), out _), "Gap mapped as a cell.");
            Require(!view.TryGetCellAtLocalPosition(origin + view.BoardSize, out _), "Outside edge mapped.");
            Require(session.GetSnapshot()["phase"].AsString() == "idle" && session.GetSnapshot()["moves_remaining"].AsInt32() == 0,
                "Independent prototype must not fake GameSession or decrement moves.");

            GD.Print("[STEP1_SMOKE_PASS] 49 cells, exact 5 boxes, 4 colors, deterministic openings, selection, rejected requests, immutable board and 49-cell hit mapping.");
            return 0;
        }
        catch (Exception error)
        {
            GD.PushError($"[STEP1_SMOKE_FAIL] {error}");
            return 1;
        }
        finally
        {
            harness?.Free();
        }
    }

    private static bool HasMatch(BoardModel board)
    {
        for (int row = 0; row < board.Height; row++)
        for (int column = 0; column < board.Width; column++)
        {
            var cell = board.GetCell(new GridPosition(column, row));
            if (cell.Kind != CellKind.Piece)
                continue;
            bool Same(int x, int y) { var next = board.GetCell(new GridPosition(x, y)); return next.Kind == CellKind.Piece && next.Color == cell.Color; }
            if (column + 2 < board.Width && Same(column + 1, row) && Same(column + 2, row))
                return true;
            if (row + 2 < board.Height && Same(column, row + 1) && Same(column, row + 2))
                return true;
        }
        return false;
    }

    private static string Dump(BoardModel board)
    {
        var rows = new string[board.Height];
        for (int row = 0; row < board.Height; row++)
        {
            var cells = new char[board.Width];
            for (int column = 0; column < board.Width; column++)
            {
                var cell = board.GetCell(new GridPosition(column, row));
                cells[column] = cell.Kind == CellKind.Box ? '#' : (char)('0' + (int)cell.Color);
            }
            rows[row] = new string(cells);
        }
        return string.Join("/", rows);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
