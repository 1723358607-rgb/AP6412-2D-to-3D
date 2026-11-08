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

        CrystalGardenUi.Texture("SettingsFrame", "levels/level_card_blank.png", overlay,
            new Rect2(610, 150, 700, 760));
        StyledLabel("SettingsTitle", "SETTINGS", overlay, new Rect2(710, 335, 500, 54), 34,
            CrystalTypography.Bold(), HorizontalAlignment.Center, new Color("4a285f"));

        AddSettingRow(overlay, "Sound Effects", "SoundToggle", new Rect2(720, 430, 480, 76));
        AddSettingRow(overlay, "Music", "MusicToggle", new Rect2(720, 525, 480, 76));

        var close = new Button
        {
            Name = "CloseSettingsButton",
            Text = "CLOSE",
            Position = new Vector2(810, 650),
            Size = new Vector2(300, 72),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        close.AddThemeFontOverride("font", CrystalTypography.Bold());
        close.AddThemeFontSizeOverride("font_size", 21);
        close.AddThemeColorOverride("font_color", new Color("fff4dd"));
        close.AddThemeColorOverride("font_hover_color", Colors.White);
        close.AddThemeColorOverride("font_pressed_color", Colors.White);
        close.AddThemeColorOverride("font_focus_color", new Color("fff4dd"));
        close.AddThemeColorOverride("font_disabled_color", new Color("c8b7cf"));
        close.AddThemeStyleboxOverride("normal", PurpleButtonStyle(new Color("7b3db2")));
        close.AddThemeStyleboxOverride("hover", PurpleButtonStyle(new Color("9352c9")));
        close.Pressed += CloseSettings;
        close.AddThemeStyleboxOverride("pressed", PurpleButtonStyle(new Color("5c267e")));
        overlay.AddChild(close);
        return overlay;
    }

    private static void AddSettingRow(Control parent, string text, string toggleName, Rect2 rect)
    {
        var panel = new Panel
        {
            Name = $"{toggleName}Row",
            Position = rect.Position,
            Size = rect.Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.34f, 0.13f, 0.47f, 0.76f),
            BorderColor = new Color(0.91f, 0.77f, 0.43f, 0.7f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
        });
        parent.AddChild(panel);
        StyledLabel($"{toggleName}Label", text.ToUpperInvariant(), parent,
            new Rect2(rect.Position + new Vector2(28, 8), new Vector2(310, 60)), 21,
            CrystalTypography.SemiBold(), HorizontalAlignment.Left, new Color("fff4dd"));
        var toggle = new CrystalToggle
        {
            Name = toggleName,
            ToggleMode = true,
            ButtonPressed = true,
            Position = rect.Position + new Vector2(rect.Size.X - 104, 19),
            Size = new Vector2(76, 38),
        };
        parent.AddChild(toggle);
    }

    private static void StyledLabel(string name, string text, Control parent, Rect2 rect, int size,
        Font font, HorizontalAlignment alignment, Color color)
    {
        var label = new Label
        {
            Name = name,
            Text = text,
            Position = rect.Position,
            Size = rect.Size,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 0);
        parent.AddChild(label);
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
