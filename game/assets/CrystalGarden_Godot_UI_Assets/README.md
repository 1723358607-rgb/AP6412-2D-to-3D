# Crystal Garden — Godot UI asset handoff

Prepared from `stitch_crystal_garden_match_3_ui.zip`. Original Stitch folder names renamed to semantic Godot-friendly English snake_case filenames. **No Godot code is changed**. All source artwork is provided as separate PNG textures; transparent portions are kept. Transparent PNGs with content are alpha-trimmed to eliminate large padding (except the full-screen background and overlay).

## Layout references
- `references/reference_home.jpg` — Home page target visual.
- `references/reference_level_selection.jpg` — Level-select target visual.

## Home screen (1920 × 1080 design coordinate space)
Use `assets/backgrounds/garden_background_16x9.png` as the full-screen landscape; overlay `assets/home/crystal_garden_logo.png` near the top center; use `button_play.png`, `button_levels.png`, and `button_settings.png` as **individually clickable** TextureButtons, arranged per the screenshot. Original reference screenshot uses a different background composition with a logo baked into it, while this export contains the image15 clean landscape; it is **similar in style, not pixel-identical**. Do not reuse screenshots as game backgrounds (would duplicate buttons).

## Level selection
Use the SAME `garden_background_16x9.png` behind the independent elements: back button (top-left), `select_level_title.png` (top-center), and three equal cards using `level_card_blank.png`. Each card contains the level label, one rounded level illustration, and three independent star assets. Level 3 illustration ONLY: draw `locked_overlay.png` above its illustration and `locked_padlock.png` above overlay; labels/stars stay above/clear. The locked overlay is a **uniform semi-transparent gray** RGBA export, so size it to match illustration, not the entire card. Use dynamic star states and progress unlock logic.

## Recommended Godot integration
- Copy `assets/` into `game/assets/ui/crystal_garden/` inside the existing shared Godot project (do not create another Godot project).
- Use Control + TextureRect/TextureButton, preserving aspect ratio; labels (Level 1/2/3) should be editable Godot text.
- The 2D and 3D gameplay versions can reuse these menu assets; game rules should remain separate.
- Test actual PNG alpha in Godot on a checkerboard background.
- Do not import screenshots into UI as interactive controls.

## Source asset mapping
| Original Stitch folder | Named PNG |
|---|---|
| `image15.png` | `assets/backgrounds/garden_background_16x9.png` |
| `image.png_3` | `assets/home/crystal_garden_logo.png` |
| `image.png_4` | `assets/home/button_play.png` |
| `image.png_2` | `assets/home/button_levels.png` |
| `image.png_1` | `assets/home/button_settings.png` |
| `image6.png` | `assets/shared/button_back.png` |
| `image.png_6` | `assets/levels/select_level_title.png` |
| `image.png_5` | `assets/levels/level_card_blank.png` |
| `image10.png` | `assets/levels/level_01_garden_ruins.png` |
| `image11.png` | `assets/levels/level_02_glass_gazebo.png` |
| `image12.png` | `assets/levels/level_03_archway_ruins.png` |
| `image8.png` | `assets/levels/star_earned.png` |
| `image9.png` | `assets/levels/star_unearned.png` |
| `image13.png` | `assets/levels/locked_overlay.png` |
| `image14.png` | `assets/levels/locked_padlock.png` |

## Important limitations
- No standalone gameplay board HUD, gameplay pieces, victory/defeat screens, or settings dialog assets exist in this source ZIP.
- The page title and home action buttons have baked-in text; make the whole respective PNG clickable rather than overlaying duplicate text.
- The sample star ratings (Level 1 3, Level 2 2, Level 3 locked) are *reference mock-up states*, not initial save data or gameplay rules.
