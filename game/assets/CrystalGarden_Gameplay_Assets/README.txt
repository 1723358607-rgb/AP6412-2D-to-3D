Crystal Garden - Gameplay UI assets

12 source images from the uploaded Stitch ZIP, renamed for Godot.
Additional button_resume_play.png extracted from earlier generated Play/Pause art.
All assets are PNG RGBA. board_7x7_frame.png had a flattened checkerboard in its source and has been processed to remove the external background; check edge transparency in Godot.

Suggested destination: game/assets/crystal_garden_gameplay/
The four gem images are separate sprites, not a baked board.
The progress fill and empty track are separate graphics, useful for masking/clipping in Godot.
The HUD panel has only GOAL/MOVES captions; dynamic counts and LEVEL text should be Godot labels.
