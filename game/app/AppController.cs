using Godot;

namespace AP6412Match3.App;

/// <summary>
/// Owns application navigation and connects the gameplay session to the UI.
/// Step 0 intentionally contains no page flow or match-3 rules.
/// </summary>
public partial class AppController : Node
{
    public override void _Ready()
    {
        GD.Print("AP6412 Step 0 shared project ready.");
    }
}
