using Godot;

namespace AP6412Match3.Ui;

/// <summary>Shared popup typography using the project's Crystal Garden serif family.</summary>
public static class CrystalTypography
{
    private const int WeightTag = 0x77676874; // OpenType 'wght'.
    public const string FontPath = "res://assets/fonts/Cinzel-VariableFont_wght.ttf";
    public const int SemiBoldWeight = 600;
    public const int BoldWeight = 700;

    public static FontVariation SemiBold() => WithWeight(SemiBoldWeight);
    public static FontVariation Bold() => WithWeight(BoldWeight);

    private static FontVariation WithWeight(int weight) => new()
    {
        BaseFont = GD.Load<FontFile>(FontPath),
        VariationOpentype = new Godot.Collections.Dictionary { [WeightTag] = weight },
    };
}
