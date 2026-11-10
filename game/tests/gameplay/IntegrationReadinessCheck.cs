using Godot;
using System.Text.Json;
using AP6412Match3.Progress;
using AP6412Match3.Ui;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>A's cross-module review checks. Requires an empty isolated store directory.</summary>
public static class IntegrationReadinessCheck
{
    public static int Run(Node host)
    {
        try
        {
            string directory = OS.GetEnvironment("AP6412_PROGRESS_STORE_DIR");
            Require(!string.IsNullOrWhiteSpace(directory), "Audit must never use real player saves.");
            string path = ProgressStore.StoragePath("3d");
            Require(!System.IO.File.Exists(path), "Audit directory must be fresh.");
            const string broken = "{not valid json; retain these exact bytes";
            System.IO.File.WriteAllText(path, broken);
            WithStore("3d", store =>
            {
                Require(store.GetBestStars(1) == 0 && !store.IsUnlocked(2), "Bad save did not recover defaults.");
            });
            Require(System.IO.Directory.GetFiles(directory, "*.corrupt-*")
                .Any(p => System.IO.File.ReadAllText(p) == broken), "Corrupt source was overwritten without preserving its bytes.");
            GD.Print("[INTEGRATION_CORRUPT_PASS] Invalid source retained separately; defaults usable.");

            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(new { SchemaVersion = 1, Mode = "3d",
                BestStars = new[] { 0, 0, 0 }, Completed = new[] { true, true, true }, Unlocked = new[] { true, true, true } }));
            WithStore("3d", store =>
            {
                Require(store.IsUnlocked(1) && !store.IsUnlocked(2) && !store.IsUnlocked(3),
                    "Saved flags overrode the best-stars unlock rule.");
            });
            GD.Print("[INTEGRATION_UNLOCK_PASS] Unlocks derive from earned best stars, not inconsistent saved flags.");

            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(new { SchemaVersion = 1, Mode = "3d",
                BestStars = new[] { 4, 0, 0 }, Completed = new[] { false, false, false }, Unlocked = new[] { true, false, false } }));
            WithStore("3d", store =>
            {
                Require(store.GetBestStars(1) == 0 && !store.IsUnlocked(2), "Out-of-range save granted a valid score.");
            });
            using (var json = JsonDocument.Parse(System.IO.File.ReadAllText(path)))
                Require(json.RootElement.GetProperty("BestStars")[0].GetInt32() == 0, "Safe state was not saved.");
            Require(System.IO.Directory.GetFiles(directory, "*.tmp-*").Length == 0, "Completed save left a temporary file.");

            CheckResult(host, level: 1, won: true, stars: 1, best: 3);
            CheckResult(host, level: 1, won: false, stars: 0, best: 3);
            CheckResult(host, level: 3, won: true, stars: 3, best: 3);
            GD.Print("[INTEGRATION_RESULT_PASS] Visible current/best values, real retry/levels/next signals and final completion title.");
            GD.Print("[INTEGRATION_AUDIT_PASS] Persistent progress edge cases and actual B result UI pass. Human acceptance remains separate.");
            return 0;
        }
        catch (Exception e) { GD.PushError("[INTEGRATION_AUDIT_FAIL] " + e); return 1; }
    }
    private static void WithStore(string mode, Action<ProgressStore> action)
    {
        var store = new ProgressStore();
        try { store.LoadProgress(mode); action(store); }
        finally { store.Free(); }
    }
    private static void CheckResult(Node host, int level, bool won, int stars, int best)
    {
        var page = GD.Load<PackedScene>("res://ui/ResultScreen.tscn").Instantiate<ResultScreen>();
        page.Configure(new GDictionary { ["level_id"] = level, ["won"] = won, ["stars"] = stars, ["moves_remaining"] = 1 }, best);
        int retries = 0, levels = 0, next = 0;
        page.RetryRequested += () => retries++; page.LevelsRequested += () => levels++; page.NextRequested += () => next++;
        host.AddChild(page);
        try
        {
            Require(page.GetNode<Label>("RunPanelValue").Text == $"{stars}/3" && page.GetNode<Label>("RunPanelValue").IsVisibleInTree(), "Visible current stars incorrect.");
            Require(page.GetNode<Label>("BestValue").Text == $"{best}/3", "Visible best stars incorrect.");
            page.GetNode<Button>("RetryButton").EmitSignal(BaseButton.SignalName.Pressed);
            page.GetNode<Button>("LevelsButton").EmitSignal(BaseButton.SignalName.Pressed);
            Require(retries == 1 && levels == 1, "Navigation signals failed.");
            var button = page.GetNodeOrNull<Button>("NextButton");
            Require((button is not null) == (won && level < 3), "Final/failure next button is wrong.");
            if (button is not null) { button.EmitSignal(BaseButton.SignalName.Pressed); Require(next == 1, "Next signal failed."); }
            if (won && level == 3) Require(page.GetNode<Label>("ResultTitle").Text == "GARDEN COMPLETE!", "Final completion title missing.");
        }
        finally { page.Free(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
