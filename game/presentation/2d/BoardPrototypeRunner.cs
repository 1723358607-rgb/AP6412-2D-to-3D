using Godot;
using AP6412Match3.Core;

namespace AP6412Match3.Presentation;

/// <summary>A-owned development harness, independent of B's final UI. F6 runs this scene alone.</summary>
public partial class BoardPrototypeRunner : Node
{
    public const string LevelPath = "res://data/levels/level_01.json";
    public BoardView2D BoardView { get; private set; } = null!;
    private Label _feedback = null!;

    public override void _Ready()
    {
        BoardView = GetNode<BoardView2D>("BoardView2D");
        BuildDebugOverlay();
        BoardView.FeedbackChanged += OnFeedback;
        ResetPrototype();
    }

    public void ResetPrototype()
    {
        var level = LevelDefinition.FromJson(Godot.FileAccess.GetFileAsString(LevelPath));
        BoardView.Bind(new BoardGenerator().CreateInitialBoard(level));
        _feedback.Text = "点击棋子选中；点击相邻棋子记录交换请求。";
        GD.Print($"[BOARD_PROTOTYPE_READY] 7x7; pieces=44; boxes=5; seed={level.Seed}; no match resolution");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;
        if (key.Keycode == Key.R)
            ResetPrototype();
        else if (key.Keycode == Key.Escape)
            GetTree().Quit();
        else
            return;
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (BoardView is not null)
            BoardView.FeedbackChanged -= OnFeedback;
    }

    private void OnFeedback(string message) => _feedback.Text = message;

    private void BuildDebugOverlay()
    {
        var layer = new CanvasLayer { Layer = 2 };
        AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(root);

        AddLabel(root, new Vector2(52, 28), new Vector2(1176, 28), "AP6412  /  GAMEPLAY STEP 1  /  DEVELOPMENT PROTOTYPE", 18, "8ca8cf");
        AddLabel(root, new Vector2(52, 62), new Vector2(1176, 48), "彩块工坊 · 棋盘原型", 34, "f0f5ff");
        AddLabel(root, new Vector2(52, 196), new Vector2(290, 200), "第一关 · 打开工坊\n\n7 × 7 棋盘\n44 枚普通棋子\n5 个固定木箱\n\n红菱形 / 蓝圆形\n绿三角 / 黄方块", 20, "c8d6ea");
        AddLabel(root, new Vector2(934, 196), new Vector2(294, 230), "本轮验收\n\n① 点击：高亮选中\n② 再点同格：取消\n③ 点相邻格：记录请求\n④ 远格 / 木箱：拒绝\n\nR：恢复固定棋盘\nEsc：退出原型", 19, "c8d6ea");
        _feedback = AddLabel(root, new Vector2(52, 628), new Vector2(1176, 36), "", 21, "fff0a5");
        AddLabel(root, new Vector2(52, 672), new Vector2(1176, 28), "开发原型：尚未执行交换、消除、扣步、星级或结算。正式菜单和 HUD 由 UI 负责人制作。", 17, "8ca8cf");
    }

    private static Label AddLabel(Control parent, Vector2 position, Vector2 size, string text, int fontSize, string color)
    {
        var label = new Label
        {
            Position = position, Size = size, Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(color));
        parent.AddChild(label);
        return label;
    }
}
