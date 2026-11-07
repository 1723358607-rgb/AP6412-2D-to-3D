using Godot;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Contract-only test double. Never loaded by the normal application; no persistence.</summary>
public partial class StepTwoUiProbe : GameUiBridge
{
    public int BestShown { get; private set; }
    public bool RecreatePages { get; set; }
    public GDictionary Result { get; private set; } = new();
    public string Page { get; private set; } = "";
    public GDictionary Snapshot { get; private set; } = new();
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;
    public override void ShowStart() { ReplacePage(false); Page = "start"; }
    public override void ShowLevelSelect(GDictionary progress) { ReplacePage(false); Page = "select"; }
    public override void ShowGame(GDictionary snapshot) { ReplacePage(true); Page = "game"; Snapshot = snapshot; }
    public override void UpdateHud(GDictionary snapshot) => Snapshot = snapshot;
    public override void ShowResult(GDictionary result, int bestStars) { ReplacePage(false); Page = "result"; Result = result; BestShown = bestStars; }

    private void ReplacePage(bool withBoard)
    {
        if (!RecreatePages) return;
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        if (withBoard) AddChild(new Control { Name = "BoardHost", Size = new Vector2(560, 500) });
    }
}
