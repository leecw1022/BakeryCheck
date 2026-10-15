# ===== 크루아상 캐릭터: 뼈대 5개 + Idle/Walk 동작 만들기 =====
# 실행 전: croissant_work.blend 를 열어 둔 상태 (크기 확정까지 끝난 파일)
# 하는 일
#  1) 뼈대(Armature) 만들기: root / body / arm.L / arm.R / leg.L / leg.R
#  2) 몸의 각 부분이 어느 뼈를 따라 움직일지 정하기 (팔·다리 위치를 기준으로 자동 계산)
#  3) 반복되는 동작 2개 만들기: Idle(2초, 숨쉬기), Walk(1초, 한 걸음씩 두 번)
#  4) 저장
import bpy, math
from mathutils import Vector

FPS = 24

# ---------- 0. 준비 ----------
scene = bpy.context.scene
scene.render.fps = FPS
mesh = None
for o in scene.objects:
    if o.type == 'MESH':
        mesh = o
        break
assert mesh is not None, "캐릭터 모델을 찾을 수 없습니다."
if bpy.context.object and bpy.context.object.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')

# 이미 만든 뼈대가 있으면 지우고 새로 만들기 (여러 번 실행해도 안전하게)
for o in list(scene.objects):
    if o.type == 'ARMATURE' and o.name.startswith("croissant_rig"):
        bpy.data.objects.remove(o, do_unlink=True)
for m in list(mesh.modifiers):
    if m.type == 'ARMATURE':
        mesh.modifiers.remove(m)
mesh.vertex_groups.clear()
mesh.parent = None
for a in list(bpy.data.actions):
    if a.name in ("Idle", "Walk"):
        bpy.data.actions.remove(a)

mesh.name = "croissant"
mesh.data.name = "croissant_mesh"

# ---------- 1. 뼈대 ----------
# 위치는 AI가 모델 모양을 분석해서 정한 값 (단위 m, 원점 = 발밑 중앙, 얼굴 = -Y 방향)
SH_L, HAND_L = Vector((0.47, -0.30, 0.42)), Vector((0.50, -0.40, 0.14))   # 왼팔(캐릭터 기준 오른쪽 화면)
HIP_L, FOOT_L = Vector((0.21, -0.06, 0.16)), Vector((0.21, -0.06, 0.0))
def mirror(v): return Vector((-v.x, v.y, v.z))

