using AP6412Match3.Core;

namespace AP6412Match3.Gameplay;

/// <summary>A-owned internal board adapter; B's public snapshot contract remains unchanged.</summary>
public interface IBoardPresenter
{
    void SetBoard(BoardModel board);
    void SetInteractive(bool enabled);
    void ClearBoard();
    Task PresentAsync(BoardStep step, CancellationToken cancellationToken);
}
