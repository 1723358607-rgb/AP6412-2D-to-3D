namespace AP6412Match3.Core;

public enum SelectionOutcome { Selected, Deselected, RequestAccepted, OutOfBounds, NotAdjacent, Blocked }
public readonly record struct SwapRequest(GridPosition From, GridPosition To);

/// <summary>Reusable selection/request state. Step 1 never swaps pieces or changes game rules.</summary>
public sealed class BoardInteraction
{
    public BoardModel Board { get; }
    public GridPosition? Selected { get; private set; }
    public SwapRequest? LastAcceptedRequest { get; private set; }
    public int AcceptedRequestCount { get; private set; }

    public BoardInteraction(BoardModel board) => Board = board ?? throw new ArgumentNullException(nameof(board));

    public SelectionOutcome Click(GridPosition position)
    {
        if (!Board.Contains(position))
            return SelectionOutcome.OutOfBounds;
        if (!Board.GetCell(position).IsMovable)
            return SelectionOutcome.Blocked;
        if (Selected == position)
        {
            Selected = null;
            return SelectionOutcome.Deselected;
        }
        if (!Selected.HasValue)
        {
            Selected = position;
            LastAcceptedRequest = null;
            return SelectionOutcome.Selected;
        }

        GridPosition from = Selected.Value;
        var validation = Board.ValidateSwap(from, position);
        if (validation != SwapValidation.Accepted)
            return validation == SwapValidation.NotAdjacent ? SelectionOutcome.NotAdjacent : SelectionOutcome.Blocked;

        LastAcceptedRequest = new SwapRequest(from, position);
        AcceptedRequestCount++;
        Selected = null;
        return SelectionOutcome.RequestAccepted;
    }
}
