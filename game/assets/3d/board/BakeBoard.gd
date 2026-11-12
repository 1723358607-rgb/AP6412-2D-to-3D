# Authoring utility. Run with --headless --script res://assets/3d/board/BakeBoard.gd.
# Produces a static PackedScene; the game/editor never needs to execute this builder.
extends SceneTree
const DIR = "res://assets/3d/board/"
var board := Node3D.new()
var honey := metal("Gilt", "dfa256", .80, .30)
var light_gold := metal("Champagne", "ffda91", .76, .26)
var bronze := metal("AntiqueGold", "976031", .73, .32)
var base_mat := metal("VioletPorphyry", "65466e", .0, .74)
var reveal := metal("Recess", "422e51", .0, .79)

func metal(label:String, color:String, m:float, r:float) -> StandardMaterial3D:
 var mat:=StandardMaterial3D.new()
 mat.resource_name=label
 mat.albedo_color=Color(color)
 mat.metallic=m
 mat.roughness=r
 return mat

func group(parent:Node, label:String)->Node3D:
 var n:=Node3D.new()
 n.name=label
 parent.add_child(n)
 n.owner=board
 return n

func part(parent:Node,label:String,mesh:Mesh,mat:Material,pos:=Vector3.ZERO)->MeshInstance3D:
 var n:=MeshInstance3D.new()
 n.name=label
 n.mesh=mesh
 n.material_override=mat
 n.position=pos
 parent.add_child(n)
 n.owner=board
 return n

# Flat-shaded loft. The clockwise top outlines may be chamfered rectangles or gemstone belts.
func mesh_of(rings:Array, closed_caps:=true, facets:=false)->ArrayMesh:
 var s:=SurfaceTool.new()
 s.begin(Mesh.PRIMITIVE_TRIANGLES)
 var n:int=rings[0].size()
 for k in range(rings.size()-1):
  for i in n:
   var j:=(i+1)%n
   if facets:
    s.set_color([Color("eddafa"),Color("b58ae6"),Color("fdf1ff"),Color("c8a8ef")][(i+k)%4])
   tri(s,rings[k][i],rings[k][j],rings[k+1][j])
   tri(s,rings[k][i],rings[k+1][j],rings[k+1][i])
 if closed_caps:
  for i in range(1,n-1):
   tri(s,rings[-1][0],rings[-1][i],rings[-1][i+1])
   tri(s,rings[0][0],rings[0][i+1],rings[0][i])
 s.index()
 return s.commit()

func tri(s:SurfaceTool,a:Vector3,b:Vector3,c:Vector3):
 var normal:=(c-a).cross(b-a).normalized()
 if normal.length_squared()<.5: return
 for v in [a,b,c]:
  s.set_normal(normal)
  s.add_vertex(v)

func rim(w:float,d:float,cut:float,y:float)->Array:
 var x:=w*.5
 var z:=d*.5
 return [Vector3(-x+cut,y,-z),Vector3(x-cut,y,-z),Vector3(x,y,-z+cut),Vector3(x,y,z-cut),Vector3(x-cut,y,z),Vector3(-x+cut,y,z),Vector3(-x,y,z-cut),Vector3(-x,y,-z+cut)]

func slab(w:float,d:float,h:float,c:float,b:float)->Mesh:
 return mesh_of([rim(w-b*2,d-b*2,c*.75,0),rim(w,d,c,b),rim(w,d,c,h-b),rim(w-b*2,d-b*2,c*.75,h)])

func band(parent:Node,label:String,profile:Array,mat:Material):
 var rings:Array=[]
 for p in profile: rings.append(rim(p.x,p.x,p.z,p.y))
 part(parent,label,mesh_of(rings,false),mat)

# A tapered chased leaf: elliptical cross sections follow a quadratic Bezier.
func leaf(a:Vector3,b:Vector3,c:Vector3,width:float,depth:float,face:Vector3)->Mesh:
 var rings:Array=[]
 for j in 13:
  var t:=float(j)/12
  var center:=a.bezier_interpolate(a.lerp(b,2.0/3),c.lerp(b,2.0/3),c,t)
  var tangent:=(b-a).lerp(c-b,t).normalized()
  var side:=face.cross(tangent).normalized()
  var radius:=.04+.96*pow(sin(PI*t) if j<12 else 0.0,.8)
  var row:Array=[]
  for i in 6:
   var angle:=-TAU*i/6.0
   row.append(center+radius*(width*cos(angle)*side+depth*sin(angle)*face))
  rings.append(row)
 return mesh_of(rings)

func diamond(w:float,h:float,depth:float,facets:=false)->Mesh:
 var rings:Array=[]
 for q in [Vector2(.90,0),Vector2(1,depth*.18),Vector2(.55,depth*.86),Vector2(.17,depth)]:
  var row:Array=[]
  for point in [Vector2(0,-1),Vector2(.75,-.36),Vector2(1,0),Vector2(.75,.36),Vector2(0,1),Vector2(-.75,.36),Vector2(-1,0),Vector2(-.75,-.36)]:
   row.append(Vector3(point.x*w*q.x,q.y,point.y*h*q.x))
  rings.append(row)
 return mesh_of(rings,true,facets)

