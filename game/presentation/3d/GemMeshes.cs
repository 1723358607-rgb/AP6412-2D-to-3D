using Godot;
using AP6412Match3.Core;

namespace AP6412Match3.Presentation;

/// <summary>Maps logical cell types to the Crystal Garden 3D scenes.</summary>
public sealed class GemMeshes
{
    private const string PieceDirectory = "res://assets/3d/pieces/";
    private const float PieceScale = .84f;
    private readonly Dictionary<PieceColor, PackedScene> _crystals = new()
    {
        [PieceColor.Red] = Load("CrystalPink"),
        [PieceColor.Blue] = Load("CrystalBlue"),
        [PieceColor.Green] = Load("CrystalPurple"),
        [PieceColor.Yellow] = Load("CrystalGold"),
    };
    private readonly PackedScene _crate = Load("WoodenCrate_01");
    private readonly PackedScene _reinforcedCrate = Load("WoodenCrate_02");

    public Node3D Cell(BoardCell cell)
    {
        var root = new Node3D();
        PackedScene scene = cell.Kind == CellKind.Box
            ? (cell.BoxLayers > 1 ? _reinforcedCrate : _crate)
            : _crystals[cell.Color];
        var model = scene.Instantiate<MeshInstance3D>();
        model.Scale = Vector3.One * PieceScale;
        root.AddChild(model);

        if (cell.Kind == CellKind.Box)
        {
            root.AddChild(new Label3D
            {
                Text = cell.BoxLayers.ToString(),
                FontSize = 80,
                PixelSize = .0038f,
                Position = new Vector3(0, .80f, .12f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Modulate = new Color("fff1c7"),
                OutlineModulate = new Color("38243f"),
                OutlineSize = 18,
            });
        }
        return root;
    }

    public static Aabb Bounds(Node3D cell)
    {
        var model = cell.GetChild<MeshInstance3D>(0);
        Aabb bounds = model.GetAabb();
        foreach (Node child in model.GetChildren())
            if (child is MeshInstance3D mesh)
                bounds = bounds.Merge(mesh.Transform * mesh.GetAabb());
        return model.Transform * bounds;
    }

    private static PackedScene Load(string name) =>
        GD.Load<PackedScene>(PieceDirectory + name + ".tscn");
}
