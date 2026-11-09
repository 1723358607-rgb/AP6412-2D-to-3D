using Godot;
using AP6412Match3.Presentation;
using GDictionary = Godot.Collections.Dictionary;

namespace AP6412Match3.Ui;

/// <summary>Crystal Garden gameplay presentation. Reads snapshots; never owns board rules.</summary>
public partial class GameplayScreen : Control
{
    private const string GameplayAssets = "res://assets/CrystalGarden_Gameplay_Assets";
    private const string UiAssets = "res://assets/CrystalGarden_Godot_UI_Assets/assets";
    private const string CinzelFont = "res://assets/fonts/Cinzel-VariableFont_wght.ttf";

    [Signal] public delegate void LeaveConfirmedEventHandler();

    private Label _levelValue = null!;
    private Label _goalValue = null!;
    private Label _movesValue = null!;
    private Label _notice = null!;
    private Control _leaveDialog = null!;
    private Control _settingsDialog = null!;

    public override void _Ready()
    {
        BuildScreen();
    }

    public void ApplySnapshot(GDictionary snapshot)
    {
        int level = ReadInt(snapshot, "level_id", 1);
        _levelValue.Text = $"LEVEL {Math.Max(level, 1)}";
        _goalValue.Text = ReadInt(snapshot, "target_remaining", 0).ToString();
        _movesValue.Text = ReadInt(snapshot, "moves_remaining", 0).ToString();
        string phase = snapshot.TryGetValue("phase", out Variant value) ? value.AsString() : "idle";
        _notice.Text = phase == "resolving" ? "RESOLVING…" : "";
    }

    public bool IsLeaveDialogOpen => _leaveDialog.Visible;
    public bool IsSettingsOpen => _settingsDialog.Visible;

