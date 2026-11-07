using Godot;

namespace AP6412Match3.Ui;

public partial class LevelSelectionScreen : Control
{
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void LevelRequestedEventHandler(int levelId);

    private ILevelProgressProvider _progress = new DemoLevelProgressProvider();
    private readonly List<LevelCardView> _cards = [];

    public override void _Ready() => BuildScreen();

    public void SetProgressProvider(ILevelProgressProvider progress)
    {
        _progress = progress;
        RefreshCards();
    }

    private void BuildScreen()
    {
        var background = CrystalGardenUi.Texture(
            "GardenBackground", "backgrounds/garden_background_16x9.png", this,
            new Rect2(0, 0, 1920, 1080));
        background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;

        CrystalGardenUi.Texture("SelectLevelTitle", "levels/select_level_title.png", this,
            new Rect2(600, 18, 720, 242));
        TextureButton back = CrystalGardenUi.TextureButton("BackButton", "shared/button_back.png", this,
            new Rect2(36, 34, 150, 100));
        back.Pressed += () => EmitSignal(SignalName.BackRequested);

        var row = new HBoxContainer
        {
            Name = "LevelCards",
            Position = new Vector2(282, 272),
            Size = new Vector2(1356, 500),
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", 48);
        AddChild(row);

        string[] illustrationPaths =
        [
            "levels/level_01_garden_ruins.png",
            "levels/level_02_glass_gazebo.png",
            "levels/level_03_archway_ruins.png",
        ];

        for (int levelId = 1; levelId <= 3; levelId++)
        {
            var card = new LevelCardView(levelId, illustrationPaths[levelId - 1]);
            card.LevelPressed += id => EmitSignal(SignalName.LevelRequested, id);
            _cards.Add(card);
            row.AddChild(card);
        }

        var dots = CrystalGardenUi.Label("PageIndicator", "●  ○  ○", this,
            new Rect2(760, 820, 400, 80), 48);
        dots.AddThemeColorOverride("font_color", new Color("ffffff"));
        dots.AddThemeColorOverride("font_outline_color", new Color("7f3bb0"));

        RefreshCards();
    }

    private void RefreshCards()
    {
        foreach (LevelCardView card in _cards)
            card.Apply(_progress.GetLevel(card.LevelId));
    }

    public bool IsLevelLocked(int levelId) => !_progress.GetLevel(levelId).IsUnlocked;
}

internal sealed partial class LevelCardView : Control
{
    public event Action<int>? LevelPressed;
    public int LevelId { get; }

    private readonly string _illustrationPath;
    private readonly List<TextureRect> _stars = [];
    private TextureRect _lockedOverlay = null!;
    private TextureRect _lockIcon = null!;
    private Button _hitArea = null!;

    public LevelCardView(int levelId, string illustrationPath)
    {
        LevelId = levelId;
        _illustrationPath = illustrationPath;
        Name = $"Level{levelId}Card";
        CustomMinimumSize = new Vector2(420, 500);
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _Ready()
    {
        CrystalGardenUi.Texture("CardBackground", "levels/level_card_blank.png", this,
            new Rect2(0, 0, 420, 458));
        Label levelTitle = CrystalGardenUi.Label("LevelTitle", $"LEVEL {LevelId}", this,
            new Rect2(55, 106, 310, 70), 48);
        var cinzelBold = new FontVariation
        {
            BaseFont = GD.Load<FontFile>("res://assets/fonts/Cinzel-VariableFont_wght.ttf"),
            VariationOpentype = new Godot.Collections.Dictionary
            {
                ["wght"] = 700,
            },
        };
        levelTitle.AddThemeFontOverride("font", cinzelBold);
        levelTitle.AddThemeColorOverride("font_color", new Color("543375"));
        levelTitle.AddThemeColorOverride("font_shadow_color", Colors.Transparent);
        levelTitle.AddThemeConstantOverride("outline_size", 0);
        levelTitle.AddThemeConstantOverride("shadow_outline_size", 0);
        levelTitle.AddThemeConstantOverride("shadow_offset_x", 0);
        levelTitle.AddThemeConstantOverride("shadow_offset_y", 0);

        var illustrationContainer = new Control
        {
            Name = "IllustrationContainer",
            Position = new Vector2(55, 156),
            Size = new Vector2(310, 194),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(illustrationContainer);

        TextureRect illustration = CrystalGardenUi.Texture(
            "LevelIllustration", _illustrationPath, illustrationContainer,
            new Rect2(Vector2.Zero, illustrationContainer.Size));
        illustration.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

        _lockedOverlay = CrystalGardenUi.Texture(
            "LockedOverlay", "levels/locked_overlay.png", illustrationContainer,
            new Rect2(Vector2.Zero, illustrationContainer.Size));
        _lockedOverlay.StretchMode = TextureRect.StretchModeEnum.Scale;
        _lockedOverlay.Material = CreateRoundedOverlayMaterial(illustration.Texture);

        var lockCenter = new CenterContainer { Name = "LockCenter" };
        CrystalGardenUi.FullRect(lockCenter);
        lockCenter.MouseFilter = MouseFilterEnum.Ignore;
        illustrationContainer.AddChild(lockCenter);

        _lockIcon = new TextureRect
        {
            Name = "LockIcon",
            Texture = GD.Load<Texture2D>($"{CrystalGardenUi.AssetRoot}/levels/locked_padlock.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(112, 106),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        lockCenter.AddChild(_lockIcon);

        var starContainer = new HBoxContainer
        {
            Name = "StarContainer",
            Position = new Vector2(108, 342),
            Size = new Vector2(204, 60),
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        starContainer.AddThemeConstantOverride("separation", 12);
        AddChild(starContainer);
        for (int index = 0; index < 3; index++)
        {
            TextureRect star = CrystalGardenUi.Texture(
                $"Star{index + 1}", "levels/star_unearned.png", starContainer,
                new Rect2(Vector2.Zero, new Vector2(60, 60)));
            star.CustomMinimumSize = new Vector2(60, 60);
            _stars.Add(star);
        }

        _hitArea = new Button
        {
            Name = "ClickTarget",
            Flat = true,
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            Position = Vector2.Zero,
            Size = new Vector2(420, 458),
        };
        _hitArea.Pressed += () => LevelPressed?.Invoke(LevelId);
        AddChild(_hitArea);
    }

    private static ShaderMaterial CreateRoundedOverlayMaterial(Texture2D illustrationTexture)
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                uniform sampler2D mask_texture : source_color, filter_linear_mipmap;

                void fragment() {
                    vec4 overlay = texture(TEXTURE, UV);
                    float illustration_alpha = texture(mask_texture, UV).a;
                    COLOR = vec4(overlay.rgb, overlay.a * illustration_alpha);
                }
                """,
        };
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("mask_texture", illustrationTexture);
        return material;
    }

    public void Apply(LevelUiState state)
    {
        if (!IsNodeReady())
            return;

        for (int index = 0; index < _stars.Count; index++)
        {
            string file = index < state.BestStars ? "star_earned.png" : "star_unearned.png";
            _stars[index].Texture = GD.Load<Texture2D>($"{CrystalGardenUi.AssetRoot}/levels/{file}");
        }

        _lockedOverlay.Visible = !state.IsUnlocked;
        _lockIcon.Visible = !state.IsUnlocked;
        _hitArea.Disabled = !state.IsUnlocked;
        _hitArea.MouseDefaultCursorShape = state.IsUnlocked ? CursorShape.PointingHand : CursorShape.Forbidden;
    }
}
