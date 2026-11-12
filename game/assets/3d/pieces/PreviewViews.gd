# Preview only. 1 = overview, 2 = four crystals, 3 = cut detail, 4 = crates.
extends Node3D
func change_view(view: String) -> void:
	get_node(view).make_current()
	if not has_node("Gems"): return
	$Gems.visible = view != "Crates"
	$CratesGroup.visible = view == "Overview" or view == "Crates"
	for gem in $Gems.get_children(): gem.visible = view != "Close" or gem.name == &"Pink"

func _unhandled_key_input(event: InputEvent) -> void:
	if not has_node("Gems") or not event is InputEventKey or not event.pressed or event.echo: return
	var views := {KEY_1:"Overview", KEY_2:"Crystals", KEY_3:"Close", KEY_4:"Crates"}
	if views.has(event.keycode): change_view(views[event.keycode])

func _ready() -> void:
	if not "--capture-pieces" in OS.get_cmdline_user_args(): return
	var views := {"Crystals":"crystal_variants","Close":"crystal_closeup","Crates":"crate_variants"} if has_node("Gems") else {"Overview":"pieces_on_board"}
	var dir := ProjectSettings.globalize_path("res://assets/3d/pieces/previews/")
	if DirAccess.make_dir_recursive_absolute(dir) != OK:
		get_tree().quit(1)
		return
	for view in views:
		change_view(view)
		for frame in 5: await RenderingServer.frame_post_draw
		var image := get_viewport().get_texture().get_image()
		if image == null or image.save_png(dir+views[view]+".png") != OK:
			push_error("Cannot capture "+view)
			get_tree().quit(1)
			return
		print("[PIECE_CAPTURE] ",views[view]," ",image.get_size())
	get_tree().quit()
