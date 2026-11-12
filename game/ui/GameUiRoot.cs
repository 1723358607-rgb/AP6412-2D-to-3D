using Godot;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Ui;

/// <summary>
/// Crystal Garden navigation shell. It consumes the shared bridge and owns only UI state.
/// Match rules remain in GameSession and the gameplay core.
/// </summary>
public partial class GameUiRoot : GameUiBridge
{
    private const string HomeScenePath = "res://ui/HomeScreen.tscn";
    private const string LevelsScenePath = "res://ui/LevelSelectionScreen.tscn";
    private const string PlaceholderScenePath = "res://ui/LevelPlaceholderScreen.tscn";
    private const string GameplayScenePath = "res://ui/GameplayScreen.tscn";

    private Control _screenHost = null!;
    private readonly ILevelProgressProvider _demoProgress = new DemoLevelProgressProvider();
    private string _displayMode = "3d";

    public override void _Ready()
    {
        _screenHost = GetNode<Control>("ScreenHost");
        CallDeferred(nameof(RunRequestedAutomation));
    }

    public override void ShowStart()
    {
        var screen = Instantiate<HomeScreen>(HomeScenePath);
        screen.ConfigureMode(_displayMode);
        screen.ModeSelected += mode =>
        {
            _displayMode = mode;
            EmitSignal(SignalName.ModeRequested, mode);
        };
        screen.PlayRequested += RequestLevelSelection;
        screen.LevelsRequested += RequestLevelSelection;
        SetScreen(screen);
    }

    public override void SetDisplayMode(string mode)
    {
        if (mode is not ("2d" or "3d"))
            throw new ArgumentException("Game mode must be '2d' or '3d'.", nameof(mode));
        _displayMode = mode;
        if (CurrentScreen is HomeScreen home) home.ConfigureMode(mode);
    }

    public override void ShowLevelSelect(GDictionary progress)
    {
        // Explicit visual-demo provider until ProgressStore is implemented.
        var screen = Instantiate<LevelSelectionScreen>(LevelsScenePath);
        screen.SetProgressProvider(_demoProgress);
        screen.BackRequested += () => EmitSignal(SignalName.StartScreenRequested);
        screen.LevelRequested += RequestLevel;
        SetScreen(screen);
    }

    public override void ShowGame(GDictionary snapshot)
    {
        var screen = Instantiate<GameplayScreen>(GameplayScenePath);
        screen.LeaveConfirmed += () => EmitSignal(SignalName.LeaveRequested);
        SetScreen(screen);
        screen.ApplySnapshot(snapshot);
    }

    public override void UpdateHud(GDictionary snapshot)
    {
        if (CurrentScreen is GameplayScreen gameplay)
            gameplay.ApplySnapshot(snapshot);
    }

    public override void ShowResult(GDictionary result, int bestStars)
    {
        ShowLevelSelect(new GDictionary());
    }

    private void RequestLevelSelection() => EmitSignal(SignalName.LevelSelectRequested);

    private void RequestLevel(int levelId)
    {
        if (!_demoProgress.GetLevel(levelId).IsUnlocked)
            return;

        // Keep the visual demo usable before ProgressStore unlock data is connected.
        DisplayLevelPlaceholder(levelId);
        EmitSignal(SignalName.StartRequested, levelId);
    }

    private void DisplayLevelPlaceholder(int levelId)
    {
        var screen = Instantiate<LevelPlaceholderScreen>(PlaceholderScenePath);
        screen.LevelId = levelId;
        screen.BackRequested += RequestLevelSelection;
        SetScreen(screen);
    }

    private void SetScreen(Control screen)
    {
        foreach (Node child in _screenHost.GetChildren())
        {
            _screenHost.RemoveChild(child);
            child.QueueFree();
        }
        _screenHost.AddChild(screen);
    }

    private static T Instantiate<T>(string path) where T : Node =>
        GD.Load<PackedScene>(path).Instantiate<T>();

    private Control? CurrentScreen => _screenHost.GetChildCount() == 0
        ? null
        : _screenHost.GetChild<Control>(0);

