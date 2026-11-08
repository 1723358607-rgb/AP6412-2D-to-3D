using Godot;
using AP6412Match3.Progress;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Ui;

/// <summary>Focused persistence checks. Tests require an isolated directory override.</summary>
public static class ProgressStoreSmokeCheck
{
    public static int Run(string operation)
    {
        try
        {
            Require(!string.IsNullOrWhiteSpace(OS.GetEnvironment("AP6412_PROGRESS_STORE_DIR")),
                "Progress tests require AP6412_PROGRESS_STORE_DIR and must not use real player data.");

            switch (operation)
            {
                case "full": RunFull(); break;
                case "write": RunRestartWrite(); break;
                case "read": RunRestartRead(); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
            return 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"[PROGRESS_STORE_TEST_FAIL] {exception.Message}");
            return 1;
        }
    }

    private static void RunFull()
    {
        string twoDimensionalPath = ProgressStore.StoragePath("2d");
        string threeDimensionalPath = ProgressStore.StoragePath("3d");
        Require(!Godot.FileAccess.FileExists(twoDimensionalPath) && !Godot.FileAccess.FileExists(threeDimensionalPath),
            "Full test requires an empty isolated directory.");

        ProgressStore first = Load("2d");
        Require(first.IsUnlocked(1) && !first.IsUnlocked(2) && !first.IsUnlocked(3), "Default unlocks are wrong.");

        first.RecordResult(Result(1, true, 3, "2d"));
        Require(first.GetBestStars(1) == 3 && first.IsUnlocked(2) && !first.IsUnlocked(3),
            "Level 1 did not persist three stars and unlock level 2.");
        first.RecordResult(Result(1, true, 1, "2d"));
        first.RecordResult(Result(1, false, 0, "2d"));
        Require(first.GetBestStars(1) == 3, "A lower score or failure replaced the historical best.");

        first.RecordResult(Result(2, true, 2, "2d"));
        Require(first.GetBestStars(2) == 2 && first.IsUnlocked(3), "Level 2 did not unlock level 3.");
        first.Free();

        ProgressStore reopened = Load("2d");
        Require(reopened.GetBestStars(1) == 3 && reopened.GetBestStars(2) == 2 && reopened.IsUnlocked(3),
            "Recreated store did not restore saved 2D progress.");
        reopened.Free();

        ProgressStore threeDimensional = Load("3d");
        Require(threeDimensional.GetBestStars(1) == 0 && threeDimensional.IsUnlocked(1) && !threeDimensional.IsUnlocked(2),
            "3D progress was contaminated by 2D progress.");
        threeDimensional.Free();

        using (Godot.FileAccess? corrupt = Godot.FileAccess.Open(threeDimensionalPath, Godot.FileAccess.ModeFlags.Write))
        {
            if (corrupt is null) throw new InvalidOperationException("Could not create corrupt-file fixture.");
            corrupt.StoreString("{not valid json");
        }
        ProgressStore recovered = Load("3d");
        Require(recovered.IsUnlocked(1) && !recovered.IsUnlocked(2) && recovered.GetBestStars(1) == 0,
            "Corrupt progress did not recover to safe defaults.");
        recovered.Free();

        GD.Print("[PROGRESS_STORE_PASS] defaults, unlocks, best stars, recreation, mode isolation and corrupt-file recovery.");
    }

    private static void RunRestartWrite()
    {
        ProgressStore store = Load("2d");
        store.RecordResult(Result(1, true, 3, "2d"));
        store.RecordResult(Result(2, true, 2, "2d"));
        store.Free();
        GD.Print("[PROGRESS_RESTART_WRITE_PASS] 2D progress saved for a separate-process read.");
    }

    private static void RunRestartRead()
    {
        ProgressStore store = Load("2d");
        Require(store.GetBestStars(1) == 3 && store.GetBestStars(2) == 2 && store.GetBestStars(3) == 0,
            "Separate process did not restore historical best stars.");
        Require(store.IsUnlocked(1) && store.IsUnlocked(2) && store.IsUnlocked(3),
            "Separate process did not restore unlock state.");
        store.Free();
        GD.Print("[PROGRESS_RESTART_READ_PASS] separate-process progress restored.");
    }

    private static ProgressStore Load(string mode)
    {
        var store = new ProgressStore();
        store.LoadProgress(mode);
        return store;
    }

    private static GDictionary Result(int levelId, bool won, int stars, string mode) => new()
    {
        ["level_id"] = levelId,
        ["won"] = won,
        ["stars"] = stars,
        ["mode"] = mode,
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
