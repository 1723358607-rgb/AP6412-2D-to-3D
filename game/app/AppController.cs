using Godot;
using AP6412Match3.Gameplay;
using AP6412Match3.App.Contracts;
using AP6412Match3.Tests.Gameplay;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.App;

/// <summary>Application composition and lifetime. Contains neither match rules nor final UI layouts.</summary>
public partial class AppController : Node
{
    private const string UiScenePath = "res://ui/GameUiRoot.tscn";
    private const string ProgressScenePath = "res://progress/ProgressStore.tscn";

    private GameSession _session = null!;
    private GameUiBridge? _ui;
    private ProgressStoreBridge? _progress;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("GameSession");
        GD.Print($"[BOOTSTRAP_OK] Godot {Engine.GetVersionInfo()["string"]}; C#; mode=2d; phase=idle");

        if (OS.GetCmdlineUserArgs().Contains("--smoke-test"))
        {
            int exitCode = StepZeroSmokeCheck.Run(_session);
            GetTree().Quit(exitCode);
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--board-smoke-test"))
        {
            int exitCode = StepOneSmokeCheck.Run(this, _session);
            GetTree().Quit(exitCode);
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--board-prototype") || !ResourceLoader.Exists(UiScenePath))
        {
            ShowBoardPrototype();
            return;
        }

        MountOptionalUi();
    }

    private void ShowBoardPrototype()
    {
        GetNode<CanvasLayer>("Bootstrap").Hide();
        GetNode<Node2D>("BoardView2D").Hide();
        var prototype = GD.Load<PackedScene>("res://presentation/2d/BoardPrototype.tscn").Instantiate();
        AddChild(prototype);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        // Escape only closes the bootstrap. The final UI owns in-game leave confirmation.
        if (_ui is null && @event is InputEventKey { Pressed: true, Keycode: Key.Escape })
            GetTree().Quit();
    }

    private void MountOptionalUi()
    {
        if (!ResourceLoader.Exists(UiScenePath))
        {
            GD.Print("[UI_PENDING] Expected in step 0: member B has not supplied GameUiRoot.tscn.");
            return;
        }

        Node uiNode = GD.Load<PackedScene>(UiScenePath).Instantiate();
        if (uiNode is not GameUiBridge ui)
        {
            uiNode.Free();
            GD.PushError("UI root must inherit AP6412Match3.App.Contracts.GameUiBridge. Bootstrap retained.");
            return;
        }

        if (ResourceLoader.Exists(ProgressScenePath))
        {
            Node progressNode = GD.Load<PackedScene>(ProgressScenePath).Instantiate();
            if (progressNode is not ProgressStoreBridge progress)
            {
                progressNode.Free();
                ui.Free();
                GD.PushError("Progress root must inherit AP6412Match3.App.Contracts.ProgressStoreBridge.");
                return;
            }
            _progress = progress;
            AddChild(progress);
            progress.LoadProgress("2d");
        }

        _ui = ui;
        GetNode("UiMount").AddChild(ui);
        ui.StartRequested += OnStartRequested;
        ui.RetryRequested += OnRetryRequested;
        ui.NextRequested += OnNextRequested;
        ui.LeaveRequested += OnLeaveRequested;
        ui.LevelSelectRequested += ShowLevelSelect;
        ui.StartScreenRequested += ui.ShowStart;
        _session.StateChanged += ui.UpdateHud;
        _session.LevelFinished += OnLevelFinished;

        GetNode<CanvasLayer>("Bootstrap").Hide();
        ui.ShowStart();
        GD.Print("[UI_CONNECTED] UI mounted through the shared C# bridge.");
    }

    private void OnStartRequested(int levelId)
    {
        if (levelId is < 1 or > 3)
            return;
        if (_progress is not null && !_progress.IsUnlocked(levelId))
            return;
        // Until ProgressStore exists, only the first level is a permissible request.
        if (_progress is null && levelId != 1)
            return;

        _session.StartLevel(levelId);
        if (_session.IsGameplayImplemented)
            _ui?.ShowGame(_session.GetSnapshot());
    }

    private void OnNextRequested()
    {
        GDictionary snapshot = _session.GetSnapshot();
        int level = snapshot["level_id"].AsInt32();
        if (snapshot["phase"].AsString() == "won" && level < 3)
            OnStartRequested(level + 1);
    }

    private void OnRetryRequested()
    {
        _session.RetryLevel();
        if (_session.IsGameplayImplemented)
            _ui?.ShowGame(_session.GetSnapshot());
    }

    private void OnLeaveRequested()
    {
        _session.LeaveLevel();
        ShowLevelSelect();
    }

    private void ShowLevelSelect() => _ui?.ShowLevelSelect(
        _progress?.GetProgress() ?? new GDictionary
        {
            ["schema_version"] = 1,
            ["mode"] = "2d",
            ["best_stars"] = new GDictionary { ["1"] = 0, ["2"] = 0, ["3"] = 0 },
        });

    private void OnLevelFinished(GDictionary result)
    {
        // Record first, then show the result; the UI must not recompute earned stars.
        _progress?.RecordResult(result);
        int level = result["level_id"].AsInt32();
        int best = _progress?.GetBestStars(level) ?? 0;
        _ui?.ShowResult(result, best);
    }

    public override void _ExitTree()
    {
        if (_ui is null)
            return;
        _session.StateChanged -= _ui.UpdateHud;
        _session.LevelFinished -= OnLevelFinished;
        _ui.StartRequested -= OnStartRequested;
        _ui.RetryRequested -= OnRetryRequested;
        _ui.NextRequested -= OnNextRequested;
        _ui.LeaveRequested -= OnLeaveRequested;
        _ui.LevelSelectRequested -= ShowLevelSelect;
        _ui.StartScreenRequested -= _ui.ShowStart;
    }
}
