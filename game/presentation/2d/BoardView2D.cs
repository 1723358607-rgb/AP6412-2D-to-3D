using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;

namespace AP6412Match3.Presentation;

/// <summary>Board-only drawing/input adapter; no menus, HUD, match resolution or session state.</summary>
public partial class BoardView2D : Node2D, IBoardPresenter
{
    private const string GameplayAssetRoot = "res://assets/CrystalGarden_Gameplay_Assets";
    private readonly Dictionary<PieceColor, TextureVisual> _pieceVisuals = [];
    private TextureVisual? _crateVisual;
    private BoardStep? _animation;
    private float _animationProgress;
    private int _animationVersion;
    private bool _interactive;
    private bool _prototype;
    private bool _debugGrid;
    public const float CellSize = 70;
    public const float Gap = 6;
    // Kept for the historical prototype test contract; production frame mapping uses GridPitch.
    public const float Pitch = CellSize + Gap;
    public static Vector2 GridPitch => CrystalGardenBoardLayout.Pitch;
    public BoardInteraction? Interaction { get; private set; }
    public string LastMessage { get; private set; } = "点击普通棋子，然后点击相邻棋子。";

    [Signal] public delegate void FeedbackChangedEventHandler(string message);
    [Signal] public delegate void SwapRequestedEventHandler(Vector2I from, Vector2I to);

    public override void _Ready()
    {
        // Source regions are the measured main Alpha silhouettes (thresholded to ignore sparse glow pixels).
        // Drawing cropped regions removes large transparent margins while preserving each gem's aspect ratio.
        _pieceVisuals[PieceColor.Red] = LoadVisual("gem_heart_pink.png", new Rect2(108, 308, 1022, 884));
        _pieceVisuals[PieceColor.Blue] = LoadVisual("gem_teardrop_blue.png", new Rect2(98, 218, 758, 992));
        _pieceVisuals[PieceColor.Green] = LoadVisual("gem_diamond_purple.png", new Rect2(152, 196, 832, 1010));
        _pieceVisuals[PieceColor.Yellow] = LoadVisual("gem_hexagon_gold.png", new Rect2(204, 242, 886, 952));
        _crateVisual = LoadVisual("obstacle_wooden_crate.png", new Rect2(222, 142, 536, 512));
        _debugGrid = OS.GetCmdlineUserArgs().Contains("--board-grid-debug");
        SetProcessUnhandledInput(false);
    }

    public override void _EnterTree()
    {
        GetViewport().SizeChanged += UpdateLayout;
        if (GetParent() is Control host) host.Resized += UpdateLayout;
    }

    public void Bind(BoardModel board)
    {
        _prototype = true;
        SetBoard(board);
        SetInteractive(true);
    }

    public void SetBoard(BoardModel board)
    {
        _animationVersion++;
        _animation = null;
        Interaction = new BoardInteraction(board);
        LastMessage = "点击普通棋子，然后点击相邻棋子。";
        UpdateLayout();
        Visible = true;
        QueueRedraw();
    }

    public void SetInteractive(bool enabled)
    {
        _interactive = enabled;
        SetProcessUnhandledInput(enabled);
    }

    public void ClearBoard()
    {
        _animationVersion++;
        _animation = null;
        Interaction = null;
        SetInteractive(false);
        Hide();
        QueueRedraw();
    }

    public async Task PresentAsync(BoardStep step, CancellationToken cancellationToken)
    {
        int version = ++_animationVersion;
        _animation = step;
        _animationProgress = 0;
        double duration = step.Kind == BoardStepKind.Clear ? 0.18 : 0.16;
        double elapsed = 0;
        try
        {
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (version != _animationVersion) throw new OperationCanceledException();
                _animationProgress = (float)(elapsed / duration);
                QueueRedraw();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                elapsed += GetProcessDeltaTime();
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            if (version == _animationVersion)
            {
                _animation = null;
                QueueRedraw();
            }
        }
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= UpdateLayout;
        if (GetParent() is Control host) host.Resized -= UpdateLayout;
    }

    private Rect2 BoardBounds => Interaction is null ? new Rect2() : new Rect2(
        CrystalGardenBoardLayout.FirstCellCenter - Vector2.One * CellSize / 2,
        new Vector2(
            (Interaction.Board.Width - 1) * GridPitch.X + CellSize,
            (Interaction.Board.Height - 1) * GridPitch.Y + CellSize));

    public Vector2 BoardSize => BoardBounds.Size;

    private void UpdateLayout()
    {
        if (GetParent() is Control boardHost && boardHost.Name == "BoardHost")
            Position = Vector2.Zero;
        else
        {
            Vector2 viewport = GetParent() is Control host ? host.Size : GetViewportRect().Size;
            Position = (viewport - BoardBounds.Size) / 2 - BoardBounds.Position
                + (GetParent() is Control ? Vector2.Zero : new Vector2(0, 16));
        }
        QueueRedraw();
    }

