using Godot;
using AP6412Match3.Core;

namespace AP6412Match3.Presentation;

/// <summary>Board-only drawing/input adapter; no menus, HUD, match resolution or session state.</summary>
public partial class BoardView2D : Node2D
{
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
        GetViewport().SizeChanged += UpdateLayout;
    }

    public void Bind(BoardModel board)
    {
        Interaction = new BoardInteraction(board);
        LastMessage = "点击普通棋子，然后点击相邻棋子。";
        UpdateLayout();
        Visible = true;
        SetProcessUnhandledInput(true);
        QueueRedraw();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= UpdateLayout;

    public Vector2 BoardSize => Interaction is null ? Vector2.Zero :
        new Vector2(Interaction.Board.Width * Pitch - Gap, Interaction.Board.Height * Pitch - Gap);

    private void UpdateLayout()
    {
        Vector2 viewport = GetViewportRect().Size;
        Position = (viewport - BoardSize) / 2 + new Vector2(0, 16);
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
        if (!IsVisibleInTree() || Interaction is null || @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
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
            SelectionOutcome.RequestAccepted => "邻接交换请求已记录。第 2 步再接入实际交换与消除。",
            _ => "请求被拒绝。",
        };
        if (outcome == SelectionOutcome.RequestAccepted && Interaction.LastAcceptedRequest is { } request)
        {
            EmitSignal(SignalName.SwapRequested,
                new Vector2I(request.From.Column, request.From.Row),
                new Vector2I(request.To.Column, request.To.Row));
            GD.Print($"[SWAP_REQUEST] ({request.From.Column},{request.From.Row}) -> ({request.To.Column},{request.To.Row}); prototype only");
        }
        EmitSignal(SignalName.FeedbackChanged, LastMessage);
        QueueRedraw();
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (Interaction is null)
            return;
        BoardModel board = Interaction.Board;
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
                DrawRect(rect.Grow(-4), new Color("a56e3e"));
                DrawRect(rect.Grow(-7), new Color("edc18b"), false, 2);
                DrawLine(origin + new Vector2(10, 10), origin + Vector2.One * (CellSize - 10), new Color("603c27"), 4, true);
                DrawLine(origin + new Vector2(CellSize - 10, 10), origin + new Vector2(10, CellSize - 10), new Color("603c27"), 4, true);
            }
            else if (cell.Kind == CellKind.Piece)
                DrawPiece(center, cell.Color);

            if (Interaction.LastAcceptedRequest is { } request && (request.From == position || request.To == position))
                DrawRect(rect.Grow(-1), new Color("63e4d7"), false, 3);
            if (Interaction.Selected == position)
                DrawRect(rect.Grow(-1), new Color("fff0a5"), false, 3);
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
