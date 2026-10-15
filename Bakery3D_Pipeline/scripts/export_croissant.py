# ===== 크루아상 캐릭터를 Unity용 FBX로 내보내기 (뼈대 + Idle/Walk 포함) =====
import bpy, os

NAME = "croissant"
scene = bpy.context.scene
if bpy.context.object and bpy.context.object.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')
mesh = bpy.data.objects.get("croissant")
rig = bpy.data.objects.get("croissant_rig")
assert mesh and rig, "뼈대 만들기 코드를 먼저 실행하세요."

blend_dir = os.path.dirname(bpy.data.filepath)
export_dir = os.path.join(os.path.dirname(blend_dir), "02_export", NAME)
tex_dir = os.path.join(export_dir, "textures")
os.makedirs(tex_dir, exist_ok=True)

# 1) 텍스처 이름 croissant_ 로 바꿔 저장
for slot in mesh.material_slots:
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

# 2) 뼈대와 모델만 선택
for o in bpy.context.view_layer.objects:
    o.select_set(False)
mesh.select_set(True); rig.select_set(True)
bpy.context.view_layer.objects.active = rig
rig.animation_data.action = bpy.data.actions["Idle"]
scene.frame_set(0)

# 3) 내보내기 - 동작(Action)마다 따로 담기
fbx_path = os.path.join(export_dir, NAME + ".fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_path, use_selection=True, object_types={'ARMATURE', 'MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', bake_space_transform=False,
    use_mesh_modifiers=False, mesh_smooth_type='FACE', use_tspace=True,
    add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
    use_armature_deform_only=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
    path_mode='RELATIVE', embed_textures=False,
)
bpy.ops.wm.save_mainfile()
print("완료:", fbx_path)
