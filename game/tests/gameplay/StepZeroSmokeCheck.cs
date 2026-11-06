using Godot;
using AP6412Match3.Gameplay;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Verifies the actual Godot/.NET runtime and cross-module boundary, not unfinished game rules.</summary>
public static class StepZeroSmokeCheck
{
    public static int Run(GameSession session)
    {
        try
        {
            GDictionary snapshot = session.GetSnapshot();
            string[] keys = ["session_id", "level_id", "phase", "initial_moves", "moves_remaining", "target_total", "target_remaining", "mode"];
            foreach (string key in keys)
                Require(snapshot.ContainsKey(key), $"Snapshot key missing: {key}");
            Require(snapshot["phase"].AsString() == "idle", "Bootstrap must remain idle.");
            Require(!session.IsGameplayImplemented, "Step 0 must not claim gameplay exists.");
            snapshot["moves_remaining"] = 999;
            Require(session.GetSnapshot()["moves_remaining"].AsInt32() == 0,
                "A UI snapshot must not mutate gameplay state.");

            int results = 0;
            int stateEvents = 0;
            void CountResult(GDictionary _) => results++;
            void CountState(GDictionary _) => stateEvents++;
            session.LevelFinished += CountResult;
            session.StateChanged += CountState;
            session.StartLevel(1);
            session.RetryLevel();
            session.LeaveLevel();
            session.LevelFinished -= CountResult;
            session.StateChanged -= CountState;
            Require(results == 0, "Bootstrap must not emit fake wins or losses.");
            Require(stateEvents == 1, "LeaveLevel must publish idle state once.");
            Require(session.GetSnapshot()["phase"].AsString() == "idle", "Commands must not fake an active game.");

            bool rejected = false;
            try { session.StartLevel(0); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Invalid level ID must be rejected.");

            var ui = new StepZeroUiProbe();
            try
            {
                int requestedLevel = 0;
                ui.StartRequested += levelId => requestedLevel = levelId;
                ui.EmitSignal(GameUiBridge.SignalName.StartRequested, 2);
                Require(requestedLevel == 2, "Inherited C# Godot request signal is not connected.");
                Require(ui.HasSignal(GameUiBridge.SignalName.LevelSelectRequested), "Level-select navigation signal missing.");
                Require(ui.HasSignal(GameUiBridge.SignalName.StartScreenRequested), "Start-screen navigation signal missing.");
            }
            finally
            {
                // This probe never enters SceneTree, so free its native Node explicitly.
                ui.Free();
            }

            GD.Print("[STEP0_SMOKE_PASS] C# assembly, snapshot isolation, idle lifecycle and inherited UI signals.");
            return 0;
        }
        catch (Exception error)
        {
            GD.PushError($"[STEP0_SMOKE_FAIL] {error}");
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
