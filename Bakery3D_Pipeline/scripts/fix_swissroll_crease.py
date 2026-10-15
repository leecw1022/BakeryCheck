# ===== 롤케이크 옆면의 접힌 자국 다듬기 =====
# 자국 위치(롤케이크 옆면) 주변 반경 2cm 안의 점만 골라 부드럽게 펴 줍니다.
# 다른 부분은 건드리지 않습니다.
import bpy, bmesh
from mathutils import Vector

SPOT = Vector((-0.131, -0.139, 0.136))   # 자국 위치 (AI가 파일에서 찾은 좌표)
RADIUS = 0.02                             # 2cm

ob = bpy.context.active_object
assert ob and ob.type == 'MESH', "롤케이크를 클릭해서 선택한 뒤 다시 실행하세요."
if ob.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')

bm = bmesh.new(); bm.from_mesh(ob.data)
verts = [v for v in bm.verts if (v.co - SPOT).length < RADIUS]
print("다듬을 점 개수:", len(verts))
for _ in range(10):
    bmesh.ops.smooth_vert(bm, verts=verts, factor=0.5,
                          use_axis_x=True, use_axis_y=True, use_axis_z=True)
bm.to_mesh(ob.data); bm.free(); ob.data.update()
print("완료")
