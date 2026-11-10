using Godot;
using AP6412Match3.Core;
using AP6412Match3.Gameplay;

namespace AP6412Match3.Presentation;

/// <summary>Real 3D meshes in a viewport, projected ray picking and computed-step animation.</summary>
public partial class BoardView3D : Node3D, IBoardPresenter
{
    private SubViewport _viewport = null!;
    private Camera3D _camera = null!;
    private Node3D _cells = null!;
    private MeshInstance3D _selection = null!;
    private TextureRect _surface = null!;
    private GemMeshes _models = null!;
    private readonly Dictionary<GridPosition, Node3D> _nodes = [];
    private bool _interactive;
    private int _version;
    public BoardInteraction? Interaction { get; private set; }
    public BoardModel? PresentedBoard { get; private set; }
    public int PresentedSteps { get; private set; }
    public int RenderedCellCount => _nodes.Count;
    public Aabb MeshBounds(GridPosition p) => _nodes[p].GetChild<MeshInstance3D>(0).Mesh.GetAabb();
    public event Action<BoardStep>? StepPresented;
    [Signal] public delegate void SwapRequestedEventHandler(Vector2I from, Vector2I to);

    public override void _Ready()
    {
        _models = new GemMeshes();
        _viewport = new SubViewport { Name = "BoardViewport", Size = new Vector2I(1080,1096),
            OwnWorld3D = true, TransparentBg = true, GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Msaa3D = Viewport.Msaa.Msaa4X };
        AddChild(_viewport);
        var world = new Node3D { Name = "BoardWorld" }; _viewport.AddChild(world);
        world.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("241c38"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("d5c5e8"), AmbientLightEnergy = .75f } });
        _camera = new Camera3D { Name = "BoardCamera", Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 8.8f, Position = new Vector3(0,10,6), Current = true, Near = .1f, Far = 50 };
        world.AddChild(_camera); _camera.LookAt(Vector3.Zero);
        world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50,-30,0), LightEnergy = 1.5f, ShadowEnabled = true });
        world.AddChild(new OmniLight3D { Position = new Vector3(-4,5,3), OmniRange = 14,
            LightColor = new Color("ffd6ef"), LightEnergy = 1.4f });
        world.AddChild(GemMeshes.Box(new Vector3(7.8f,.3f,8.9f),new Vector3(0,-.35f,0),_models.Base));
        for(int y=0;y<7;y++) for(int x=0;x<7;x++)
            world.AddChild(GemMeshes.Box(new Vector3(.94f,.10f,1.04f),Point(new(x,y))+new Vector3(0,-.11f,0),_models.Tile));
        _cells = new Node3D { Name = "Cells" }; world.AddChild(_cells);
        _selection = new MeshInstance3D { Name = "SelectedCell", Mesh = new TorusMesh { InnerRadius = .43f, OuterRadius = .49f, Rings = 24, RingSegments = 6 },
            MaterialOverride = _models.Band, Scale = new Vector3(1,1,1.12f), Visible = false }; world.AddChild(_selection);
        _surface = new TextureRect { Name = "Board3DSurface", Texture = _viewport.GetTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        // The application owns the surface between disposable B pages.
        var parking = new CanvasLayer { Name = "SurfaceParking", Layer = -1 }; AddChild(parking); parking.AddChild(_surface);
        SetProcessUnhandledInput(false);
    }
    public void AttachHost(Control host)
    {
        if (_surface.GetParent()!=host) _surface.Reparent(host,false);
        _surface.Position = CrystalGardenBoardLayout.FirstCellCenter - Vector2.One * BoardView2D.CellSize/2;
        _surface.Size = new Vector2(6*CrystalGardenBoardLayout.Pitch.X+BoardView2D.CellSize,
            6*CrystalGardenBoardLayout.Pitch.Y+BoardView2D.CellSize);
        _surface.Show(); Show();
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
    }
    public void Park()
    {
        if (_surface is null) return;
        var parking = GetNode<CanvasLayer>("SurfaceParking");
        if (_surface.GetParent()!=parking) _surface.Reparent(parking,false);
        _surface.Hide(); Hide();
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }
    public void SetBoard(BoardModel board)
    {
        _version++; Interaction = new BoardInteraction(board); PresentedBoard = board;
        Rebuild(board); _selection.Hide();
        if (_surface.GetParent() is Control) { _surface.Show(); Show(); }
    }
    public void SetInteractive(bool enabled) { _interactive=enabled; SetProcessUnhandledInput(enabled); if(!enabled)_selection.Hide(); }
    public void ClearBoard() { _version++; Interaction=null; PresentedBoard=null; _selection.Hide(); ClearNodes(); SetInteractive(false); Park(); }
    private static Vector3 Point(GridPosition p) => new(p.Column-3,0,(p.Row-3)*1.12f);
    private void ClearNodes() { foreach(var node in _nodes.Values) node.Free(); _nodes.Clear(); }
    private void Rebuild(BoardModel board)
    {
        ClearNodes();
        for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)
        {
            var p=new GridPosition(x,y); var cell=board.GetCell(p); if(cell.Kind==CellKind.Empty)continue;
            var node=_models.Cell(cell); node.Name=$"Cell_{x}_{y}"; node.Position=Point(p); _cells.AddChild(node); _nodes[p]=node;
        }
    }
    public Vector2 CellCanvasCenter(GridPosition p)
    {
        Vector2 projected=_camera.UnprojectPosition(Point(p)+new Vector3(0,.42f,0));
        return _surface.GetGlobalTransformWithCanvas() * (projected/(Vector2)_viewport.Size*_surface.Size);
    }
    public bool TryPick(Vector2 canvasPosition,out GridPosition position)
    {
        position=default; if(Interaction is null || !_surface.IsVisibleInTree())return false;
        Vector2 local=_surface.GetGlobalTransformWithCanvas().AffineInverse()*canvasPosition;
        if(!new Rect2(Vector2.Zero,_surface.Size).HasPoint(local))return false;
        Vector2 pixel=local/_surface.Size*(Vector2)_viewport.Size;
        Vector3 origin=_camera.ProjectRayOrigin(pixel), direction=_camera.ProjectRayNormal(pixel);
        float nearest=float.PositiveInfinity; bool hit=false;
        for(int y=0;y<Interaction.Board.Height;y++)for(int x=0;x<Interaction.Board.Width;x++)
        {
            GridPosition p=new(x,y); Vector3 center=Point(p)+new Vector3(0,.42f,0);
            if(RayBox(origin,direction,center-new Vector3(.43f,.58f,.48f),center+new Vector3(.43f,.58f,.48f),out float t)&&t<nearest)
            { nearest=t; position=p;hit=true; }
        }
        return hit;
    }
    // Ray versus 3D collision proxies: works independently of physics-frame timing.
    private static bool RayBox(Vector3 origin,Vector3 direction,Vector3 min,Vector3 max,out float distance)
    {
        float near=0,far=float.PositiveInfinity;
        for(int axis=0;axis<3;axis++)
        {
            if(Mathf.Abs(direction[axis])<.000001f) {if(origin[axis]<min[axis]||origin[axis]>max[axis]){distance=0;return false;}continue;}
            float a=(min[axis]-origin[axis])/direction[axis],b=(max[axis]-origin[axis])/direction[axis];
            if(a>b)(a,b)=(b,a);near=Mathf.Max(near,a);far=Mathf.Min(far,b);
            if(near>far){distance=0;return false;}
        }
        distance=near;return far>=0;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(!_interactive || input is not InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Left} mouse || !TryPick(mouse.Position,out var p))return;
        var outcome=Interaction!.Click(p);
        _selection.Visible=Interaction.Selected.HasValue;
        if(Interaction.Selected is {} selected)_selection.Position=Point(selected)+new Vector3(0,.08f,0);
        if(outcome==SelectionOutcome.RequestAccepted && Interaction.LastAcceptedRequest is {} request)
            EmitSignal(SignalName.SwapRequested,new Vector2I(request.From.Column,request.From.Row),new Vector2I(request.To.Column,request.To.Row));
        GetViewport().SetInputAsHandled();
    }
    public async Task PresentAsync(BoardStep step,CancellationToken cancellationToken)
    {
        int version=++_version; Rebuild(step.Kind==BoardStepKind.Reshuffle?step.After:step.Before);
        var motions=new List<(Node3D Node,Vector3 From,Vector3 To,bool Spawned)>();
        foreach(var m in step.Motions)
        {
            Node3D? node;
            if(m.Spawned) {node=_models.Cell(m.Cell);_cells.AddChild(node);_nodes[m.To]=node;}
            else if(!_nodes.TryGetValue(m.From,out node))continue;
            motions.Add((node,Point(m.From)+(m.Spawned?Vector3.Up*1.1f:Vector3.Zero),Point(m.To),m.Spawned));
        }
        double elapsed=0,duration=step.Kind==BoardStepKind.Clear?.18:.16;
        while(elapsed<duration)
        {
            cancellationToken.ThrowIfCancellationRequested(); if(version!=_version)throw new OperationCanceledException();
            float t=Mathf.SmoothStep(0,1,(float)(elapsed/duration));
            foreach(var m in motions){m.Node.Position=m.From.Lerp(m.To,t);m.Node.Scale=Vector3.One*(m.Spawned?Mathf.Lerp(.1f,1,t):1);}
            foreach(var p in step.Cleared)if(_nodes.TryGetValue(p,out var node))node.Scale=Vector3.One*Mathf.Max(.01f,1-t);
            if(step.Kind==BoardStepKind.Clear)foreach(var p in step.DamagedBoxes??[])
                if(_nodes.TryGetValue(p,out var node))node.Scale=Vector3.One*(step.Before.GetCell(p).BoxLayers==1?Mathf.Max(.01f,1-t):1+.12f*Mathf.Sin(t*Mathf.Pi));
            if(step.Kind==BoardStepKind.Reshuffle)foreach(var node in _nodes.Values)node.Scale=Vector3.One*Mathf.Lerp(.25f,1,t);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);elapsed+=GetProcessDeltaTime();
        }
        cancellationToken.ThrowIfCancellationRequested();if(version!=_version)throw new OperationCanceledException();
        PresentedBoard=step.After;PresentedSteps++;StepPresented?.Invoke(step);
    }
    public override void _ExitTree() { _version++; }
}
