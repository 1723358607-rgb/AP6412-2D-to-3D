using Godot;
using System.Threading.Tasks;

namespace AP6412Match3.Assets.Board;

// Preview-only camera controls. The reusable board scene has no script or dependencies on gameplay.
public partial class BoardPreview : Node3D
{
    private static readonly string[] Views = { "Isometric", "Top", "Detail" };

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        int i = key.Keycode switch { Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, _ => -1 };
        if (i >= 0) GetNode<Camera3D>(Views[i]).MakeCurrent();
    }

    public override async void _Ready()
    {
        if (!System.Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--capture-board")) return;
        var board = GetNode<Node3D>("CrystalGardenBoard/BoardTiles");
        if (board.GetChildCount() != 49) { GetTree().Quit(1); return; }
        for (int r = 0; r < 7; ++r)
        for (int c = 0; c < 7; ++c)
        {
            var tile = board.GetNodeOrNull<MeshInstance3D>($"Tile_R{r:D2}_C{c:D2}");
            if (tile?.Mesh == null || !tile.Position.IsEqualApprox(new Vector3((c-3)*1.03f,.011f,(r-3)*1.03f)))
            { GD.PushError("Invalid board tile"); GetTree().Quit(1); return; }
        }
        string dir = ProjectSettings.GlobalizePath("res://assets/3d/board/previews");
        DirAccess.MakeDirRecursiveAbsolute(dir);
        foreach (string view in Views)
        {
            GetNode<Camera3D>(view).MakeCurrent();
            for (int i = 0; i < 5; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng($"{dir}/board_{view.ToLowerInvariant()}.png") != Error.Ok)
            { GetTree().Quit(1); return; }
            GD.Print($"[BOARD_CAPTURE] {view} {image.GetSize()}, 49 tiles verified");
        }
        GetTree().Quit();
    }
}
