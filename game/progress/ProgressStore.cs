using System.Text.Json;
using Godot;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Progress;

/// <summary>Persistent per-mode best stars, completions and unlocks.</summary>
public partial class ProgressStore : ProgressStoreBridge
{
    private const int SchemaVersion = 1;
    private const string TestDirectoryVariable = "AP6412_PROGRESS_STORE_DIR";

    private readonly int[] _bestStars = new int[3];
    private readonly bool[] _completed = new bool[3];
    private readonly bool[] _unlocked = new bool[3];
    private string _mode = "2d";
    private bool _loaded;
    private bool _saveBlocked;

    public override void LoadProgress(string mode)
    {
        ValidateMode(mode);
        _mode = mode;
        _loaded = false;
        _saveBlocked = false;
        ResetToDefaults();

        string path = StoragePath(mode);
        if (!Godot.FileAccess.FileExists(path))
        {
            _loaded = true;
            Save();
            return;
        }

        try
        {
            SaveData? data = JsonSerializer.Deserialize<SaveData>(Godot.FileAccess.GetFileAsString(path));
            if (data is null || data.SchemaVersion != SchemaVersion || data.Mode != mode ||
                data.BestStars is null || data.Completed is null || data.Unlocked is null ||
                data.BestStars.Length != 3 || data.Completed.Length != 3 || data.Unlocked.Length != 3)
                throw new InvalidDataException("Unsupported or incomplete progress data.");
            if (data.BestStars.Any(stars => stars is < 0 or > 3))
                throw new InvalidDataException("Saved stars must be within 0–3.");

            for (int index = 0; index < 3; index++)
            {
                _bestStars[index] = data.BestStars[index];
                _completed[index] = _bestStars[index] > 0;
            }
            NormalizeUnlocks();
            _loaded = true;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Progress file '{path}' is invalid; defaults restored. {exception.Message}");
            ResetToDefaults();
            _loaded = true;
            // Preserve exact invalid bytes before replacing the active file.
            // If preservation fails, keep usable RAM defaults without overwriting the source.
            string physical = ProjectSettings.GlobalizePath(path);
            try
            {
                if (System.IO.File.Exists(physical))
                    System.IO.File.Copy(physical, physical + ".corrupt-" + Guid.NewGuid().ToString("N"), overwrite: false);
            }
            catch (IOException backupError) { _saveBlocked = true; GD.PushWarning("Invalid progress could not be backed up; disk writes disabled: " + backupError.Message); }
            catch (UnauthorizedAccessException backupError) { _saveBlocked = true; GD.PushWarning("Invalid progress could not be backed up; disk writes disabled: " + backupError.Message); }
            Save();
        }
    }

    public override void RecordResult(GDictionary result)
    {
        EnsureLoaded();
        int levelId = ReadRequiredInt(result, "level_id");
        int stars = ReadRequiredInt(result, "stars");
        string resultMode = ReadRequiredString(result, "mode");
        bool won = ReadRequiredBool(result, "won");

        if (levelId is < 1 or > 3 || stars is < 0 or > 3 || resultMode != _mode)
            throw new ArgumentException("Invalid gameplay result for this progress store.");

        if (!won || stars == 0)
            return;

        int index = levelId - 1;
        _bestStars[index] = Math.Max(_bestStars[index], stars);
        _completed[index] = true;
        if (levelId < 3)
            _unlocked[levelId] = true;
        NormalizeUnlocks();
        Save();
    }

    public override int GetBestStars(int levelId)
    {
        EnsureLoaded();
        return levelId is >= 1 and <= 3 ? _bestStars[levelId - 1] : 0;
    }

    public override bool IsUnlocked(int levelId)
    {
        EnsureLoaded();
        return levelId is >= 1 and <= 3 && _unlocked[levelId - 1];
    }

    public override GDictionary GetProgress()
    {
        EnsureLoaded();
        var best = new GDictionary();
        var completed = new Godot.Collections.Array<int>();
        var unlocked = new Godot.Collections.Array<int>();
        for (int index = 0; index < 3; index++)
        {
            best[(index + 1).ToString()] = _bestStars[index];
            if (_completed[index]) completed.Add(index + 1);
            if (_unlocked[index]) unlocked.Add(index + 1);
        }
        return new GDictionary
        {
            ["schema_version"] = SchemaVersion,
            ["mode"] = _mode,
            ["best_stars"] = best,
            ["completed_levels"] = completed,
            ["unlocked_levels"] = unlocked,
        };
    }

    internal static string StoragePath(string mode)
    {
        ValidateMode(mode);
        string directory = OS.GetEnvironment(TestDirectoryVariable);
        if (string.IsNullOrWhiteSpace(directory))
            return $"user://crystal_garden_progress_{mode}.json";

        DirAccess.MakeDirRecursiveAbsolute(directory);
        return System.IO.Path.Combine(directory, $"crystal_garden_progress_{mode}.json");
    }

    private void Save()
    {
        if (_saveBlocked) return;
        var data = new SaveData
        {
            SchemaVersion = SchemaVersion,
            Mode = _mode,
            BestStars = [.. _bestStars],
            Completed = [.. _completed],
            Unlocked = [.. _unlocked],
        };
        string path = ProjectSettings.GlobalizePath(StoragePath(_mode));
        string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.Move(temporary, path, overwrite: true);
        }
        finally { if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
    }

    private void ResetToDefaults()
    {
        Array.Clear(_bestStars);
        Array.Clear(_completed);
        Array.Clear(_unlocked);
        _unlocked[0] = true;
    }

    private void NormalizeUnlocks()
    {
        _unlocked[0] = true;
        _unlocked[1] = _bestStars[0] > 0;
        _unlocked[2] = _bestStars[1] > 0;
    }

    private void EnsureLoaded()
    {
        if (!_loaded)
            throw new InvalidOperationException("LoadProgress must be called before using the progress store.");
    }

    private static void ValidateMode(string mode)
    {
        if (mode is not ("2d" or "3d"))
            throw new ArgumentException("Progress mode must be '2d' or '3d'.", nameof(mode));
    }

    private static int ReadRequiredInt(GDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) ? value.AsInt32() : throw new ArgumentException($"Missing result field: {key}");

    private static string ReadRequiredString(GDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) ? value.AsString() : throw new ArgumentException($"Missing result field: {key}");

    private static bool ReadRequiredBool(GDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) ? value.AsBool() : throw new ArgumentException($"Missing result field: {key}");

    private sealed class SaveData
    {
        public int SchemaVersion { get; set; }
        public string Mode { get; set; } = "2d";
        public int[] BestStars { get; set; } = [0, 0, 0];
        public bool[] Completed { get; set; } = [false, false, false];
        public bool[] Unlocked { get; set; } = [true, false, false];
    }
}
