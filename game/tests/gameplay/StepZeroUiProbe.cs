using Godot;
using AP6412Match3.Gameplay;
using AP6412Match3.App.Contracts;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Tests.Gameplay;

/// <summary>Headless contract probe only; not member B's UI and never mounted in the normal application.</summary>
public partial class StepZeroUiProbe : GameUiBridge
{
    public override void ShowStart() { }
    public override void ShowLevelSelect(GDictionary progress) { }
    public override void ShowGame(GDictionary snapshot) { }
    public override void UpdateHud(GDictionary snapshot) { }
    public override void ShowResult(GDictionary result, int bestStars) { }
}