func _initialize():
 board.name="CrystalGardenBoard"
 var plinth:=group(board,"BoardBase")
 part(plinth,"InsetPlinth",slab(7.34,7.34,.18,.20,.035),reveal,Vector3(0,-.53,0))
 part(plinth,"Foot",slab(7.52,7.52,.11,.19,.025),reveal,Vector3(0,-.35,0))
 part(plinth,"Body",slab(7.64,7.64,.30,.17,.045),base_mat,Vector3(0,-.29,0))
 band(plinth,"FootBinding",[Vector3(7.54,-.294,.17),Vector3(7.60,-.276,.17),Vector3(7.60,-.25,.17)],bronze)
 var tiles:=group(board,"BoardTiles")
 var palette:Array=[]
 for color in ["9c83bc","9781af","aa89b8","9479af"]:
  var mat:=ShaderMaterial.new()
  mat.shader=load(DIR+"LavenderStone.gdshader")
  mat.set_shader_parameter("tint",Color(color))
  palette.append(mat)
 var tile:=slab(1,1,.15,.053,.026)
 var rng:=RandomNumberGenerator.new()
 rng.seed=6412
 for r in 7:
  for c in 7:
   part(tiles,"Tile_R%02d_C%02d"%[r,c],tile,palette[rng.randi_range(0,3)],Vector3((c-3)*1.03,.011,(r-3)*1.03))
 var frame:=group(board,"GoldFrame")
 band(frame,"RaisedBinding",[Vector3(7.58,-.06,.17),Vector3(7.68,-.018,.18),Vector3(7.68,.11,.18),Vector3(7.59,.171,.15),Vector3(7.29,.171,.095),Vector3(7.22,.13,.065),Vector3(7.22,.015,.065)],honey)
 band(frame,"FineBead",[Vector3(7.53,.171,.14),Vector3(7.51,.186,.14),Vector3(7.48,.186,.14),Vector3(7.46,.171,.14)],light_gold)
 var corners:=group(board,"CornerDecorations")
 var sprig:=leaf(Vector3(-.05,0,0),Vector3(-.49,.035,-.03),Vector3(-.69,0,-.16),.063,.037,Vector3.UP)
 var curl:=leaf(Vector3(-.03,0,-.03),Vector3(-.23,.05,-.28),Vector3(-.41,.005,-.17),.046,.032,Vector3.UP)
 var bud:=diamond(.105,.105,.07)
 for i in 4:
  var corner:=group(corners,"Corner%d"%i)
  corner.rotation.y=PI*.5*i
  var socket:=group(corner,"Mount")
  socket.position=Vector3(3.62,.181,3.62)
  for j in 2:
   var n:=part(socket,"Leaf%d"%j,sprig,honey)
   var k:=part(socket,"Tendril%d"%j,curl,light_gold)
   if j==1:
    n.transform.basis=Basis(Vector3.UP,-PI/2).scaled(Vector3(-1,1,1))
    k.transform.basis=n.transform.basis
  part(socket,"Finial",bud,light_gold)
 # Small four-point brass pins at interior seam intersections.
 var pins:=group(frame,"SeamPins")
 var pin:=mesh_of([[Vector3(0,0,-.104),Vector3(.021,0,-.024),Vector3(.10,0,0),Vector3(.021,0,.024),Vector3(0,0,.104),Vector3(-.021,0,.024),Vector3(-.10,0,0),Vector3(-.021,0,-.024)],[Vector3.ZERO,Vector3.ZERO,Vector3.ZERO,Vector3.ZERO,Vector3.ZERO,Vector3.ZERO,Vector3.ZERO,Vector3.ZERO].map(func(v):return v+Vector3.UP*.022)],false)
 for r in 6:
  for c in 6:
   part(pins,"Pin%d%d"%[r,c],pin,honey,Vector3((c-2.5)*1.03,.161,(r-2.5)*1.03))
 var emblem:=group(board,"CenterCrystal")
 emblem.position=Vector3(0,-.064,3.844)
 emblem.rotation.x=deg_to_rad(75)
 part(emblem,"AntiqueSetting",diamond(.33,.48,.086),bronze)
 part(emblem,"GoldBezel",diamond(.309,.45,.10),light_gold,Vector3.UP*.052)
 var violet:=metal("Amethyst","a54cdd",.09,.19)
 violet.emission_enabled=true
 violet.emission=Color("5d197f")
 violet.emission_energy_multiplier=.20
 violet.vertex_color_use_as_albedo=true
 var gem:=diamond(.243,.355,.167,true)
 part(emblem,"Amethyst",gem,violet,Vector3.UP*.121)
 var wing:=leaf(Vector3(.21,.045,.025),Vector3(.55,.08,-.30),Vector3(.93,.025,-.12),.077,.048,Vector3.UP)
 var scroll:=leaf(Vector3(.34,.028,.12),Vector3(.73,.11,-.09),Vector3(.67,.035,.085),.044,.030,Vector3.UP)
 for i in 2:
  var w:=part(emblem,"Wing%d"%i,wing,honey)
  var s:=part(emblem,"Scroll%d"%i,scroll,light_gold)
  if i==1: w.scale.x=-1; s.scale.x=-1
 var scene:=PackedScene.new()
 var err:=scene.pack(board)
 if err==OK: err=ResourceSaver.save(scene,DIR+"CrystalGardenBoard.tscn")
 board.free()
 quit(err)