arm_data = bpy.data.armatures.new("croissant_rig_data")
rig = bpy.data.objects.new("croissant_rig", arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
for o in bpy.context.selected_objects: o.select_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones
def bone(name, head, tail, parent=None, roll=0.0):
    b = eb.new(name); b.head = head; b.tail = tail; b.roll = roll
    if parent: b.parent = eb[parent]
    return b
bone("root", (0, 0, 0), (0, 0.25, 0))
bone("body", (0, 0, 0.12), (0, 0, 0.85), "root")
bone("arm.L", SH_L, HAND_L, "body")
bone("arm.R", mirror(SH_L), mirror(HAND_L), "body")
bone("leg.L", HIP_L, FOOT_L, "root")
bone("leg.R", mirror(HIP_L), mirror(FOOT_L), "root")
bpy.ops.object.mode_set(mode='OBJECT')

# ---------- 2. 어느 부분이 어느 뼈를 따라갈지 (가중치) ----------
def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length, t
def smooth(x0, x1, x):  # x0에서 1, x1에서 0으로 부드럽게
    if x <= x0: return 1.0
    if x >= x1: return 0.0
    t = (x - x0) / (x1 - x0); return 1 - t * t * (3 - 2 * t)

groups = {n: mesh.vertex_groups.new(name=n) for n in ("body", "arm.L", "arm.R", "leg.L", "leg.R")}
for v in mesh.data.vertices:
    p = v.co
    w = {}
    for side, sh, hd, hip in (("L", SH_L, HAND_L, HIP_L), ("R", mirror(SH_L), mirror(HAND_L), mirror(HIP_L))):
        d, t = seg_dist(p, sh, hd)
        # 팔: 팔 뼈 가까이 + 몸 앞쪽으로 튀어나온 부분, 어깨 쪽은 몸과 섞이게
        if p.y < -0.22:
            wa = smooth(0.10, 0.16, d) * (0.35 + 0.65 * smooth(0.0, 0.35, 1 - t))
            if wa > 0: w["arm." + side] = wa
        # 다리: 발 부분(바닥 근처), 위로 갈수록 몸과 섞이게
        dx = math.hypot(p.x - hip.x, p.y - hip.y)
        wl = smooth(0.13, 0.17, dx) * smooth(0.09, 0.15, p.z)
        if wl > 0: w["leg." + side] = wl
    rest = max(0.0, 1.0 - sum(w.values()))
    w["body"] = rest
    tot = sum(w.values()) or 1.0
    for n, val in w.items():
        if val > 1e-4: groups[n].add([v.index], val / tot, 'REPLACE')

mesh.parent = rig
mod = mesh.modifiers.new("Armature", 'ARMATURE'); mod.object = rig

# ---------- 3. 동작 ----------
rig.animation_data_create()
for pb in rig.pose.bones:
    pb.rotation_mode = 'XYZ'

def make_action(name, frames, keys):
    """keys: {frame: {bone: {'rot':(x,y,z deg), 'loc':(..), 'scale':(..)}}} 마지막 프레임 = 첫 프레임"""
    act = bpy.data.actions.new(name); act.use_fake_user = True
    rig.animation_data.action = act
    for f in sorted(keys):
        scene.frame_set(f)
        for pb in rig.pose.bones:
            k = keys[f].get(pb.name, {})
            pb.rotation_euler = [math.radians(a) for a in k.get('rot', (0, 0, 0))]
            pb.location = k.get('loc', (0, 0, 0))
            pb.scale = k.get('scale', (1, 1, 1))
            pb.keyframe_insert("rotation_euler", frame=f)
            pb.keyframe_insert("location", frame=f)
            pb.keyframe_insert("scale", frame=f)
    act.frame_range = (0, frames)
    try:
        act.use_frame_range = True
        act.use_cyclic = True
    except AttributeError:
        pass
    return act

# Idle: 2초(48프레임). 숨 쉬듯 몸이 살짝 부풀었다 돌아오고 팔이 살짝 흔들림
idle = {}
for f in range(0, 49, 6):
    s = math.sin(2 * math.pi * f / 48)
    idle[f] = {
        "body": {"scale": (1 + 0.012 * s, 1 + 0.03 * s, 1 + 0.012 * s)},  # 뼈 Y축 = 위쪽
        "arm.L": {"rot": (0, 0, 5 * s)},
        "arm.R": {"rot": (0, 0, -5 * s)},
    }
make_action("Idle", 48, idle)

# Walk: 1초(24프레임). 다리를 앞뒤로 번갈아 들고, 몸은 좌우로 뒤뚱, 걸음마다 살짝 통통
walk = {}
for f in range(0, 25, 3):
    ph = 2 * math.pi * f / 24
    s, c2 = math.sin(ph), math.cos(2 * ph)
    walk[f] = {
        "leg.L": {"rot": (28 * s, 0, 0)},
        "leg.R": {"rot": (-28 * s, 0, 0)},
        "body": {"rot": (0, 0, 6 * s), "loc": (0, 0.02 * (1 - c2) / 2, 0)},  # 좌우로 기우뚱(뼈 Z축), 위아래 통통(뼈 Y축)
        "arm.L": {"rot": (-18 * s, 0, 0)},
        "arm.R": {"rot": (18 * s, 0, 0)},
    }
make_action("Walk", 24, walk)

# 기본으로는 Idle을 보이게
rig.animation_data.action = bpy.data.actions["Idle"]
scene.frame_start, scene.frame_end = 0, 48
scene.frame_set(0)

bpy.context.view_layer.objects.active = rig
bpy.ops.wm.save_mainfile()
print("완료: 뼈대 6개(root 포함), 동작 Idle(48프레임)·Walk(24프레임)")