    private async void RunRequestedAutomation()
    {
        string[] args = OS.GetCmdlineUserArgs();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (args.Contains("--ui-smoke-test"))
        {
            await RunUiSmokeTest();
            return;
        }

        bool captureLevels = args.Contains("--ui-capture-levels");
        bool captureGameplay = args.Contains("--ui-capture-gameplay");
        bool captureHome = args.Contains("--ui-capture-home");
        if (!captureHome && !captureLevels && !captureGameplay)
            return;

        GetWindow().ContentScaleSize = new Vector2I(1920, 1080);
        GetWindow().Size = new Vector2I(1920, 1080);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (captureLevels)
            ShowLevelSelect(new GDictionary());
        else if (captureGameplay)
            EmitSignal(SignalName.StartRequested, 1);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        string capturePath = args
            .FirstOrDefault(arg => arg.StartsWith("--capture-path="))?
            .Split('=', 2)[1] ?? "/private/tmp/crystal-garden-ui.png";
        Error error = GetViewport().GetTexture().GetImage().SavePng(capturePath);
        GD.Print(error == Error.Ok
            ? $"[UI_CAPTURE_OK] {capturePath}"
            : $"[UI_CAPTURE_FAIL] {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private async Task RunUiSmokeTest()
    {
        try
        {
            Require(CurrentScreen is HomeScreen, "home screen mounted");

            var home = (HomeScreen)CurrentScreen!;
            Press(home, "PlayButton");
            Require(CurrentScreen is LevelSelectionScreen, "Play navigates to levels");

            var levels = (LevelSelectionScreen)CurrentScreen!;
            Require(!levels.IsLevelLocked(1), "level 1 unlocked");
            Require(!levels.IsLevelLocked(2), "level 2 unlocked");
            Require(levels.IsLevelLocked(3), "level 3 locked");
            Press(levels, "BackButton");
            Require(CurrentScreen is HomeScreen, "Back navigates home");

            home = (HomeScreen)CurrentScreen!;
            Press(home, "SettingsButton");
            Require(home.IsSettingsOpen, "settings opens");
            Press(home, "CloseSettingsButton");
            Require(!home.IsSettingsOpen, "settings closes");

            Press(home, "LevelsButton");
            levels = (LevelSelectionScreen)CurrentScreen!;
            Press(levels, "LevelCards/Level1Card/ClickTarget");
            Require(CurrentScreen is GameplayScreen, "level 1 opens real gameplay UI");
            Press(CurrentScreen!, "SettingsButton");
            Require(CurrentScreen is GameplayScreen { IsSettingsOpen: true }, "gameplay settings opens");
            Press(CurrentScreen!, "CloseSettingsButton");
            Require(CurrentScreen is GameplayScreen { IsSettingsOpen: false }, "gameplay settings closes");
            Press(CurrentScreen!, "PauseButton");
            Require(CurrentScreen is GameplayScreen, "pending pause action does not corrupt gameplay screen");
            Press(CurrentScreen!, "BackButton");
            Require(CurrentScreen is GameplayScreen gameplay && gameplay.IsLeaveDialogOpen,
                "gameplay back opens leave confirmation");
            Press(CurrentScreen!, "ConfirmLeaveButton");

            levels = (LevelSelectionScreen)CurrentScreen!;
            Press(levels, "LevelCards/Level2Card/ClickTarget");
            Require(CurrentScreen is LevelPlaceholderScreen { LevelId: 2 }, "level 2 opens placeholder");
            Press(CurrentScreen!, "BackButton");

            levels = (LevelSelectionScreen)CurrentScreen!;
            var lockedButton = Find<Button>(levels, "LevelCards/Level3Card/ClickTarget");
            Require(lockedButton.Disabled, "level 3 click target disabled");

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("[CRYSTAL_UI_SMOKE_PASS] navigation, settings and lock state verified");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[CRYSTAL_UI_SMOKE_FAIL] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Press(Control root, string path)
    {
        BaseButton button = Find<BaseButton>(root, path);
        if (button.Disabled)
            throw new InvalidOperationException($"Button is disabled: {path}");
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private static T Find<T>(Control root, string path) where T : Node
    {
        Node? node = path.Contains('/')
            ? root.GetNodeOrNull(path)
            : root.FindChild(path, true, false);
        return node as T ?? throw new InvalidOperationException($"Missing {typeof(T).Name}: {path}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
