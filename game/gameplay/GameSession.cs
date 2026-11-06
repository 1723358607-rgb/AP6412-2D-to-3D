using Godot;
using AP6412Match3.Core;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Gameplay;

/// <summary>Owns real move/phase state and serializes logical transactions and cancellable presentation.</summary>
public partial class GameSession : Node
{
    [Signal] public delegate void StateChangedEventHandler(GDictionary snapshot);
    [Signal] public delegate void LevelFinishedEventHandler(GDictionary result);

    private static int _nextSessionId;
    private int _sessionId, _moves;
    private string _phase = "idle";
    private uint _randomState;
    private LevelDefinition? _level;
    private CancellationTokenSource _lifetime = new();
    private IBoardPresenter? _presenter;
    private readonly MatchResolver _resolver = new();

    public bool IsGameplayImplemented => true;
    public bool IsBusy => _phase == "resolving";
    public BoardModel? CurrentBoard { get; private set; }
    public SwapResolution? LastResolution { get; private set; }
    public string LastRequestNotice { get; private set; } = "Ready to start level 1.";

    public GDictionary GetSnapshot() => new()
    {
        ["session_id"] = _sessionId,
        ["level_id"] = _level?.LevelId ?? 0,
        ["phase"] = _phase,
        ["initial_moves"] = _level?.InitialMoves ?? 0,
        ["moves_remaining"] = _moves,
        ["target_total"] = _level?.Boxes.Length ?? 0,
        ["target_remaining"] = CountBoxes(),
        ["mode"] = "2d",
    };

    public void AttachPresenter(IBoardPresenter? presenter)
    {
        _presenter = presenter;
        if (CurrentBoard is not null) presenter?.SetBoard(CurrentBoard);
        presenter?.SetInteractive(_phase == "playing");
    }

    public void StartLevel(int levelId)
    {
        if (levelId is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(levelId));
        if (levelId != 1)
        {
            LastRequestNotice = "Levels 2 and 3 will be connected in step 3.";
            GD.Print($"[LEVEL_PENDING] {LastRequestNotice}");
            return;
        }
        var level = LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString("res://data/levels/level_01.json"));
        var board = new BoardGenerator().CreateInitialBoard(level, out uint state);
        RenewLifetime();
        _level = level;
        CurrentBoard = board;
        _randomState = state;
        _moves = level.InitialMoves;
        _phase = "playing";
        LastResolution = null;
        LastRequestNotice = "Level 1 started. Box damage and victory are pending step 3.";
        _presenter?.SetBoard(board);
        _presenter?.SetInteractive(true);
        PublishState();
    }

    public void RetryLevel()
    {
        if (_level is not null) StartLevel(_level.LevelId);
    }

    public void LeaveLevel()
    {
        RenewLifetime();
        _level = null;
        CurrentBoard = null;
        LastResolution = null;
        _moves = 0;
        _randomState = 0;
        _phase = "idle";
        _presenter?.ClearBoard();
        _presenter?.SetInteractive(false);
        PublishState();
    }

    public async Task<SwapAttemptStatus> RequestSwapAsync(GridPosition from, GridPosition to)
    {
        if (_phase != "playing" || CurrentBoard is null || _moves <= 0) return SwapAttemptStatus.Busy;
        BoardModel original = CurrentBoard;
        int previousMoves = _moves;
        uint previousRandomState = _randomState;
        SwapResolution? previousResolution = LastResolution;
        SwapResolution resolution;
        try { resolution = _resolver.ResolveSwap(original, from, to, _randomState); }
        catch (Exception error)
        {
            LastRequestNotice = error.Message;
            GD.PushError($"[RESOLUTION_ERROR] {error}");
            return SwapAttemptStatus.Error;
        }
        if (resolution.Status == SwapAttemptStatus.Invalid) return SwapAttemptStatus.Invalid;
        int id = _sessionId;
        CancellationToken token = _lifetime.Token;
        IBoardPresenter? presenter = _presenter;
        _phase = "resolving";
        if (resolution.Status == SwapAttemptStatus.Accepted) _moves--;
        presenter?.SetInteractive(false);
        PublishState();
        try
        {
            foreach (BoardStep step in resolution.Steps)
            {
                token.ThrowIfCancellationRequested();
                if (id != _sessionId) return SwapAttemptStatus.Cancelled;
                if (presenter is not null) await presenter.PresentAsync(step, token);
                token.ThrowIfCancellationRequested();
                if (id != _sessionId) return SwapAttemptStatus.Cancelled;
                CurrentBoard = step.After;
                presenter?.SetBoard(step.After);
            }
            _randomState = resolution.NextRandomState;
            CurrentBoard = resolution.FinalBoard;
            LastResolution = resolution;
            _phase = _moves == 0 ? "lost" : "playing";
            LastRequestNotice = resolution.Status == SwapAttemptStatus.NoMatch
                ? "没有形成匹配：已交换回去，不扣步。"
                : $"本次扣1步，连锁{resolution.Cascades}轮，消除{resolution.TotalCleared}枚。";
            presenter?.SetInteractive(_phase == "playing");
            PublishState();
            if (id == _sessionId && _phase == "lost")
            {
                GDictionary result = GetSnapshot();
                result["won"] = false;
                result["stars"] = 0;
                EmitSignal(SignalName.LevelFinished, result);
            }
            return resolution.Status;
        }
        catch (OperationCanceledException) { return SwapAttemptStatus.Cancelled; }
        catch (Exception error)
        {
            if (id != _sessionId) return SwapAttemptStatus.Cancelled;
            CurrentBoard = original;
            _moves = previousMoves;
            _randomState = previousRandomState;
            LastResolution = previousResolution;
            _phase = "playing";
            LastRequestNotice = "表现播放失败，本次操作已回退。";
            presenter?.SetBoard(original);
            presenter?.SetInteractive(true);
            PublishState();
            GD.PushError($"[PRESENTATION_ERROR] {error}");
            return SwapAttemptStatus.Error;
        }
    }

    private int CountBoxes()
    {
        if (CurrentBoard is null) return 0;
        int count = 0;
        for (int row = 0; row < CurrentBoard.Height; row++)
        for (int column = 0; column < CurrentBoard.Width; column++)
            if (CurrentBoard.GetCell(new GridPosition(column, row)).Kind == CellKind.Box) count++;
        return count;
    }

    private void RenewLifetime()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        _lifetime = new CancellationTokenSource();
        _sessionId = Interlocked.Increment(ref _nextSessionId);
    }

    private void PublishState() => EmitSignal(SignalName.StateChanged, GetSnapshot());

    public override void _ExitTree()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
