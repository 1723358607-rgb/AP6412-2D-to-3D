namespace AP6412Match3.Core;

public enum BoardStepKind { Swap, SwapBack, Clear, Fall, Refill }
public enum SwapAttemptStatus { Accepted, NoMatch, Invalid, Busy, Cancelled, Error }
public readonly record struct PieceMotion(GridPosition From, GridPosition To, BoardCell Cell, bool Spawned = false);

/// <summary>Logical snapshots and motion descriptions, independent of any renderer.</summary>
public sealed record BoardStep(BoardStepKind Kind, BoardModel Before, BoardModel After,
    IReadOnlyList<GridPosition> Cleared, IReadOnlyList<PieceMotion> Motions);

public sealed record SwapResolution(SwapAttemptStatus Status, BoardModel FinalBoard, uint NextRandomState,
    IReadOnlyList<BoardStep> Steps, int Cascades, int TotalCleared);
