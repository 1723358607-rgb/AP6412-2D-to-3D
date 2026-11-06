namespace AP6412Match3.Core;

public enum PieceColor { Red, Blue, Green, Yellow }
public enum CellKind { Empty, Piece, Box }
public enum SwapValidation { Accepted, OutOfBounds, NotAdjacent, Blocked }

/// <summary>Zero-based domain coordinates; no screen pixels, Godot vectors or scene nodes.</summary>
public readonly record struct GridPosition(int Column, int Row);

public readonly record struct BoardCell
{
    public CellKind Kind { get; }
    public PieceColor Color { get; }
    public int BoxLayers { get; }
    public bool IsMovable => Kind == CellKind.Piece;

    private BoardCell(CellKind kind, PieceColor color, int layers) =>
        (Kind, Color, BoxLayers) = (kind, color, layers);

    public static BoardCell Piece(PieceColor color)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color));
        return new BoardCell(CellKind.Piece, color, 0);
    }

    public static BoardCell Box(int layers)
    {
        if (layers is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(layers));
        return new BoardCell(CellKind.Box, default, layers);
    }
}
