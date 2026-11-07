using Godot;

namespace AP6412Match3.Ui;

internal static class CrystalGardenUi
{
    public const string AssetRoot = "res://assets/CrystalGarden_Godot_UI_Assets/assets";

    public static TextureRect Texture(string name, string path, Control parent, Rect2 rect)
    {
        var node = new TextureRect
        {
            Name = name,
            Texture = GD.Load<Texture2D>($"{AssetRoot}/{path}"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = rect.Position,
            Size = rect.Size,
        };
        parent.AddChild(node);
        return node;
    }

    public static TextureButton TextureButton(string name, string path, Control parent, Rect2 rect)
    {
        var texture = GD.Load<Texture2D>($"{AssetRoot}/{path}");
        var button = new TextureButton
        {
            Name = name,
            TextureNormal = texture,
            IgnoreTextureSize = true,
            StretchMode = Godot.TextureButton.StretchModeEnum.KeepAspectCentered,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            FocusMode = Control.FocusModeEnum.None,
            Position = rect.Position,
            Size = rect.Size,
            PivotOffset = rect.Size / 2f,
        };
        parent.AddChild(button);
        AddHoverScale(button);
        return button;
    }

    public static Label Label(string name, string text, Control parent, Rect2 rect, int fontSize)
    {
        var label = new Label
        {
            Name = name,
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = rect.Position,
            Size = rect.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color("fff4dd"));
        label.AddThemeColorOverride("font_outline_color", new Color("5d257e"));
        label.AddThemeConstantOverride("outline_size", 8);
        parent.AddChild(label);
        return label;
    }

    public static void FullRect(Control node)
    {
        node.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    private static void AddHoverScale(BaseButton button)
    {
        button.MouseEntered += () => AnimateScale(button, new Vector2(1.035f, 1.035f));
        button.MouseExited += () => AnimateScale(button, Vector2.One);
    }

    private static void AnimateScale(Control control, Vector2 target)
    {
        Tween tween = control.CreateTween();
        tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(control, "scale", target, 0.11);
    }
}
