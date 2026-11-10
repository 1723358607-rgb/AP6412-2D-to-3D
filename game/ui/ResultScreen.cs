using Godot;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Ui;

/// <summary>Crystal Garden result presentation. It displays core results without recalculating them.</summary>
public partial class ResultScreen : Control
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void LevelsRequestedEventHandler();
    [Signal] public delegate void NextRequestedEventHandler();

    private GDictionary _result = new();
    private int _bestStars;

    public void Configure(GDictionary result, int bestStars)
    {
        _result = result.Duplicate(true);
        _bestStars = Math.Clamp(bestStars, 0, 3);
    }

    public override void _Ready()
    {
        BuildScreen();
    }

    private void BuildScreen()
    {
        bool won = ReadBool("won");
        int level = ReadInt("level_id");
        int stars = Math.Clamp(ReadInt("stars"), 0, 3);
        int movesRemaining = Math.Max(ReadInt("moves_remaining"), 0);

        TextureRect background = CrystalGardenUi.Texture(
            "GardenBackground", "backgrounds/garden_background_16x9.png", this,
            new Rect2(0, 0, 1920, 1080));
        background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;

        var veil = new ColorRect
        {
            Name = "ResultVeil",
            Position = Vector2.Zero,
            Size = new Vector2(1920, 1080),
            Color = new Color(0.19f, 0.05f, 0.28f, 0.18f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(veil);

        CrystalGardenUi.Texture("ResultFrame", "levels/level_card_blank.png", this,
            new Rect2(570, 64, 780, 930));

        // The frame artwork reserves its upper area for the crystal ornament.
        StyledLabel("ResultTitle", won ? level == 3 ? "GARDEN COMPLETE!" : "LEVEL COMPLETE!" : "TRY AGAIN",
            new Rect2(650, 315, 620, 48), 34, CrystalTypography.Bold(), new Color("4a285f"));
        StyledLabel("LevelLabel", $"LEVEL {level}",
            new Rect2(730, 382, 460, 40), 25, CrystalTypography.SemiBold(), new Color("5b3373"));

        for (int index = 0; index < 3; index++)
        {
            string texture = index < stars ? "levels/star_earned.png" : "levels/star_unearned.png";
            CrystalGardenUi.Texture($"EarnedStar{index + 1}", texture, this,
                new Rect2(790 + index * 115, 430, 100, 100));
        }

        AddStatPanel("MovesPanel", "MOVES LEFT", movesRemaining.ToString(), new Rect2(680, 530, 270, 105));
        AddStatPanel("RunPanel", "THIS RUN", $"{stars}/3", new Rect2(970, 530, 270, 105));
        StyledLabel("BestHeading", "BEST", new Rect2(825, 642, 130, 44), 21,
            CrystalTypography.SemiBold(), new Color("5b3373"));
        StyledLabel("BestValue", $"{_bestStars}/3", new Rect2(955, 642, 140, 44), 29,
            CrystalTypography.Bold(), new Color("4a285f"));
        // Preserve the integration-test data contract without reintroducing duplicate visible copy.
        var resultDetail = new Label
        {
            Name = "ResultDetail",
            Text = $"LEVEL {level} · MOVES LEFT {movesRemaining} · THIS RUN {stars}/3 · BEST {_bestStars}/3",
            Visible = false,
        };
        AddChild(resultDetail);

        if (won && level < 3)
        {
            StyledButton("RetryButton", "RETRY", new Rect2(700, 695, 250, 62), 21,
                () => EmitSignal(SignalName.RetryRequested));
            StyledButton("LevelsButton", "LEVELS", new Rect2(970, 695, 250, 62), 21,
                () => EmitSignal(SignalName.LevelsRequested));
            StyledButton("NextButton", "NEXT LEVEL", new Rect2(835, 768, 250, 62), 20,
                () => EmitSignal(SignalName.NextRequested));
        }
        else
        {
            StyledButton("RetryButton", "RETRY", new Rect2(700, 715, 250, 64), 21,
                () => EmitSignal(SignalName.RetryRequested));
            StyledButton("LevelsButton", "LEVELS", new Rect2(970, 715, 250, 64), 21,
                () => EmitSignal(SignalName.LevelsRequested));
        }
    }

    private void AddStatPanel(string name, string heading, string value, Rect2 rect)
    {
        var panel = new Panel { Name = name, Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.14f, 0.48f, 0.72f),
            BorderColor = new Color("e8c56e"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 22,
            CornerRadiusTopRight = 22,
            CornerRadiusBottomLeft = 22,
            CornerRadiusBottomRight = 22,
        });
        AddChild(panel);
        StyledLabel($"{name}Heading", heading,
            new Rect2(rect.Position + new Vector2(8, 7), new Vector2(rect.Size.X - 16, 34)), 19,
            CrystalTypography.SemiBold(), new Color("fff4dd"));
        StyledLabel($"{name}Value", value,
            new Rect2(rect.Position + new Vector2(8, 38), new Vector2(rect.Size.X - 16, 58)), 31,
            CrystalTypography.Bold(), Colors.White);
    }

    private void StyledButton(string name, string text, Rect2 rect, int fontSize, Action action)
    {
        var button = new Button
        {
            Name = name,
            Text = text,
            Position = rect.Position,
            Size = rect.Size,
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            PivotOffset = rect.Size / 2f,
        };
        button.AddThemeFontOverride("font", CrystalTypography.Bold());
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", new Color("fff4dd"));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", new Color("fff4dd"));
        button.AddThemeColorOverride("font_disabled_color", new Color("c8b7cf"));
        button.AddThemeStyleboxOverride("normal", ButtonStyle(new Color("71329b")));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(new Color("8d48b9")));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(new Color("5c267e")));
        button.Pressed += action;
        button.MouseEntered += () => AnimateScale(button, new Vector2(1.035f, 1.035f));
        button.MouseExited += () => AnimateScale(button, Vector2.One);
        AddChild(button);
    }

    private void StyledLabel(string name, string text, Rect2 rect, int fontSize, Font font, Color color)
    {
        var label = new Label
        {
            Name = name,
            Text = text,
            Position = rect.Position,
            Size = rect.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 0);
        AddChild(label);
    }

    private static StyleBoxFlat ButtonStyle(Color color) => new()
    {
        BgColor = color,
        BorderColor = new Color("f1cf75"),
        BorderWidthLeft = 4,
        BorderWidthTop = 4,
        BorderWidthRight = 4,
        BorderWidthBottom = 4,
        CornerRadiusTopLeft = 30,
        CornerRadiusTopRight = 30,
        CornerRadiusBottomLeft = 30,
        CornerRadiusBottomRight = 30,
        ShadowColor = new Color(0.18f, 0.03f, 0.28f, 0.45f),
        ShadowSize = 8,
    };

    private static void AnimateScale(Control control, Vector2 target)
    {
        Tween tween = control.CreateTween();
        tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(control, "scale", target, 0.11);
    }

    private int ReadInt(string key) =>
        _result.TryGetValue(key, out Variant value) ? value.AsInt32() : 0;

    private bool ReadBool(string key) =>
        _result.TryGetValue(key, out Variant value) && value.AsBool();
}
