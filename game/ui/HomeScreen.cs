using Godot;

namespace AP6412Match3.Ui;

public partial class HomeScreen : Control
{
    [Signal] public delegate void PlayRequestedEventHandler();
    [Signal] public delegate void LevelsRequestedEventHandler();

    private Control _settingsOverlay = null!;

    public override void _Ready()
    {
        BuildScreen();
    }

    private void BuildScreen()
    {
        var background = CrystalGardenUi.Texture(
            "GardenBackground", "backgrounds/garden_background_16x9.png", this,
            new Rect2(0, 0, 1920, 1080));
        background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;

        CrystalGardenUi.Texture("CrystalGardenLogo", "home/crystal_garden_logo.png", this,
            new Rect2(550, 42, 820, 314));

        TextureButton play = CrystalGardenUi.TextureButton("PlayButton", "home/button_play.png", this,
            new Rect2(660, 525, 600, 186));
        TextureButton levels = CrystalGardenUi.TextureButton("LevelsButton", "home/button_levels.png", this,
            new Rect2(630, 750, 320, 113));
        TextureButton settings = CrystalGardenUi.TextureButton("SettingsButton", "home/button_settings.png", this,
            new Rect2(970, 750, 320, 113));

        play.Pressed += () => EmitSignal(SignalName.PlayRequested);
        levels.Pressed += () => EmitSignal(SignalName.LevelsRequested);
        settings.Pressed += OpenSettings;

        _settingsOverlay = BuildSettingsOverlay();
        _settingsOverlay.Hide();
    }

    private Control BuildSettingsOverlay()
    {
        var overlay = new Control { Name = "SettingsOverlay", MouseFilter = MouseFilterEnum.Stop };
        CrystalGardenUi.FullRect(overlay);
        AddChild(overlay);

        var dim = new ColorRect { Name = "Dim", Color = new Color(0.09f, 0.03f, 0.16f, 0.58f) };
        CrystalGardenUi.FullRect(dim);
        overlay.AddChild(dim);

        var panel = new PanelContainer
        {
            Name = "SettingsPanel",
            Position = new Vector2(660, 280),
            Size = new Vector2(600, 520),
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color("e9c8ff"),
            BorderColor = new Color("f8d874"),
            CornerRadiusTopLeft = 38,
            CornerRadiusTopRight = 38,
            CornerRadiusBottomLeft = 38,
            CornerRadiusBottomRight = 38,
            BorderWidthLeft = 8,
            BorderWidthTop = 8,
            BorderWidthRight = 8,
            BorderWidthBottom = 8,
            ShadowColor = new Color(0.25f, 0.05f, 0.38f, 0.45f),
            ShadowSize = 18,
        };
        panel.AddThemeStyleboxOverride("panel", style);
        overlay.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 72);
        margin.AddThemeConstantOverride("margin_right", 72);
        margin.AddThemeConstantOverride("margin_top", 52);
        margin.AddThemeConstantOverride("margin_bottom", 48);
        panel.AddChild(margin);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 30);
        margin.AddChild(column);

        var title = new Label { Text = "SETTINGS", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 52);
        title.AddThemeColorOverride("font_color", new Color("5c267e"));
        column.AddChild(title);

        column.AddChild(CreateToggle("SoundToggle", "Sound Effects", true));
        column.AddChild(CreateToggle("MusicToggle", "Music", true));

        var close = new Button
        {
            Name = "CloseSettingsButton",
            Text = "CLOSE",
            CustomMinimumSize = new Vector2(300, 82),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        close.AddThemeFontSizeOverride("font_size", 32);
        close.AddThemeColorOverride("font_color", new Color("fff4dd"));
        close.AddThemeStyleboxOverride("normal", PurpleButtonStyle(new Color("7b3db2")));
        close.AddThemeStyleboxOverride("hover", PurpleButtonStyle(new Color("9352c9")));
        close.Pressed += CloseSettings;
        column.AddChild(close);
        return overlay;
    }

    private static CheckButton CreateToggle(string name, string text, bool pressed)
    {
        var toggle = new CheckButton
        {
            Name = name,
            Text = text,
            ButtonPressed = pressed,
            CustomMinimumSize = new Vector2(420, 70),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        toggle.AddThemeFontSizeOverride("font_size", 30);
        toggle.AddThemeColorOverride("font_color", new Color("5c267e"));
        return toggle;
    }

    private static StyleBoxFlat PurpleButtonStyle(Color color) => new()
    {
        BgColor = color,
        BorderColor = new Color("f8d874"),
        BorderWidthLeft = 4,
        BorderWidthTop = 4,
        BorderWidthRight = 4,
        BorderWidthBottom = 4,
        CornerRadiusTopLeft = 24,
        CornerRadiusTopRight = 24,
        CornerRadiusBottomLeft = 24,
        CornerRadiusBottomRight = 24,
    };

    public void OpenSettings() => _settingsOverlay.Show();
    public void CloseSettings() => _settingsOverlay.Hide();
    public bool IsSettingsOpen => _settingsOverlay.Visible;
}
