# Crystal Garden · 3D pieces
Open PiecesPreview.tscn and press F6. Keys 1–4 show the entire set, the four cuts, a single jewel and the two crates. PiecesOnBoardPreview.tscn is a separate static fit study on the actual board. Neither preview is part of the game startup.

## Instantiating a model
Use CrystalPink.tscn, CrystalBlue.tscn, CrystalPurple.tscn or CrystalGold.tscn for gems. Use WoodenCrate_01.tscn and WoodenCrate_02.tscn for the regular and reinforced crate visuals. All six are static Node3D-compatible scenes with no scripts, collisions, input or game state. The origin is at the bottom, centered on X/Z. In board-local coordinates (+X = column, +Z = row):
```csharp
var model = GD.Load<PackedScene>("res://assets/3d/pieces/CrystalBlue.tscn").Instantiate<Node3D>();
board.AddChild(model);
model.Position = new Vector3((col - 3) * 1.03f, 0.161f, (row - 3) * 1.03f);
```
The integration parent must use the board's local frame. Keep crates axis-aligned on the board. Width × depth × height: gems 0.70 × 0.70 × 1.00; regular crate 0.899 × 0.899 × 0.8795; reinforced crate 0.899 × 0.899 × 0.8875, including all metalwork.

## Resources
- The four gem scenes instance one DoubleTerminated ArrayMesh. Its facet coordinates drive four shared palette materials using Facets.gdshader. The inner pavilion is an opaque optical effect and works without glow.
- Both crates use CrateBody with two surfaces (wood and steel); the reinforced scene inherits the regular crate and adds the Reinforcement mesh. Timber.gdshader aligns wood grain with each individual plank and brace.
- DisplayTile and PreviewStudio are for presentation only. The plinth and lighting use the stone and studio shaders from the existing board folder.
- All four meshes are already baked. To edit their geometry, change BakePieceMeshes.gd and run Godot with --headless --path game --script res://assets/3d/pieces/BakePieceMeshes.gd from the repository root. Normal loading does not run the baker.
- To refresh previews, launch each preview scene on a graphical renderer with --resolution 1600x900 and pass -- --capture-pieces. The four captured viewport PNGs go to previews/.
