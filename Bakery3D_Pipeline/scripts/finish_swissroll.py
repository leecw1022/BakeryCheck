# ===== 롤케이크 마무리: 자국 다듬기 + 저장 + Unity용 FBX 내보내기 =====
# 실행 전: 오른쪽 위 목록에서 롤케이크(texture_mapping 또는 swissroll)를 클릭해 선택
import bpy, bmesh, os
from mathutils import Vector

NAME = "swissroll"
SPOT = Vector((-0.131, -0.139, 0.136))   # 옆면 자국 위치
RADIUS = 0.02

ob = bpy.context.active_object
if ob is None or ob.type != 'MESH':
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(meshes) == 1, "롤케이크를 클릭해서 선택한 뒤 다시 실행하세요."
    ob = meshes[0]
for o in bpy.context.selected_objects:
    o.select_set(False)
ob.select_set(True); bpy.context.view_layer.objects.active = ob
if ob.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')

# 1) 자국 다듬기 (반경 2cm 안의 점만)
bm = bmesh.new(); bm.from_mesh(ob.data)
verts = [v for v in bm.verts if (v.co - SPOT).length < RADIUS]
for _ in range(10):
    bmesh.ops.smooth_vert(bm, verts=verts, factor=0.5,
                          use_axis_x=True, use_axis_y=True, use_axis_z=True)
bm.to_mesh(ob.data); bm.free(); ob.data.update()
print("자국 다듬은 점 개수:", len(verts))

# 2) 텍스처 이름 바꿔 저장
blend_dir = os.path.dirname(bpy.data.filepath)
export_dir = os.path.join(os.path.dirname(blend_dir), "02_export", NAME)
tex_dir = os.path.join(export_dir, "textures")
os.makedirs(tex_dir, exist_ok=True)
for slot in ob.material_slots:
    mat = slot.material
    if not mat or not mat.use_nodes:
        continue
    mat.name = f"{NAME}_mat"
    for node in mat.node_tree.nodes:
        if node.type == 'TEX_IMAGE' and node.image:
            img = node.image
            base = img.name.split(".")[0]
            if not base.startswith(NAME + "_"):
                base = f"{NAME}_{base}"
            img.name = base
            path = os.path.join(tex_dir, base + ".png")
            img.filepath_raw = path
            img.file_format = 'PNG'
            img.save()
            img.filepath = path

ob.name = NAME
ob.data.name = f"{NAME}_mesh"

# 3) FBX 내보내기 (단팥빵과 같은 설정)
fbx_path = os.path.join(export_dir, NAME + ".fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_path, use_selection=True, object_types={'MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', bake_space_transform=True,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', use_tspace=True,
    bake_anim=False, path_mode='RELATIVE', embed_textures=False,
)
# 4) 작업 파일 저장
bpy.ops.wm.save_mainfile()
print("완료:", fbx_path)
