using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;

namespace AP6412Match3.Presentation;

/// <summary>Board-only drawing/input adapter; no menus, HUD, match resolution or session state.</summary>
public partial class BoardView2D : Node2D, IBoardPresenter
{
    private BoardStep? _animation;
    private float _animationProgress;
    private int _animationVersion;
    private bool _interactive;
    private bool _prototype;
    public const float CellSize = 58;
    public const float Gap = 6;
    public const float Pitch = CellSize + Gap;
    public BoardInteraction? Interaction { get; private set; }
    public string LastMessage { get; private set; } = "点击普通棋子，然后点击相邻棋子。";

    [Signal] public delegate void FeedbackChangedEventHandler(string message);
    [Signal] public delegate void SwapRequestedEventHandler(Vector2I from, Vector2I to);

    public override void _Ready()
    {
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

    public Vector2 BoardSize => Interaction is null ? Vector2.Zero :
        new Vector2(Interaction.Board.Width * Pitch - Gap, Interaction.Board.Height * Pitch - Gap);

    private void UpdateLayout()
    {
        Vector2 viewport = GetParent() is Control host ? host.Size : GetViewportRect().Size;
        Position = (viewport - BoardSize) / 2 + (GetParent() is Control ? Vector2.Zero : new Vector2(0, 16));
        QueueRedraw();
    }

    public bool TryGetCellAtLocalPosition(Vector2 point, out GridPosition position)
    {
        position = default;
        if (Interaction is null || point.X < 0 || point.Y < 0 || point.X >= BoardSize.X || point.Y >= BoardSize.Y)
            return false;
        int column = (int)(point.X / Pitch);
        int row = (int)(point.Y / Pitch);
        if (point.X - column * Pitch >= CellSize || point.Y - row * Pitch >= CellSize)
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
        DrawRect(new Rect2(new Vector2(-14, -14), BoardSize + new Vector2(28, 28)), new Color("111c30"));
        for (int row = 0; row < board.Height; row++)
        for (int column = 0; column < board.Width; column++)
        {
            var position = new GridPosition(column, row);
            Vector2 origin = new(column * Pitch, row * Pitch);
            Vector2 center = origin + Vector2.One * (CellSize / 2);
            var rect = new Rect2(origin, Vector2.One * CellSize);
            DrawRect(rect, new Color("20314b"));
            BoardCell cell = board.GetCell(position);
            if (cell.Kind == CellKind.Box)
            {
                bool hit = _animation?.Kind == BoardStepKind.Clear && _animation.DamagedBoxes?.Contains(position) == true;
                float scale = hit && cell.BoxLayers == 1 ? 1 - progress : 1;
                DrawSetTransform(center, 0, Vector2.One * scale);
                var boxRect = new Rect2(-Vector2.One * CellSize / 2, Vector2.One * CellSize);
                Color wood = new(cell.BoxLayers == 2 ? "855943" : "a56e3e");
                DrawRect(boxRect.Grow(-4), hit ? wood.Lerp(new Color("fff0a5"), Mathf.Sin(progress * Mathf.Pi)) : wood);
                DrawRect(boxRect.Grow(-7), new Color("edc18b"), false, cell.BoxLayers == 2 ? 4 : 2);
                DrawLine(new Vector2(-19, -19), new Vector2(19, 19), new Color("603c27"), 4, true);
                DrawLine(new Vector2(19, -19), new Vector2(-19, 19), new Color("603c27"), 4, true);
                DrawRect(new Rect2(new Vector2(-10, -13), new Vector2(20, 26)), new Color("3e2d27"));
                DrawString(ThemeDB.FallbackFont, new Vector2(-6, 7), cell.BoxLayers.ToString(), fontSize: 19, modulate: new Color("fff0ce"));
                DrawSetTransform(Vector2.Zero);
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
            Vector2 destination = new Vector2(motion.To.Column, motion.To.Row) * Pitch + Vector2.One * CellSize / 2;
            Vector2 source = motion.Spawned ? destination + new Vector2(0, -14) :
                new Vector2(motion.From.Column, motion.From.Row) * Pitch + Vector2.One * CellSize / 2;
            DrawSetTransform(source.Lerp(destination, progress), 0, Vector2.One * (motion.Spawned ? Mathf.Lerp(0.1f, 1, progress) : 1));
            DrawPiece(Vector2.Zero, motion.Cell.Color);
            DrawSetTransform(Vector2.Zero);
        }

        Font font = ThemeDB.FallbackFont;
        for (int column = 0; column < board.Width; column++)
            DrawString(font, new Vector2(column * Pitch, -22), (column + 1).ToString(), HorizontalAlignment.Center, CellSize, 16, new Color("93a6c2"));
        for (int row = 0; row < board.Height; row++)
            DrawString(font, new Vector2(-42, row * Pitch + CellSize / 2 + 6), (row + 1).ToString(), HorizontalAlignment.Center, 20, 16, new Color("93a6c2"));
    }

    private void DrawPiece(Vector2 center, PieceColor color)
    {
        const float radius = 20;
        switch (color)
        {
            case PieceColor.Red:
                DrawColoredPolygon([center + new Vector2(0, -radius), center + new Vector2(radius, 0), center + new Vector2(0, radius), center + new Vector2(-radius, 0)], new Color("f47788"));
                break;
            case PieceColor.Blue:
                DrawCircle(center, radius, new Color("68b9fa"));
                DrawCircle(center + new Vector2(-6, -6), 5, new Color("b5e3ff"));
                break;
            case PieceColor.Green:
                DrawColoredPolygon([center + new Vector2(0, -radius - 2), center + new Vector2(radius + 1, radius - 1), center + new Vector2(-radius - 1, radius - 1)], new Color("67d8a7"));
                break;
            case PieceColor.Yellow:
                DrawRect(new Rect2(center - Vector2.One * 17, Vector2.One * 34), new Color("f6cd70"));
                DrawRect(new Rect2(center - Vector2.One * 8, Vector2.One * 16), new Color("ffe8a5"));
                break;
        }
    }
}
