using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Presentation;

/// <summary>A's isolated gameplay harness. Real levels/results; no mock progress or persistent saves.</summary>
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
        Session.LevelFinished += OnFinished;
        BoardView.SwapRequested += OnSwap;
        BoardView.FeedbackChanged += OnFeedback;
        Session.StartLevel(1);
        GD.Print("[GAMEPLAY_LOOP_READY] Real three-level gameplay, box damage and earned stars; no persistent saves in this development harness.");
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
        string status = phase switch { "resolving" => "结算中 · 输入已锁定", "won" => "目标完成 · 已通关", "lost" => "步数耗尽 · 按 R 重试", _ => "可以操作" };
        int level = snapshot["level_id"].AsInt32();
        string name = level switch { 1 => "打开工坊", 2 => "深入整理", 3 => "最后一批", _ => "" };
        _state.Text = $"第{level}关 · {name}\n\n剩余步数  {snapshot["moves_remaining"]} / {snapshot["initial_moves"]}\n木箱剩余  {snapshot["target_remaining"]} / {snapshot["target_total"]}\n\n{status}";
        _feedback.Text = phase == "resolving" ? "正在交换 / 消除 / 下落 / 补充；连锁不会额外扣步。" : Session.LastRequestNotice;
    }

    private void OnFinished(GDictionary result)
    {
        bool won = result["won"].AsBool();
        int level = result["level_id"].AsInt32();
        _state.Text += $"\n本次星级  {result["stars"]} / 3";
        _feedback.Text = won ? level == 3 ? "第三关通关！R可重试。本入口不保存历史成绩。" : "通关！N进入下一关，R重试。本入口不保存历史成绩。" : "本次失败 · 0星。R重试；1 / 2 / 3切换开发关卡。";
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.R) Session.RetryLevel();
        else if (key.Keycode is Key.Key1 or Key.Kp1) Session.StartLevel(1);
        else if (key.Keycode is Key.Key2 or Key.Kp2) Session.StartLevel(2);
        else if (key.Keycode is Key.Key3 or Key.Kp3) Session.StartLevel(3);
        else if (key.Keycode == Key.N)
        {
            var snapshot = Session.GetSnapshot();
            if (snapshot["phase"].AsString() == "won" && snapshot["level_id"].AsInt32() < 3)
                Session.StartLevel(snapshot["level_id"].AsInt32() + 1);
        }
        else if (key.Keycode == Key.Escape) GetTree().Quit();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        Session.StateChanged -= OnState;
        Session.LevelFinished -= OnFinished;
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
        AddLabel(root, new Vector2(52, 28), new Vector2(GetViewport().GetVisibleRect().Size.X - 104, 28), "AP6412  /  GAMEPLAY STEP 3  /  DEVELOPMENT HARNESS", 18, "8ca8cf");
        AddLabel(root, new Vector2(52, 62), new Vector2(1176, 48), "彩块工坊 · 木箱与三星", 34, "f0f5ff");
        _state = AddLabel(root, new Vector2(52, 196), new Vector2(290, 290), "", 20, "c8d6ea");
        AddLabel(root, new Vector2(GetViewport().GetVisibleRect().Size.X - 346, 196), new Vector2(294, 320), "消除木箱旁棋子\n\n箱上数字 = 剩余层数\n同轮最多伤害一层\n全部木箱消失即通关\n\n1 / 2 / 3：开发关卡\nR：重试同一关\nN：通关后下一关\nEsc：退出", 19, "c8d6ea");
        _feedback = AddLabel(root, new Vector2(52, GetViewport().GetVisibleRect().Size.Y - 92), new Vector2(GetViewport().GetVisibleRect().Size.X - 104, 36), "", 21, "fff0a5");
        AddLabel(root, new Vector2(52, GetViewport().GetVisibleRect().Size.Y - 48), new Vector2(GetViewport().GetVisibleRect().Size.X - 104, 28), "玩法开发入口：可直接测试三关，不写存档、不代表解锁。正式菜单、成绩保存与解锁由队友接入。", 17, "8ca8cf");
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
