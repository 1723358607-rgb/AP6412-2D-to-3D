using Godot;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Contract-only test double. Never loaded by the normal application; no persistence.</summary>
public partial class StepTwoUiProbe : GameUiBridge
{
    public string Page { get; private set; } = "";
    public GDictionary Snapshot { get; private set; } = new();
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;
    public override void ShowStart() => Page = "start";
    public override void ShowLevelSelect(GDictionary progress) => Page = "select";
    public override void ShowGame(GDictionary snapshot) { Page = "game"; Snapshot = snapshot; }
    public override void UpdateHud(GDictionary snapshot) => Snapshot = snapshot;
    public override void ShowResult(GDictionary result, int bestStars) => Page = "result";
}
