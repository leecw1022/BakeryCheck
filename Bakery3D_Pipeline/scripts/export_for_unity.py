# ===== Bakery3D: 선택한 모델을 Unity용 FBX로 내보내기 =====
# 바꿀 곳은 아래 한 줄뿐입니다. (anpan / swissroll / croissant)
NAME = "anpan"

import bpy, os

blend_dir = os.path.dirname(bpy.data.filepath)            # ...\Bakery3D\01_work
export_dir = os.path.join(os.path.dirname(blend_dir), "02_export", NAME)
tex_dir = os.path.join(export_dir, "textures")
os.makedirs(tex_dir, exist_ok=True)

ob = bpy.context.active_object
assert ob is not None and ob.type == 'MESH', "빵 모델을 클릭해서 선택한 뒤 다시 실행하세요."

# 1) 텍스처 이름 바꾸고 PNG 파일로 저장 (원본 이미지 내용은 그대로)
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
            print("텍스처 저장:", path)

ob.name = NAME
ob.data.name = f"{NAME}_mesh"

# 2) FBX 내보내기 (Unity용 설정)
fbx_path = os.path.join(export_dir, NAME + ".fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,            # 선택한 모델만
    object_types={'MESH'},         # 카메라·조명 제외
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',  # Unity에서 Scale 1로 들어오게
    axis_forward='-Z', axis_up='Y',
    bake_space_transform=True,     # Unity에서 -90도 회전이 붙지 않게
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    use_tspace=True,               # 노멀맵용 정보 포함
    bake_anim=False,               # 빵은 애니메이션 없음
    path_mode='RELATIVE', embed_textures=False,  # textures 폴더를 가리킴
)
bpy.ops.wm.save_mainfile()
print("완료:", fbx_path)
