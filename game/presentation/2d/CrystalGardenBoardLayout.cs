using Godot;

namespace AP6412Match3.Presentation;

/// <summary>
/// Single measured mapping between the Crystal Garden board-frame PNG and BoardView2D.
/// Source grid values come from the visible slot-edge pixels in board_7x7_frame.png.
/// </summary>
public static class CrystalGardenBoardLayout
{
    public static readonly Rect2 FrameRect = new(625, 80, 745, 815);
    public static readonly Rect2 BoardHostRect = new(760, 260, 535, 535);

    private const float FrameSourceWidth = 1199;
    private const float FrameSourceHeight = 1312;
    private static readonly float FrameScale = Mathf.Min(
        FrameRect.Size.X / FrameSourceWidth,
        FrameRect.Size.Y / FrameSourceHeight);
    private static readonly Vector2 RenderedFrameSize = new(
        FrameSourceWidth * FrameScale,
        FrameSourceHeight * FrameScale);
    private static readonly Vector2 RenderedFrameOrigin =
        FrameRect.Position + (FrameRect.Size - RenderedFrameSize) / 2;

    // Linear fit of all seven measured source-space centers.
    private static readonly Vector2 SourceFirstCenter = new(224.054f, 353.643f);
    private static readonly Vector2 SourcePitch = new(126.125f, 128.286f);

    public static readonly Vector2 FirstCellCenter =
        RenderedFrameOrigin + SourceFirstCenter * FrameScale - BoardHostRect.Position;
    public static readonly Vector2 Pitch = SourcePitch * FrameScale;
}
