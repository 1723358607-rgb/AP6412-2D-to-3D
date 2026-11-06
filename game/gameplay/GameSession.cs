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
    private bool _resultPublished;
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
        var level = LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString($"res://data/levels/level_{levelId:00}.json"));
        var board = new BoardGenerator().CreateInitialBoard(level, out uint state);
        BeginLevel(level, board, state);
    }

    // Shared composition boundary for supplied level data; isolated rule checks use their own fixtures.
    // The production path above always loads the three checked-in configurations, never test boards.
    internal void BeginLevel(LevelDefinition level, BoardModel board, uint randomState)
    {
        level.Validate();
        if (randomState == 0 || board.Width != level.Width || board.Height != level.Height)
            throw new ArgumentException("Invalid initial board or random state.");
        var boxes = level.Boxes.ToDictionary(b => new GridPosition(b.Column, b.Row), b => b.Layers);
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++)
        {
            var position = new GridPosition(x, y);
            BoardCell cell = board.GetCell(position);
            if (cell.Kind == CellKind.Empty || (boxes.TryGetValue(position, out int layers)
                ? cell.Kind != CellKind.Box || cell.BoxLayers != layers : cell.Kind != CellKind.Piece))
                throw new ArgumentException("Initial cells must agree with the supplied level configuration.");
        }
        if (_resolver.FindMatches(board).Count != 0) throw new ArgumentException("Initial board must be stable.");
        RenewLifetime();
        _level = level;
        CurrentBoard = board;
        _randomState = randomState;
        _moves = level.InitialMoves;
        _phase = "playing";
        LastResolution = null;
        _resultPublished = false;
        LastRequestNotice = $"第{level.LevelId}关开始 · {level.Name}。消除木箱旁的棋子即可伤害木箱。";
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
        _resultPublished = false;
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
                if (step.Kind == BoardStepKind.Clear) PublishState();
            }
            token.ThrowIfCancellationRequested();
            if (id != _sessionId) return SwapAttemptStatus.Cancelled;
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
        _randomState = resolution.NextRandomState;
        CurrentBoard = resolution.FinalBoard;
        LastResolution = resolution;
        _phase = LevelRules.PhaseAfterCascades(CountBoxes(), _moves);
        LastRequestNotice = resolution.Status == SwapAttemptStatus.NoMatch
            ? "没有形成匹配：已交换回去，不扣步。"
            : $"本次扣1步，连锁{resolution.Cascades}轮，消除{resolution.TotalCleared}枚。";
        bool terminal = _phase is "won" or "lost";
        GDictionary? result = null;
        if (terminal && !_resultPublished)
        {
            _resultPublished = true;
            result = GetSnapshot();
            bool won = _phase == "won";
            result["won"] = won;
            result["stars"] = LevelRules.Stars(_level!.InitialMoves, _moves, won);
        }
        presenter?.SetInteractive(!terminal);
        PublishState();
        // A state listener may synchronously leave or retry; never publish an old session's result.
        if (result is not null && id == _sessionId) EmitSignal(SignalName.LevelFinished, result);
        return resolution.Status;
    }

    private int CountBoxes() => CurrentBoard is null ? 0 : LevelRules.CountBoxes(CurrentBoard);

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
