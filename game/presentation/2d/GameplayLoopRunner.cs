using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Presentation;

/// <summary>A's isolated step-2 harness. Uses the shared session and presenter, never fake HUD counters.</summary>
public partial class GameplayLoopRunner : Node
{
    public GameSession Session { get; private set; } = null!;
    public BoardView2D BoardView { get; private set; } = null!;
    private Label _state = null!;
    private Label _feedback = null!;

    public override void _Ready()
    {
        Session = GetNode<GameSession>("GameSession");
        BoardView = GetNode<BoardView2D>("BoardView2D");
        BuildOverlay();
        Session.AttachPresenter(BoardView);
        Session.StateChanged += OnState;
        BoardView.SwapRequested += OnSwap;
        BoardView.FeedbackChanged += OnFeedback;
        Session.StartLevel(1);
        GD.Print("[GAMEPLAY_LOOP_READY] Real level-1 swaps, moves and cascades; box damage/victory pending step 3.");
    }

    private async void OnSwap(Vector2I from, Vector2I to) =>
        await Session.RequestSwapAsync(new GridPosition(from.X, from.Y), new GridPosition(to.X, to.Y));

    private void OnFeedback(string text)
    {
        if (!Session.IsBusy) _feedback.Text = text;
    }

    private void OnState(GDictionary snapshot)
    {
        string phase = snapshot["phase"].AsString();
        string status = phase switch { "resolving" => "结算中 · 输入已锁定", "lost" => "步数耗尽 · 按 R 重试", _ => "可以操作" };
        _state.Text = $"第一关 · 打开工坊\n\n剩余步数  {snapshot["moves_remaining"]} / {snapshot["initial_moves"]}\n木箱目标  {snapshot["target_remaining"]} / {snapshot["target_total"]}\n\n{status}";
        _feedback.Text = phase == "resolving" ? "正在交换 / 消除 / 下落 / 补充；连锁不会额外扣步。" : Session.LastRequestNotice;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.R) Session.RetryLevel();
        else if (key.Keycode == Key.Escape) GetTree().Quit();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        Session.StateChanged -= OnState;
        BoardView.SwapRequested -= OnSwap;
        BoardView.FeedbackChanged -= OnFeedback;
    }

    private void BuildOverlay()
    {
        var layer = new CanvasLayer { Layer = 2 };
        AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(root);
        AddLabel(root, new Vector2(52, 28), new Vector2(1176, 28), "AP6412  /  GAMEPLAY STEP 2  /  DEVELOPMENT HARNESS", 18, "8ca8cf");
        AddLabel(root, new Vector2(52, 62), new Vector2(1176, 48), "彩块工坊 · 消除闭环", 34, "f0f5ff");
        _state = AddLabel(root, new Vector2(52, 196), new Vector2(290, 290), "", 20, "c8d6ea");
        AddLabel(root, new Vector2(934, 196), new Vector2(294, 290), "先试这一手\n\n第1行第2列\n↕\n第2行第2列\n\n相邻点击进行交换\nR：从15步重新开始\nEsc：退出", 19, "c8d6ea");
        _feedback = AddLabel(root, new Vector2(52, 628), new Vector2(1176, 36), "", 21, "fff0a5");
        AddLabel(root, new Vector2(52, 672), new Vector2(1176, 28), "玩法开发入口：木箱暂不受伤；通关、星级、第二/三关在第3步接入。正式菜单/HUD由队友制作。", 17, "8ca8cf");
    }

    private static Label AddLabel(Control parent, Vector2 position, Vector2 size, string text, int fontSize, string color)
    {
        var label = new Label { Position = position, Size = size, Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(color));
        parent.AddChild(label);
        return label;
    }
}
