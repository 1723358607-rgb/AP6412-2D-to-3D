# Offline authoring only: build reusable geometry, never run during gameplay.
extends SceneTree
const DIR = "res://assets/3d/pieces/"
var streams: Dictionary
var failure := false

func surface() -> SurfaceTool:
	var s := SurfaceTool.new()
	s.begin(Mesh.PRIMITIVE_TRIANGLES)
	return s

func begin_mesh(parts: Array) -> void:
	streams = {}
	for key in parts:
		streams[key] = surface()

func save_mesh(file: String) -> void:
	var mesh := ArrayMesh.new()
	for key in streams:
		var s: SurfaceTool = streams[key]
		s.index()
		s.commit(mesh)
		mesh.surface_set_name(mesh.get_surface_count()-1,key)
		var material_path := DIR + "materials/" + str(key) + ".tres"
		if ResourceLoader.exists(material_path):
			mesh.surface_set_material(mesh.get_surface_count()-1,load(material_path))
	if ResourceSaver.save(mesh,DIR+"meshes/"+file+".res",ResourceSaver.FLAG_COMPRESS) != OK:
		push_error("Cannot save "+file)
		failure = true

# Godot front faces are clockwise. Orient from an explicit outward vector.
func face(key: String, poly: Array, outward: Vector3, xf: Transform3D, value: float, grain_offset: Vector2 = Vector2.ZERO, lip := 0.0) -> void:
	if (poly[2]-poly[0]).cross(poly[1]-poly[0]).dot(outward) < 0:
		poly.reverse()
	var n: Vector3 = (poly[2]-poly[0]).cross(poly[1]-poly[0]).normalized()
	var s: SurfaceTool = streams[key]
	for i in range(1, poly.size()-1):
		for idx in [0,i,i+1]:
			var p: Vector3 = poly[idx]
			s.set_normal((xf.basis*n).normalized())
			s.set_color(Color(value,value,value,1))
			if key == "Cut":
				s.set_uv(Vector2(value,lip))
			else:
				s.set_uv(grain_offset+Vector2(p.x,p.z if abs(n.y)>abs(n.z) else p.y))
			s.add_vertex(xf*p)

func gem_face(poly: Array, tint: float) -> void:
	var mid := Vector3.ZERO
	for p in poly: mid += p
	mid /= float(poly.size())
	var outward := mid-Vector3.UP*.5
	var inset := []
	for p in poly: inset.append(p.lerp(mid,0.028))
	face("Cut",inset.duplicate(),outward,Transform3D.IDENTITY,tint)
	for i in poly.size():
		var j := (i+1)%poly.size()
		face("Cut",[poly[i],poly[j],inset[j],inset[i]],outward,Transform3D.IDENTITY,tint,Vector2.ZERO,0.75)

func cut_crystal() -> void:
	begin_mesh(["Cut"])
	var lower := []
	var upper := []
	for i in 8:
		var a := TAU*i/8.
		var xz := Vector3(.35*sin(a),0,.35*cos(a))
		lower.append(xz+Vector3.UP*(.33+.035*cos(2*a)))
		upper.append(xz+Vector3.UP*(.67+.035*cos(2*a)))
	var crown := [.87,.51,.76,.91,.46,.67,.95,.62]
	var body := [.34,.66,.25,.52,.74,.32,.63,.45]
	var base := [.66,.37,.83,.48,.68,.89,.39,.78]
	for i in 8:
		var j := (i+1)%8
		gem_face([upper[i],Vector3.UP,upper[j]],crown[i])
		gem_face([lower[i],upper[i],upper[j],lower[j]],body[i])
		gem_face([lower[j],Vector3.ZERO,lower[i]],base[i])
	save_mesh("DoubleTerminated")

# Bevelled plank or plate with grain running along local X.
func plank(key: String, dims: Vector3, origin: Vector3, basis: Basis, shade: float, bevel := .009) -> void:
	var xf := Transform3D(basis,origin)
	var h := dims*.5
	var b := minf(bevel,h[h.min_axis_index()]*.65)
	var rings := []
	for k in 4:
		var end := k==0 or k==3
		var y := h.y-(b*.72 if end else 0.0)
		var z := h.z-(b*.72 if end else 0.0)
		var cut := b*(.40 if end else 1.)
		var x: float = [-h.x,-h.x+b,h.x-b,h.x][k]
		var ring := []
		for pair in [[-y,-z+cut],[-y+cut,-z],[y-cut,-z],[y,-z+cut],[y,z-cut],[y-cut,z],[-y+cut,z],[-y,z-cut]]:
			ring.append(Vector3(x,pair[0],pair[1]))
		rings.append(ring)
	var uv_offset := Vector2(shade*7.1,origin.y*.37+origin.x*.27+shade)
	for k in 3:
		for i in 8:
			var j := (i+1)%8
			var poly := [rings[k][i],rings[k+1][i],rings[k+1][j],rings[k][j]]
			var out: Vector3 = poly[0]+poly[1]+poly[2]+poly[3]
			face(key,poly,out,xf,shade,uv_offset)
	face(key,rings[0].duplicate(),Vector3.LEFT,xf,shade,uv_offset)
	face(key,rings[3].duplicate(),Vector3.RIGHT,xf,shade,uv_offset)

