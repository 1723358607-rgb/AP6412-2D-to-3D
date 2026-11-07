using Godot;

namespace AP6412Match3.Ui;

public partial class LevelPlaceholderScreen : Control
{
    [Signal] public delegate void BackRequestedEventHandler();

    public int LevelId { get; set; } = 1;

    public override void _Ready()
    {
        var background = CrystalGardenUi.Texture(
            "GardenBackground", "backgrounds/garden_background_16x9.png", this,
            new Rect2(0, 0, 1920, 1080));
        background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;

        var dim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0.12f, 0.04f, 0.20f, 0.42f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CrystalGardenUi.FullRect(dim);
        AddChild(dim);

        Label title = CrystalGardenUi.Label("PlaceholderTitle",
            $"LEVEL {LevelId}\nGAMEPLAY COMING NEXT", this,
            new Rect2(460, 350, 1000, 260), 64);
        title.AddThemeConstantOverride("line_spacing", 18);

        TextureButton back = CrystalGardenUi.TextureButton("BackButton", "shared/button_back.png", this,
            new Rect2(36, 34, 150, 100));
        back.Pressed += () => EmitSignal(SignalName.BackRequested);
    }
}
