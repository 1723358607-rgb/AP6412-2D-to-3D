using Godot;
using AP6412Match3.Gameplay;
using AP6412Match3.App.Contracts;
using AP6412Match3.Tests.Gameplay;
using AP6412Match3.Core;
using AP6412Match3.Presentation;
using AP6412Match3.App.Integration;
using AP6412Match3.Tests.Ui;
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
    private BoardView2D _boardView = null!;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("GameSession");
        _boardView = GetNode<BoardView2D>("BoardView2D");
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

        if (OS.GetCmdlineUserArgs().Contains("--level-smoke-test"))
        {
            RunLevelChecks();
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--balance-scan"))
        {
            GetTree().Quit(BalanceStudy.Run());
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--step4-smoke-test"))
        {
            RunStepFourChecks();
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--rule-reference"))
        {
            RunRuleReference();
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--layout-study"))
        {
            GetTree().Quit(LayoutStudy.Run());
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--progress-store-test"))
        {
            GetTree().Quit(ProgressStoreSmokeCheck.Run("full"));
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--progress-store-restart-write"))
        {
            GetTree().Quit(ProgressStoreSmokeCheck.Run("write"));
            return;
        }
        if (OS.GetCmdlineUserArgs().Contains("--progress-store-restart-read"))
        {
            GetTree().Quit(ProgressStoreSmokeCheck.Run("read"));
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--gameplay-smoke-test"))
        {
            RunGameplayChecks();
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--board-prototype"))
        {
            ShowBoardPrototype();
            return;
        }

        if (OS.GetCmdlineUserArgs().Contains("--gameplay-loop") || !ResourceLoader.Exists(UiScenePath))
        {
            GetNode<CanvasLayer>("Bootstrap").Hide();
            _boardView.Hide();
            AddChild(GD.Load<PackedScene>("res://presentation/2d/GameplayLoop.tscn").Instantiate());
            return;
        }

        MountOptionalUi();
    }

    private async void RunLevelChecks() => GetTree().Quit(await StepThreeSmokeCheck.Run(this));
    private async void RunStepFourChecks() => GetTree().Quit(await StepFourSmokeCheck.Run(this));
    private async void RunRuleReference() => GetTree().Quit(await RuleReferenceCheck.Run());

    private async void RunGameplayChecks() => GetTree().Quit(await StepTwoSmokeCheck.Run(this));

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

    internal void MountOptionalUi()
    {
        if (!ResourceLoader.Exists(UiScenePath))
        {
            GD.Print("[UI_PENDING] Expected in step 0: member B has not supplied GameUiRoot.tscn.");
            return;
        }

        bool isolatedUiDemo = OS.GetCmdlineUserArgs().Contains("--ui-smoke-test");
        bool runtimeFallback = !ResourceLoader.Exists(ProgressScenePath) && !isolatedUiDemo;
        // Normal play keeps the already-integrated Crystal Garden result/progress UI.
        // The original GameUiRoot remains available for its isolated visual smoke test.
        Node uiNode = GD.Load<PackedScene>(isolatedUiDemo ? UiScenePath : "res://app/integration/IntegratedUi.tscn").Instantiate();
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
        else if (runtimeFallback)
        {
            _progress = new RuntimeProgressBridge();
            AddChild(_progress); _progress.LoadProgress("2d");
            GD.Print("[RUNTIME_PROGRESS] Real session-only stars/unlocks; persistent ProgressStore remains pending with B.");
        }

        ConnectUi(ui);
    }

    internal void ConnectUi(GameUiBridge ui, ProgressStoreBridge? progress = null)
    {
        if (progress is not null)
        {
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
        ui.StartScreenRequested += ShowStart;
        _session.StateChanged += OnStateChanged;
        _session.LevelFinished += OnLevelFinished;
        _session.AttachPresenter(_boardView);
        _boardView.SwapRequested += OnBoardSwap;
        _boardView.Hide();

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
        if (_session.GetSnapshot()["level_id"].AsInt32() == levelId)
            ShowGame();
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
            ShowGame();
    }

    private void OnLeaveRequested()
    {
        ShowLevelSelect();
    }

    private void ShowLevelSelect()
    {
        _session.LeaveLevel();
        _ui?.ShowLevelSelect(
        _progress?.GetProgress() ?? new GDictionary
        {
            ["schema_version"] = 1,
            ["mode"] = "2d",
            ["best_stars"] = new GDictionary { ["1"] = 0, ["2"] = 0, ["3"] = 0 },
        });
    }

    private void ShowStart()
    {
        _session.LeaveLevel();
        _ui?.ShowStart();
    }

    private void ShowGame()
    {
        if (_session.CurrentBoard is null) return;
        ParkBoard();
        _ui?.ShowGame(_session.GetSnapshot());
        // Optional named UI slot; public bridge signatures stay unchanged.
        if (_ui?.FindChild("BoardHost", true, false) is Control host)
        {
            host.MouseFilter = Control.MouseFilterEnum.Ignore;
            if (_boardView.GetParent() != host) _boardView.Reparent(host, false);
        }
        else
        {
            var fallback = GetNodeOrNull<CanvasLayer>("BoardFallback");
            if (fallback is null)
            {
                fallback = new CanvasLayer { Name = "BoardFallback", Layer = 2 };
                AddChild(fallback);
            }
            if (_boardView.GetParent() != fallback) _boardView.Reparent(fallback, false);
        }
        _boardView.SetBoard(_session.CurrentBoard);
    }

    private void OnStateChanged(GDictionary snapshot)
    {
        if (snapshot["phase"].AsString() is "idle" or "won" or "lost") ParkBoard();
        _ui?.UpdateHud(snapshot);
    }

    private void ParkBoard()
    {
        // B may remove and free its old page (including BoardHost) on any navigation.
        // A owns this presenter for the application lifetime, so reclaim it before that callback.
        if (_boardView.GetParent() != this) _boardView.Reparent(this, false);
        _boardView.Hide();
    }

    private async void OnBoardSwap(Vector2I from, Vector2I to) =>
        await _session.RequestSwapAsync(new GridPosition(from.X, from.Y), new GridPosition(to.X, to.Y));

    private void OnLevelFinished(GDictionary result)
    {
        // Record first, then show the result; the UI must not recompute earned stars.
        _progress?.RecordResult(result);
        int level = result["level_id"].AsInt32();
        int best = _progress?.GetBestStars(level) ?? 0;
        ParkBoard();
        _ui?.ShowResult(result, best);
    }

    public override void _ExitTree()
    {
        if (_ui is null)
            return;
        _session.StateChanged -= OnStateChanged;
        _session.LevelFinished -= OnLevelFinished;
        _ui.StartRequested -= OnStartRequested;
        _ui.RetryRequested -= OnRetryRequested;
        _ui.NextRequested -= OnNextRequested;
        _ui.LeaveRequested -= OnLeaveRequested;
        _ui.LevelSelectRequested -= ShowLevelSelect;
        _ui.StartScreenRequested -= ShowStart;
        _boardView.SwapRequested -= OnBoardSwap;
    }
}
