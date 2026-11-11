# Crystal Garden board
Open BoardPreview.tscn and run the current scene (F6). Press 1 for the three-quarter view, 2 for the top view, or 3 for the front bezel detail. The preview lights, floor and cameras are separate from the reusable CrystalGardenBoard.tscn. The board itself is a static scene and is visible before running it.

## Placement
Use the board root's local space, in Godot units. Column increases along +X and row increases along +Z, towards the front amethyst. Each of the 49 meshes is named BoardTiles/Tile_Rrr_Ccc (zero-based, two digits per index). Tile width/depth = 1.00, height = 0.15, gap = 0.03, pitch = 1.03. The grid footprint is 7.18 × 7.18.

For row r and column c, the surface center is Vector3((c - 3) * 1.03, 0.161, (r - 3) * 1.03). Row 0 is at Z = -3.09 and column 0 at X = -3.09. Place a bottom-pivot piece at local Y = 0.162; for a centered mesh add its half-height. If the board is transformed, convert this local point with the board root's ToGlobal(). There are no collisions, game state or input handlers in this asset.

## Authoring
BakeBoard.gd rebuilds the baked scene deterministically when run explicitly with Godot --headless --script res://assets/3d/board/BakeBoard.gd. Normal editing and gameplay do not run it. To refresh the three PNGs, run BoardPreview.tscn with user argument --capture-board on a graphical renderer.