    public bool TryGetCellAtLocalPosition(Vector2 point, out GridPosition position)
    {
        position = default;
        Rect2 bounds = BoardBounds;
        if (Interaction is null || !bounds.HasPoint(point))
            return false;
        Vector2 relative = point - bounds.Position;
        int column = Mathf.FloorToInt(relative.X / GridPitch.X);
        int row = Mathf.FloorToInt(relative.Y / GridPitch.Y);
        if (relative.X - column * GridPitch.X >= CellSize || relative.Y - row * GridPitch.Y >= CellSize)
            return false;
        position = new GridPosition(column, row);
        return Interaction.Board.Contains(position);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_interactive || !IsVisibleInTree() || Interaction is null || @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
            return;
        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
        if (!TryGetCellAtLocalPosition(local, out var position))
            return;

        SelectionOutcome outcome = Interaction.Click(position);
        string cell = $"第 {position.Row + 1} 行 · 第 {position.Column + 1} 列";
        LastMessage = outcome switch
        {
            SelectionOutcome.Selected => $"已选中 {cell}。请选择上下左右相邻棋子。",
            SelectionOutcome.Deselected => "已取消选择。",
            SelectionOutcome.Blocked => "木箱不能选中或交换；原有选择保持不变。",
            SelectionOutcome.NotAdjacent => "交换请求被拒绝：只允许上下左右相邻格。",
            SelectionOutcome.RequestAccepted => _prototype ? "邻接交换请求已记录。此第1步场景不执行消除。" : "正在处理交换，请等待消除和连锁结束。",
            _ => "请求被拒绝。",
        };
        if (outcome == SelectionOutcome.RequestAccepted && Interaction.LastAcceptedRequest is { } request)
        {
            EmitSignal(SignalName.SwapRequested,
                new Vector2I(request.From.Column, request.From.Row),
                new Vector2I(request.To.Column, request.To.Row));
            GD.Print($"[SWAP_REQUEST] ({request.From.Column},{request.From.Row}) -> ({request.To.Column},{request.To.Row}); prototype={_prototype}");
        }
        EmitSignal(SignalName.FeedbackChanged, LastMessage);
        QueueRedraw();
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (Interaction is null)
            return;
        BoardModel board = _animation is { Kind: BoardStepKind.Clear } clear ? clear.Before : _animation?.After ?? Interaction.Board;
        float progress = Mathf.SmoothStep(0, 1, _animationProgress);
        var movingDestinations = _animation?.Motions.Select(m => m.To).ToHashSet() ?? [];
        for (int row = 0; row < board.Height; row++)
        for (int column = 0; column < board.Width; column++)
        {
            var position = new GridPosition(column, row);
            Vector2 center = CrystalGardenBoardLayout.FirstCellCenter
                + new Vector2(column * GridPitch.X, row * GridPitch.Y);
            var rect = new Rect2(center - Vector2.One * CellSize / 2, Vector2.One * CellSize);
            BoardCell cell = board.GetCell(position);
            if (cell.Kind == CellKind.Box)
            {
                if (_crateVisual is { } crate)
                    DrawVisual(crate, center, 62);
            }
            else if (cell.Kind == CellKind.Piece && !movingDestinations.Contains(position))
            {
                float scale = _animation?.Kind == BoardStepKind.Clear && _animation.Cleared.Contains(position) ? 1 - progress : 1;
                DrawSetTransform(center, 0, Vector2.One * scale);
                DrawPiece(Vector2.Zero, cell.Color);
                DrawSetTransform(Vector2.Zero);
            }

            if (Interaction.LastAcceptedRequest is { } request && (request.From == position || request.To == position))
                DrawRect(rect.Grow(-1), new Color("63e4d7"), false, 3);
            if (Interaction.Selected == position)
                DrawRect(rect.Grow(-1), new Color("fff0a5"), false, 3);
        }

        if (_animation is not null)
        foreach (PieceMotion motion in _animation.Motions)
        {
            Vector2 destination = CrystalGardenBoardLayout.FirstCellCenter
                + new Vector2(motion.To.Column * GridPitch.X, motion.To.Row * GridPitch.Y);
            Vector2 source = motion.Spawned ? destination + new Vector2(0, -14) :
                CrystalGardenBoardLayout.FirstCellCenter
                + new Vector2(motion.From.Column * GridPitch.X, motion.From.Row * GridPitch.Y);
            DrawSetTransform(source.Lerp(destination, progress), 0, Vector2.One * (motion.Spawned ? Mathf.Lerp(0.1f, 1, progress) : 1));
            DrawPiece(Vector2.Zero, motion.Cell.Color);
            DrawSetTransform(Vector2.Zero);
        }

        if (_debugGrid)
            DrawGridDebug(board);

    }

    private void DrawGridDebug(BoardModel board)
    {
        for (int row = 0; row < board.Height; row++)
        for (int column = 0; column < board.Width; column++)
        {
            Vector2 center = CrystalGardenBoardLayout.FirstCellCenter
                + new Vector2(column * GridPitch.X, row * GridPitch.Y);
            Rect2 hitRect = new(center - Vector2.One * CellSize / 2, Vector2.One * CellSize);
            DrawRect(hitRect, new Color(0.1f, 1f, 1f, 0.8f), false, 1.5f);
            DrawLine(center - new Vector2(7, 0), center + new Vector2(7, 0), new Color("ff315f"), 2);
            DrawLine(center - new Vector2(0, 7), center + new Vector2(0, 7), new Color("ff315f"), 2);
        }
    }

    private void DrawPiece(Vector2 center, PieceColor color)
    {
        if (_pieceVisuals.TryGetValue(color, out TextureVisual visual))
            DrawVisual(visual, center, 61);
    }

    private static TextureVisual LoadVisual(string fileName, Rect2 sourceRegion) =>
        new(GD.Load<Texture2D>($"{GameplayAssetRoot}/{fileName}"), sourceRegion);

    private void DrawVisual(TextureVisual visual, Vector2 center, float longestSide)
    {
        float scale = longestSide / Mathf.Max(visual.SourceRegion.Size.X, visual.SourceRegion.Size.Y);
        Vector2 size = visual.SourceRegion.Size * scale;
        DrawTextureRectRegion(visual.Texture, new Rect2(center - size / 2, size), visual.SourceRegion);
    }

    private readonly record struct TextureVisual(Texture2D Texture, Rect2 SourceRegion);
}
