using Godot;

namespace AP6412Match3.Ui;

/// <summary>Local visual-only settings switch. Audio wiring is intentionally out of scope.</summary>
public partial class CrystalToggle : BaseButton
{
    private bool _hovered;

    public override void _Ready()
    {
        ToggleMode = true;
        CustomMinimumSize = new Vector2(76, 38);
        FocusMode = FocusModeEnum.All;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        Toggled += _ => QueueRedraw();
        MouseEntered += () => { _hovered = true; QueueRedraw(); };
        MouseExited += () => { _hovered = false; QueueRedraw(); };
        Resized += QueueRedraw;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color track = ButtonPressed ? new Color("9a5bc4") : new Color("685a74");
        if (_hovered) track = track.Lightened(0.12f);
        Rect2 bounds = new(Vector2.Zero, Size);
        var style = new StyleBoxFlat
        {
            BgColor = track,
            BorderColor = ButtonPressed ? new Color("f1cf75") : new Color("b9a9c6"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 19,
            CornerRadiusTopRight = 19,
            CornerRadiusBottomLeft = 19,
            CornerRadiusBottomRight = 19,
        };
        DrawStyleBox(style, bounds);
        float knobX = ButtonPressed ? Size.X - 19 : 19;
        DrawCircle(new Vector2(knobX, Size.Y / 2f), 14, new Color("fff4dd"));
        DrawCircle(new Vector2(knobX, Size.Y / 2f), 14, ButtonPressed ? new Color("f7df9a") : new Color("ddd5e3"), false, 2);
        Font font = CrystalTypography.Bold();
        string state = ButtonPressed ? "ON" : "OFF";
        Vector2 textPosition = ButtonPressed ? new Vector2(8, 24) : new Vector2(37, 24);
        DrawString(font, textPosition, state, HorizontalAlignment.Left, -1, 14, new Color("fff4dd"));
    }
}