# Hammered seven-sided button rivet; no invisible back cap.
func stud(pos: Vector3, basis: Basis, rad := .026) -> void:
	var xf := Transform3D(basis,pos)
	for i in 7:
		var a := TAU*i/7.
		var b := TAU*(i+1)/7.
		var p := Vector3(cos(a),sin(a),0)*rad
		var q := Vector3(cos(b),sin(b),0)*rad
		var s := p*.71+Vector3.BACK*.008
		var t := q*.71+Vector3.BACK*.008
		face("Iron",[p,q,t,s],p+q+Vector3.BACK*.02,xf,1.)
		face("Iron",[s,t,Vector3.BACK*.012],Vector3.BACK,xf,1.)

func crate() -> void:
	begin_mesh(["Timber","Iron"])
	# Core provides depth behind narrow seams and an actual bottom at y=0.
	plank("Timber",Vector3(.76,.83,.76),Vector3.UP*.415,Basis.IDENTITY,.11)
	for side in 5:
		var b := Basis(Vector3.UP,side*PI*.5) if side<4 else Basis(Vector3.RIGHT,-PI*.5)
		var o := Vector3.UP*.43
		for i in 5:
			var v := -.292+i*.146
			plank("Timber",Vector3(.73,.141,.040),o+b*Vector3(v,0,.383),b*Basis(Vector3.BACK,PI*.5),[.35,.68,.43,.86,.48][(i+side)%5])
		# Flush X: the second diagonal is two short half-lap pieces, not coplanar beams.
		plank("Timber",Vector3(.89,.076,.029),o+b*Vector3(0,0,.410),b*Basis(Vector3.BACK,PI*.25),.85,.006)
		for sign_x in [-1,1]:
			var off: float = .25*sign_x
			plank("Timber",Vector3(.388,.076,.029),o+b*Vector3(off*.7071,-off*.7071,.410),b*Basis(Vector3.BACK,-PI*.25),.66,.006)
		stud(o+b*Vector3(0,0,.425),b,.018)
		for sign_edge in [-1,1]:
			plank("Timber",Vector3(.835,.087,.047),o+b*Vector3(0,sign_edge*.373,.397),b,.97)
			plank("Timber",Vector3(.66,.087,.047),o+b*Vector3(sign_edge*.373,0,.397),b*Basis(Vector3.BACK,PI*.5),.69)
		for sx in [-1,1]:
			for sy in [-1,1]:
				var xy := Vector3(sx*.358,sy*.358,0)
				plank("Iron",Vector3(.119,.119,.031),o+b*(xy+Vector3.BACK*.422),b,.76,.011)
				stud(o+b*(xy+Vector3.BACK*.4375),b,.024)
	save_mesh("CrateBody")
	begin_mesh(["Band","Iron"])
	# Two continuous straps over the lid and down both broad faces.
	for sx in [-1,1]:
		var x: float = sx*.22
		plank("Band",Vector3(.846,.017,.078),Vector3(x,.867,0),Basis(Vector3.UP,PI*.5),.7,.004)
		for sz in [-1,1]:
			var b := Basis.IDENTITY if sz>0 else Basis(Vector3.UP,PI)
			plank("Band",Vector3(.846,.078,.017),Vector3(x,.445,sz*.432),b*Basis(Vector3.BACK,PI*.5),.7,.004)
			for y in [.18,.71]:
				stud(Vector3(x,y,sz*.4375),b,.016)
		for z in [-.26,.26]:
			stud(Vector3(x,.8755,z),Basis(Vector3.RIGHT,-PI*.5),.016)
	save_mesh("Reinforcement")

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(DIR+"meshes"))
	cut_crystal()
	crate()
	begin_mesh(["PedestalStone","Gilt"])
	plank("PedestalStone",Vector3(1.20,.12,1.20), Vector3(0,-.142,0),Basis.IDENTITY,.7,.04)
	plank("Gilt",Vector3(1.195,.026,1.195), Vector3(0,-.077,0),Basis.IDENTITY,.8,.012)
	plank("PedestalStone",Vector3(1.24,.075,1.24), Vector3(0,-.0375,0),Basis.IDENTITY,.7,.028)
	save_mesh("DisplayTile")
	quit(1 if failure else 0)