    private void BuildScreen()
    {
        TextureRect background = Texture("GardenBackground", $"{UiAssets}/backgrounds/garden_background_16x9.png", this,
            new Rect2(0, 0, 1920, 1080), TextureRect.StretchModeEnum.KeepAspectCovered);

        var veil = new ColorRect
        {
            Name = "GameplayVeil", Position = Vector2.Zero, Size = new Vector2(1920, 1080),
            Color = new Color(0.18f, 0.06f, 0.28f, 0.12f), MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(veil);

        BuildLeftHud();
        BuildBoard();
        BuildTopButtons();
        BuildProgress();
        _leaveDialog = BuildLeaveDialog();
        _settingsDialog = BuildSettingsDialog();
    }

    private void BuildLeftHud()
    {
        Texture("GoalMovesPanel", $"{GameplayAssets}/hud_goal_moves_panel.png", this,
            new Rect2(68, 150, 390, 676));
        _levelValue = StyledLabel("LevelValue", "LEVEL 1", this, new Rect2(123, 245, 315, 70), 48, "fff4dd");
        Texture("GoalCrate", $"{GameplayAssets}/obstacle_wooden_crate.png", this,
            new Rect2(154, 416, 106, 90));
        _goalValue = StyledLabel("GoalValue", "5", this, new Rect2(260, 423, 112, 78), 58, "543375");
        _movesValue = StyledLabel("MovesValue", "15", this, new Rect2(128, 635, 270, 100), 72, "543375");
    }

    private void BuildBoard()
    {
        Texture("BoardFrame", $"{GameplayAssets}/board_7x7_frame.png", this,
            CrystalGardenBoardLayout.FrameRect);
        var host = new Control
        {
            Name = "BoardHost",
            Position = CrystalGardenBoardLayout.BoardHostRect.Position,
            Size = CrystalGardenBoardLayout.BoardHostRect.Size,
            MouseFilter = MouseFilterEnum.Ignore, ClipContents = false,
        };
        AddChild(host);
        _notice = StyledLabel("PhaseNotice", "", this, new Rect2(790, 755, 415, 42), 23, "fff4dd");
    }

    private void BuildTopButtons()
    {
        TextureButton back = Button("BackButton", $"{UiAssets}/shared/button_back.png", this,
            new Rect2(35, 34, 148, 100));
        back.Pressed += () => _leaveDialog.Show();

        TextureButton settings = Button("SettingsButton", $"{GameplayAssets}/button_settings.png", this,
            new Rect2(1600, 38, 118, 100));
        settings.Pressed += () => _settingsDialog.Show();

        TextureButton pause = Button("PauseButton", $"{GameplayAssets}/button_pause.png", this,
            new Rect2(1740, 38, 118, 100));
        pause.TooltipText = "Pause interface pending Gameplay API";
        pause.Pressed += () =>
        {
            _notice.Text = "PAUSE API PENDING";
            GD.Print("[GAMEPLAY_UI_PENDING] GameSession has no pause/resume contract; gameplay was not paused.");
        };
    }

    private void BuildProgress()
    {
        Texture("ProgressFrame", $"{GameplayAssets}/hud_bottom_progress_frame.png", this,
            new Rect2(530, 838, 960, 228));
        Texture("ProgressTrack", $"{GameplayAssets}/progress_track_empty.png", this,
            new Rect2(682, 912, 650, 52));
        for (int index = 0; index < 3; index++)
        {
            TextureRect star = Texture($"ProgressStar{index + 1}", $"{UiAssets}/levels/star_unearned.png", this,
                new Rect2(815 + index * 105, 894, 78, 78));
            star.Modulate = new Color(1, 1, 1, 0.96f);
            star.ZIndex = 2;
        }
    }

    private Control BuildLeaveDialog()
    {
        Control dialog = ModalBase("LeaveDialog");
        StyledLabel("Prompt", "LEAVE THIS LEVEL?", dialog, new Rect2(560, 375, 800, 90), 48, "fff4dd");
        StyledLabel("Detail", "Current board progress will not be saved.", dialog,
            new Rect2(570, 470, 780, 48), 24, "fff4dd");
        Button cancel = TextButton("CancelButton", "CANCEL", dialog, new Rect2(700, 565, 230, 76));
        cancel.Pressed += dialog.Hide;
        Button leave = TextButton("ConfirmLeaveButton", "LEAVE", dialog, new Rect2(990, 565, 230, 76));
        leave.Pressed += () => EmitSignal(SignalName.LeaveConfirmed);
        dialog.Hide();
        return dialog;
    }

    private Control BuildSettingsDialog()
    {
        Control dialog = ModalBase("SettingsDialog");
        StyledLabel("Title", "SETTINGS", dialog, new Rect2(650, 360, 620, 90), 50, "fff4dd");
        CheckButton music = new() { Name = "MusicToggle", Text = "MUSIC", ButtonPressed = true,
            Position = new Vector2(785, 480), Size = new Vector2(350, 52) };
        CheckButton sound = new() { Name = "SoundToggle", Text = "SOUND EFFECTS", ButtonPressed = true,
            Position = new Vector2(785, 540), Size = new Vector2(350, 52) };
        dialog.AddChild(music); dialog.AddChild(sound);
        Button close = TextButton("CloseSettingsButton", "CLOSE", dialog, new Rect2(845, 630, 230, 70));
        close.Pressed += dialog.Hide;
        dialog.Hide();
        return dialog;
    }

    private Control ModalBase(string name)
    {
        var modal = new Control { Name = name, Position = Vector2.Zero, Size = new Vector2(1920, 1080) };
        AddChild(modal);
        var dim = new ColorRect { Color = new Color(0.08f, 0.02f, 0.15f, 0.76f),
            Position = Vector2.Zero, Size = new Vector2(1920, 1080) };
        modal.AddChild(dim);
        var panel = new ColorRect { Color = new Color("543375"), Position = new Vector2(545, 330), Size = new Vector2(830, 410),
            MouseFilter = MouseFilterEnum.Ignore };
        modal.AddChild(panel);
        return modal;
    }

    private static TextureRect Texture(string name, string path, Control parent, Rect2 rect,
        TextureRect.StretchModeEnum stretch = TextureRect.StretchModeEnum.KeepAspectCentered)
    {
        var node = new TextureRect { Name = name, Texture = GD.Load<Texture2D>(path),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = stretch,
            MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(node);
        node.Position = rect.Position;
        node.Size = rect.Size;
        return node;
    }

    private static TextureButton Button(string name, string path, Control parent, Rect2 rect)
    {
        var node = new TextureButton { Name = name, TextureNormal = GD.Load<Texture2D>(path),
            IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
        parent.AddChild(node);
        node.Position = rect.Position;
        node.Size = rect.Size;
        return node;
    }

    private static Button TextButton(string name, string text, Control parent, Rect2 rect)
    {
        var button = new Button { Name = name, Text = text, Position = rect.Position, Size = rect.Size,
            FocusMode = FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 24);
        parent.AddChild(button);
        return button;
    }

    private static Label StyledLabel(string name, string text, Control parent, Rect2 rect, int size, string color)
    {
        var font = new FontVariation
        {
            BaseFont = GD.Load<FontFile>(CinzelFont),
            VariationOpentype = new Godot.Collections.Dictionary { ["wght"] = 700 },
        };
        var label = new Label { Name = name, Text = text, Position = rect.Position, Size = rect.Size,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(color));
        label.AddThemeConstantOverride("outline_size", 0);
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 0);
        parent.AddChild(label);
        return label;
    }

    private static int ReadInt(GDictionary snapshot, string key, int fallback) =>
        snapshot.TryGetValue(key, out Variant value) ? value.AsInt32() : fallback;
}
