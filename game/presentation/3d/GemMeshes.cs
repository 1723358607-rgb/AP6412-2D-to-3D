using Godot;
using AP6412Match3.Core;
namespace AP6412Match3.Presentation;

/// <summary>Procedural solid meshes/materials only. No matching, random or level logic.</summary>
public sealed class GemMeshes
{
    private readonly Dictionary<PieceColor, Mesh> _meshes;
    private readonly Dictionary<PieceColor, StandardMaterial3D> _materials;
    public StandardMaterial3D Wood { get; } = Material("985b38", .78f);
    public StandardMaterial3D Band { get; } = Material("f0bd68", .3f, .5f);
    public StandardMaterial3D Tile { get; } = Material("73568b", .52f);
    public StandardMaterial3D Base { get; } = Material("38294f", .5f, .2f);

    public GemMeshes()
    {
        _materials = new() { [PieceColor.Red] = Material("f58ac1", .27f, .2f),
            [PieceColor.Blue] = Material("65d7f3", .24f, .28f),
            [PieceColor.Green] = Material("b783ed", .2f, .3f),
            [PieceColor.Yellow] = Material("f5ce69", .28f, .55f) };
        _meshes = new() { [PieceColor.Red] = Heart(), [PieceColor.Blue] = Crystal(12, true),
            [PieceColor.Green] = Crystal(4, false),
            [PieceColor.Yellow] = new CylinderMesh { TopRadius = .36f, BottomRadius = .42f, Height = .37f, RadialSegments = 6 } };
    }
    public Node3D Cell(BoardCell cell)
    {
        var root = new Node3D();
        if (cell.Kind == CellKind.Piece)
        {
            root.AddChild(new MeshInstance3D { Mesh = _meshes[cell.Color], MaterialOverride = _materials[cell.Color],
                Position = cell.Color == PieceColor.Yellow ? new Vector3(0, .43f, 0) : Vector3.Zero });
        }
        else if (cell.Kind == CellKind.Box)
        {
            root.AddChild(Box(new Vector3(.78f, .64f, .83f), new Vector3(0, .33f, 0), Wood));
            for (int i = 0; i < cell.BoxLayers; i++)
            {
                float z = cell.BoxLayers == 1 ? 0 : -.22f + i * .44f;
                root.AddChild(Box(new Vector3(.82f, .055f, .08f), new Vector3(0, .68f, z), Band));
            }
            root.AddChild(new Label3D { Text = cell.BoxLayers.ToString(), FontSize = 80, PixelSize = .006f,
                Position = new Vector3(0, .87f, 0), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Modulate = new Color("fff1c7"), OutlineModulate = new Color("38243f"), OutlineSize = 18 });
        }
        return root;
    }
    public static MeshInstance3D Box(Vector3 size, Vector3 at, Material material) =>
        new() { Mesh = new BoxMesh { Size = size }, Position = at, MaterialOverride = material };
    private static StandardMaterial3D Material(string color, float roughness, float metallic = 0) =>
        new() { AlbedoColor = new Color(color), Roughness = roughness, Metallic = metallic,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled };

    private static ArrayMesh Crystal(int sides, bool drop)
    {
        using var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 top = new(0, drop ? 1.0f : .9f, 0), bottom = new(0, .12f, 0);
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.Tau / sides, b = (i + 1) * Mathf.Tau / sides;
            Vector3 v = new(Mathf.Cos(a) * .42f, .38f, Mathf.Sin(a) * .42f);
            Vector3 w = new(Mathf.Cos(b) * .42f, .38f, Mathf.Sin(b) * .42f);
            Triangle(tool, top, w, v); Triangle(tool, bottom, v, w);
        }
        return tool.Commit();
    }
    private static ArrayMesh Heart()
    {
        const int count = 32; var edge = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float t = i * Mathf.Tau / count;
            edge[i] = new Vector2(16 * Mathf.Pow(Mathf.Sin(t), 3),
                -(13 * Mathf.Cos(t) - 5 * Mathf.Cos(2*t) - 2 * Mathf.Cos(3*t) - Mathf.Cos(4*t))) * .023f;
        }
        using var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < count; i++)
        {
            Vector2 a = edge[i], b = edge[(i + 1) % count];
            Vector3 v = new(a.X, .62f, a.Y), w = new(b.X, .62f, b.Y);
            Vector3 lowV = new(a.X, .30f, a.Y), lowW = new(b.X, .30f, b.Y);
            Triangle(tool, new Vector3(0,.62f,0), w, v);
            Triangle(tool, new Vector3(0,.30f,0), lowV, lowW);
            Triangle(tool, v, w, lowW); Triangle(tool, v, lowW, lowV);
        }
        return tool.Commit();
    }
    private static void Triangle(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (b-a).Cross(c-a).Normalized();
        tool.SetNormal(normal); tool.AddVertex(a); tool.AddVertex(b); tool.AddVertex(c);
    }
}
