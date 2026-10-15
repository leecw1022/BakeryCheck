# Blender 편집 기록

## 편집 요청 1. 단팥빵 – 갈라진 점 합치기 (Merge by Distance)

- **요청 내용**: 텍스처 경계를 따라 갈라진 점을 하나로 합쳐 줘.
- **원래 문제**: VARCO 3D에서 가져온 단팥빵은 텍스처 경계마다 점이 둘로 나뉘어 314조각처럼 분리된 상태였음. 이대로 면 수를 줄이면 경계마다 틈이 생김 (같은 방식의 롤케이크 시험에서 틈 6,484개 확인).
- **수행한 작업** (Blender 5.2.2, 채원 직접 실행): `anpan_work.blend` → texture_mapping 선택 → Tab(편집 모드) → A(전체 선택) → M → By Distance → Merge Distance `0.00001 m`.
- **결과**: 점 267,261 → **250,002** (17,259개 병합), 면 500,000 그대로, 모양과 텍스처 변화 없음.
- **확인 사진**: `anpan_01_before_import_stats.png` (수정 전), `anpan_02_merge_by_distance.png` (수정 후)

## 편집 요청 2. 단팥빵 – 면 수 줄이기 (Decimate)

- **요청 내용**: 둥근 모양과 참깨 토핑을 유지하면서 단팥빵을 게임용으로 가볍게 만들어 줘.
- **원래 문제**: 단팥빵 면 수 500,000개로 다른 두 모델(50,000개)의 10배. 게임 아이템으로는 과도하게 무거움.
- **비교 근거** (AI 사전 시험, 클라우드 Blender 엔진 렌더): 20,000 / 10,000은 원본과 거의 구분 안 됨, 5,000은 윗면 텍스처 번짐, 2,500은 외곽선 울퉁불퉁 → 10,000 추천 (`anpan_decimate_compare.png`)
- **수행한 작업** (채원 직접 실행): Modifiers → Add Modifier → Generate → Decimate (Collapse), Ratio `0.02`
- **중간 결과**: Face Count 10,000, 화면상 둥근 외곽과 참깨 형태 유지 확인 (`anpan_03_decimate_preview.png`)
- **Material Preview 비교**: 모디파이어 켜고 끈 두 화면에서 외곽선·참깨 동일, 윗면 일부 껍질 무늬가 아주 약간 늘어난 정도 (`anpan_04_compare_texture_A/B.png`)
- **적용(Apply)** (채원 직접 실행): Decimate → v → Apply → Ctrl+S
- **결과**: 면 500,000 → **10,000** (98% 감소), 점 250,002 → 5,002. 적용 후 모디파이어 목록 비어 있음, 파일 저장 확인 (`anpan_05_decimate_applied.png`)

## 편집 요청 3. 단팥빵 – 게임 크기로 맞추고 크기 확정 (Scale + Apply Scale)

- **요청 내용**: 단팥빵을 정한 게임 크기(지름 0.5m)로 맞추고, 바닥 중앙 원점은 유지해 줘.
- **원래 문제**: VARCO 3D가 모든 모델을 가장 긴 쪽 약 1m로 생성해서 단팥빵 지름이 0.9995m로 목표의 두 배였음.
- **수행한 작업** (채원 직접 실행): N 패널 → Scale X/Y/Z `0.5` 입력 → 3D 화면에서 Ctrl+A → Scale
- **결과**: Dimensions 0.5 × 0.499 × 0.22m, Scale 0.500 → **1.000**으로 확정, Location 0/0/0 유지(원점 = 바닥 중앙), 회전 0°, 면 10,000 유지
- **확인 사진**: `anpan_06_scale_before_apply.png` (확정 전, Scale 0.5), `anpan_07_scale_applied.png` (확정 후, Scale 1.0)

## 내보내기 – 단팥빵 FBX (Unity용)

- **방법**: `scripts/export_for_unity.py`를 Blender Scripting 탭에서 실행 (AI가 작성하고 클라우드에서 같은 조건으로 사전 시험, 채원 직접 실행)
- **설정**: 선택한 메시만, Apply Scalings = FBX All, Forward −Z / Up Y, Apply Transform(축 변환 굽기) 켬, 애니메이션 끔, 텍스처는 별도 PNG(상대 경로)
- **결과물**: `02_export/anpan/anpan.fbx` (0.68MB, 원본 57.1MB), `textures/anpan_base_color.png` (4096²), `textures/anpan_normal.png` (2048²)
- **AI 검증**: 내보낸 FBX를 다시 열어 면 10,000, 크기 0.5 × 0.22(높이) × 0.499m, 배율 1, 재질 `anpan_mat`, 텍스처 경로 `textures\anpan_*.png` 확인
- **확인 사진**: `anpan_08_export_script.png`

## Unity 확인 중 발견한 문제 – 텍스처가 조각조각 얼룩져 보임

- **현상**: Unity 6000.6.0(URP)에서 단팥빵 표면이 텍스처 조각 단위로 밝고 어둡게 얼룩져 보임. Blender에서는 정상 (`anpan_10_unity_problem.png`)
- **원인 확인 1 (AI)**: 채원 PC의 내보낸 FBX와 텍스처를 그대로 가져와 Blender 엔진으로 렌더링 → 6방향 모두 정상, 텍스처도 원본과 바이트 단위로 동일 → 모델 파일 문제가 아니라 Unity 재질 설정 문제로 판단 (`anpan_09_export_check_blender.png`)
- **원인 확인 2 (채원 실행)**: After 재질에서 Normal Map을 비우자 얼룩이 사라짐. 노멀맵을 넣은 Before는 같은 얼룩이 나타남 (`anpan_11_unity_normal_removed.png`)
- **노멀맵 분석 (AI)**: 단팥빵 노멀맵의 R·G 채널 표준편차가 255 중 2 → 거의 평평한(정보가 없는) 노멀맵. 평평한 노멀맵이 얼룩을 만든다는 것은 Unity가 이 그림을 노멀맵이 아닌 일반 색 그림(sRGB)으로 읽고 있다는 뜻
- **조치 (채원 실행)**: Before의 추출된 normal 텍스처 → Texture Type을 Normal map으로 지정 → Apply. After는 정보가 거의 없는 노멀맵을 재질에서 제외
- **결과**: 두 빵 모두 얼룩이 사라지고 구운 색·참깨가 정상 표시 (`anpan_12_unity_fixed.png`)
- **교훈**: 노멀맵은 Unity에서 반드시 Texture Type = Normal map으로 지정해야 함. 롤케이크·크루아상에도 같은 설정 적용 예정

## Unity 확인 – 단팥빵 크기·방향·바닥 접촉

- **확인 (채원 실행, AI 판독)**: anpan Transform = Rotation 0/0/0, Scale 1/1/1 (회전 보정값 없이 들어옴), Position Y 0
- **바닥 접촉**: Iso 측면 시점에서 수정 전·후 빵 모두 바닥판에 붙어 있음 (떠 있거나 묻힌 부분 없음)
- **크기**: 수정 후 빵이 수정 전의 절반 (지름 1m → 0.5m)으로 표시
- **확인 사진**: `anpan_13_unity_transform_side.png`
- **발견**: 원근 측면 시점에서 빵 옆면에 하늘색 반사가 비침 → ③ 광택 조정 대상

## 색감·질감 – 단팥빵 광택 줄이기 (Unity 재질)

- **요청 내용**: 구운 색은 살리고 과도한 플라스틱 광택은 줄여 줘.
- **원래 문제**: Unity로 가져온 재질의 Smoothness가 0.5 → 측면에 하늘이 비치고 윗면에 흰 반사광이 생김 (`anpan_14_gloss_before_side.png`, `anpan_15_material_inspector_before.png`)
- **Blender/Unity 구분**: Blender의 Roughness·Specular 값은 FBX로 그대로 옮겨지지 않아, 광택은 Unity 재질에서 정함 (재질 파일은 Unity 프로젝트에 저장 → GitHub에 함께 포함)
- **수행한 작업** (채원 직접 실행): After/anpan FBX → Materials → Extract Materials (After 폴더) → 꺼낸 anpan_mat의 Smoothness 0.5 → `0.2`, Normal Map 비움
- **결과**: 같은 Iso 각도 비교에서 윗면 왼쪽의 흰 반사광이 줄고 껍질이 무광에 가까운 구운 빵 질감으로 바뀜. 색(Base Map)은 그대로 (`anpan_16_smoothness_0.2.png` 수정 후 / `anpan_17_smoothness_0.5.png` 수정 전)

---

# 롤케이크

## 편집 요청 4. 롤케이크 – 갈라진 점 합치기

- **원래 문제**: 단팥빵과 같이 텍스처 경계마다 점이 나뉘어 242조각처럼 분리 (원본 점 29,989)
- **수행한 작업** (채원 직접 실행): `swissroll_work.blend` → Tab → A → M → By Distance (0.00001 m)
- **결과**: 점 29,989 → **25,002**, 면 50,000 유지 (`swissroll_01_merged.png`)
- **면 수 사전 비교 (AI)**: 단면 크림 결 기준 20,000/10,000은 원본과 거의 동일, 5,000은 크림 결 약간 뭉개짐, 2,500은 확실히 흐려짐 → 10,000(20%) 추천 (`swissroll_decimate_compare.png`, `swissroll_decimate_zoom.png`)

## 롤케이크 – 면 줄이기 중 생긴 표면 자국 발견

- **현상**: Ratio 0.2(10,000면) 적용 후 옆면 가운데에 작은 검은 자국 (`swissroll_03_ratio0.2_applied_with_mark.png`, 크기 0.276 × 0.797 × 0.247m, Scale 1 확정 상태)
- **원인 확인 (AI)**: 원본(50,000면)과 0.2 결과를 색 없는 회색 재질로 렌더링 → 원본에는 없고 0.2에서만 생김 (`swissroll_02_decimate_artifact_check.png` 아래 줄: 왼쪽 원본, 오른쪽 0.2). 인접한 두 삼각형이 서로 반대로 접힌 곳(접힌 모서리)을 계산해 보니 옆면 좌표 x≈−0.165, y≈−0.17, z≈0.17에 새로 생김 → 면 줄이기 과정에서 삼각형이 접힌 것
- **비율별 시험 (AI)**: 0.19~0.24는 같은 자리에 접힘이 남고, **0.25(12,500면)부터 옆면 접힘 없음**. 단면 아래쪽 크림 틈의 접힘은 원본에도 있는 모양이라 그대로 둠
- **결정**: 롤케이크는 0.25(12,500면)로 다시 진행 (모든 모델에 같은 비율을 쓰지 않는 근거)

## 편집 요청 5. 롤케이크 – 0.25로 다시 줄이기 + 크기 확정, 남은 자국 국소 수정

- **수행한 작업 (채원 직접 실행)**: 원본 FBX 다시 불러오기 → 점 합치기 → Decimate Ratio `0.25` 적용 → Scale `0.8` 입력 후 Ctrl+A → Scale
- **결과**: 면 50,000 → **12,500**, 크기 0.276 × 0.797 × 0.247m, Scale 1, 모디파이어 없음 (`swissroll_06_ratio0.25_applied.png`)
- **추가 확인 (AI)**: 채원 PC의 `swissroll_work.blend`를 가져와 회색 재질로 렌더링 → 자국은 작아졌지만 같은 자리(x −0.131, y −0.139, z 0.136)에 날카로운 꺾임이 남아 있음 (`swissroll_04_ratio0.25_clay.png`)
- **조치 계획**: 자국 주변 반경 2cm 안의 점(2개)만 부드럽게 펴는 코드 `scripts/fix_swissroll_crease.py` 작성. 클라우드 사전 시험 결과 꺾인 모서리 0개, 면 수·크기 변화 없음, 자국 사라짐 (`swissroll_05_crease_fixed_clay_test.png`)
- **실행**: 처음 안내한 단독 코드는 실행·저장이 확인되지 않아(파일 저장 시각 변화 없음), 다듬기·텍스처 이름 변경·FBX 내보내기·저장을 한 번에 하는 `scripts/finish_swissroll.py`로 통합 → 채원 실행
- **AI 검증**: 내보낸 `02_export/swissroll/swissroll.fbx`를 다시 열어 확인 — 면 12,500, 크기 0.276 × 0.797(길이) × 0.247m, 재질 `swissroll_mat`, 텍스처 `swissroll_base_color`·`swissroll_normal`, **자국 자리 3cm 안 꺾인 모서리 0개** (`swissroll_07_export_check_blender.png`)
- **함께 발견한 점**: 뒤쪽 단면과 −X 옆면의 스펀지 색이 앞쪽·+X 옆면보다 진한 갈색 (AI 생성 시 보이지 않는 면을 추측한 흔적으로 보임) → ③ 색 통일에서 검토

## Unity – 롤케이크 가져오기 (AI가 프로젝트 파일 점검 후 편집기 도구 작성)

- **AI 점검 (BakeryCheck 프로젝트 파일 직접 확인)**: swissroll 재질은 이미 꺼내져 FBX와 연결됨, swissroll_normal은 Normal map(sRGB 꺼짐)으로 지정됨 → 채원이 이미 처리한 부분. 다만 swissroll_mat과 anpan_mat의 Smoothness가 파일상 0.5 (단팥빵의 0.2 변경이 저장되지 않은 것으로 보임)
- **조치**: Unity 연결 도구(unity-mcp)가 PC에서 실행되지 않아 AI가 Unity를 직접 조작할 수 없음 → 남은 작업을 메뉴 한 번으로 처리하는 편집기 코드 `Assets/Editor/BakerySetup.cs` 작성·설치 (광택 0.2, Before 롤케이크 텍스처 꺼내기 및 노멀맵 지정, Compare_Swissroll 장면 생성·배치·저장)
- **실행 (채원)**: Scripting 탭에서 실행 → Outliner에 croissant_rig 생성, 모델 이름 croissant로 변경, Actions 2개(Idle·Walk) 생성, 제목 줄에 저장 표시(*) 없음 → 저장 완료 (`croissant_04_rig_script_run.png`)
- **실행 결과 (채원 실행)**: Compare_Swissroll 장면 생성·배치 정상, 수정 후 롤케이크는 정상 표시. 그러나 **수정 전 롤케이크가 얼룩덜룩한 무늬로 표시** (`swissroll_08_unity_before_wrong_texture.png`)

## Unity 문제 2 – 수정 전 롤케이크에 단팥빵 텍스처가 붙음

- **원인 (AI, 프로젝트 폴더 확인)**: Before 폴더에 단팥빵 원본에서 꺼낸 `base_color.png`(20.2MB)가 있고, 롤케이크 원본 텍스처는 `SwissRoll_textures/base_color.png`(25.1MB)로 따로 꺼내짐. VARCO 3D 원본 FBX들의 텍스처 이름이 똑같아서, Unity가 롤케이크 재질에 이름이 같은 단팥빵 텍스처를 연결함 → 처음 점검 때 예상한 "텍스처 이름 중복" 문제가 실제로 발생한 사례
- **수정 후 모델에는 문제 없음**: 내보낼 때 `swissroll_` / `anpan_` 으로 이름을 구분했기 때문
- **조치**: 편집기 메뉴 `Bakery > 비포 롤케이크 텍스처 바로잡기` 추가 — 원본 재질을 복사해 롤케이크 자기 텍스처를 명시적으로 연결하고 FBX 재질 칸에 지정 (원본 광택 값은 비교를 위해 그대로 둠)
- **진행 중 문제**: 처음 수정한 편집기 코드가 PC에 덮어써지지 않아 메뉴가 안 보임 → AI가 파일 크기(4.3KB → 6.5KB)로 확인 후 다시 설치
- **결과 (채원 실행)**: 수정 전·후 롤케이크 모두 자기 텍스처(구운 스펀지 + 크림 단면)로 정상 표시. 수정 후 롤케이크가 수정 전의 0.8배 크기, 두 모델 모두 바닥에 놓임 (`swissroll_09_unity_fixed_compare.png`)
- **교훈**: AI 생성 원본 FBX를 여러 개 한 프로젝트에 넣을 때는 텍스처 이름이 겹치는지 먼저 확인할 것. 수정본은 내보낼 때 모델 이름을 앞에 붙여 이 문제를 예방함

---

# 크루아상 캐릭터

## 크기 결정 (2026-10-15)

- 원본 1.0(가로) × 0.61(앞뒤) × 0.57m(높이), 옆으로 넓은 체형. "키 1m" 결정 유지 → 배율 1.75 (결과 약 1.75 × 1.07 × 1.0m)

## 면 수 사전 비교 (AI)

- 50,000 / 20,000 / 15,000 / 12,500 / 10,000 렌더 비교에서 얼굴·팔다리·결 모두 눈으로 구분 어려움 (`croissant_decimate_compare.png`)
- 접힌 모서리 검사: 원본에도 44곳(빵 결 사이 골짜기, VARCO 3D 생성 상태) 존재. 줄이며 새로 생기는 접힘은 0.2→6곳, 0.25→5곳, 0.3→4곳, 0.4→1곳
- **결정: 0.3 (15,000면)** — 빵보다 조금 많이 남김. 캐릭터는 팔다리를 움직일 예정이라 관절 주변에 면이 더 있어야 접히는 모양이 자연스러움. 남는 접힘은 Blender에서 확인 후 필요하면 국소 수정

## 편집 요청 6. 크루아상 – 점 합치기 · 면 줄이기(0.3) · 키 1m 확정

- **수행한 작업 (채원 직접 실행)**: `croissant_work.blend` → 점 합치기(0.00001) → Decimate 0.3 적용 → Scale 1.75 입력 → 캐릭터 선택 후 Ctrl+A → Scale → 저장
- **진행 중 막힌 점**: 처음 Ctrl+A를 누를 때 캐릭터 선택이 풀려 있어 적용되지 않음 → Outliner에서 다시 선택 후 적용
- **결과**: 면 50,000 → **15,000**, 점 7,492, 크기 1.749 × 1.07 × 1.0m, Scale 1.000, 위치·회전 0 (`croissant_02_scale_applied.png`)
- **AI 검증**: 저장된 파일을 가져와 회색 재질로 앞·뒤·양옆 렌더 → 얼굴·팔·다리·빵 결 모양 유지, 새로 생긴 자국 없음. 접힌 모서리 44곳 = 원본과 같은 수(빵 결 골짜기의 원래 상태) (`croissant_01_clay_check.png`)

## 편집 요청 7. 크루아상 – 뼈대와 반복 동작(Idle·Walk) 만들기

- **원래 문제**: 원본에 뼈대·애니메이션 없음. 팔다리가 짧은 빵 체형이라 사람용 자동 뼈대(Mixamo, VARCO 3D Animate)는 잘 맞지 않을 가능성이 높음 → Blender에서 간단한 뼈대로 직접 제작하기로 결정(채원 선택)
- **AI 작업**: 저장된 모델에서 팔(앞쪽 양옆으로 늘어진 부분, x ±0.47~0.5 / z 0.14~0.42)과 다리(바닥 근처 x ±0.21) 위치를 계산 → `scripts/make_croissant_rig.py` 작성
  - 뼈 6개: root(바닥 기준) / body / arm.L·R / leg.L·R
  - 가중치: 팔·다리 뼈 가까운 부분만 따라 움직이고, 몸과 만나는 곳은 부드럽게 섞이도록 거리 기준으로 계산 (몸 한 덩어리 모델이라 자동 가중치보다 번짐이 적게)
  - Idle 48프레임(2초): 숨쉬듯 몸이 살짝 부풀었다 돌아오고 팔이 ±5° 흔들림
  - Walk 24프레임(1초): 다리 ±28° 번갈아, 몸 좌우 ±6° 기우뚱 + 걸음마다 2cm 통통, 팔은 다리와 반대로 ±18°
  - 두 동작 모두 첫 프레임 = 마지막 프레임 → 반복할 때 끊김 없음
- **사전 시험 (AI)**: 채원 파일 복사본에 실행 → 오류 없음, 두 번 실행해도 중복 생성 없음. Walk 0/6/12/18프레임 앞·옆 렌더에서 다리가 번갈아 앞뒤로 움직이고 몸이 좌우로 기우뚱, 몸통 찌그러짐·찢어짐 없음 (`croissant_03_walk_test_frames.png`)
- **시험 중 고친 점**: 처음 코드는 몸 흔들림 축이 잘못되어 좌우로 기우는 대신 제자리에서 몸을 돌림 → 회전 축을 바꿔 수정
- **실행 (채원)**: Scripting 탭에서 실행 → Outliner에 croissant_rig 생성, 모델 이름 croissant로 변경, Actions 2개(Idle·Walk) 생성, 제목 줄에 저장 표시(*) 없음 → 저장 완료 (`croissant_04_rig_script_run.png`)
- **동작 확인 (채원)**: Layout 화면에서 스페이스바로 재생 → Idle 동작으로 몸이 움직이는 것 확인

## 내보내기 – 크루아상 FBX (뼈대 + 동작 포함)

- **방법**: `scripts/export_croissant.py` (AI 작성·사전 시험, 채원 실행) (`croissant_05_export_script_run.png`)
- **설정**: 뼈대+모델만, 끝 뼈 추가 안 함(Add Leaf Bones 끔), 모든 동작을 각각 따로 담기(All Actions), 축 변환 굽기 끔(뼈대가 있으면 굽기 기능이 불안정), 텍스처 `croissant_` 이름으로 별도 저장
- **AI 검증**: 내보낸 FBX를 다시 열어 확인 — 모델 면 15,000·크기 1.749 × 1.07 × 1.0m, 뼈 6개, 동작 Idle(49프레임)·Walk(25프레임) 포함, 텍스처 `croissant_base_color`·`croissant_normal`

## Unity – 크루아상 준비 (AI가 파일 복사 + 편집기 도구 작성)

- **AI 작업**: 수정 후 크루아상(`croissant.fbx` + 텍스처 2장)을 Unity 프로젝트 `Assets/After/croissant`에 직접 복사, 편집기 메뉴 `Bakery > 크루아상 비교 장면 만들기`(`Assets/Editor/BakeryCroissant.cs`) 설치
  - 노멀맵 종류 지정, 재질 꺼내기 + 광택 0.2 (빵들과 같은 질감)
  - 동작 이름을 `Idle`·`Walk`로 정리, 반복 재생(Loop Time) 켬, 첫 장면과 같은 마지막 장면 1개를 빼서 반복 이음새를 매끄럽게
  - Animator Controller(`croissant_anim`: Idle 기본, Walking 체크 시 Walk로 전환) 생성
  - 원본 크루아상도 텍스처를 `Before/Croissant_textures`로 꺼내 자기 텍스처를 명시적으로 연결 (롤케이크에서 겪은 텍스처 이름 겹침 예방)
  - `Compare_Croissant` 장면: 수정 전(정지) / 수정 후 Idle / 수정 후 Walk 나란히 배치
- **원본 FBX(24.8MB)는 AI가 보낼 수 있는 용량(20MB)을 넘어 채원이 직접 Before 폴더로 끌어다 넣음**
- **결과 (채원 실행)**: Compare_Croissant 장면에 수정 전·Idle·Walk 세 개 배치, 세 모델 모두 자기 텍스처로 정상 표시(이름 겹침 없음). Play 시 Animator에 croissant_anim 연결, 동작 2개(Clip Count 2) 인식, 움직임 재생 확인 (`croissant_06_unity_play.png`)
- **방향 확인**: Unity에서 캐릭터 얼굴이 +Z(파란 화살표) 쪽을 향함 → Unity 캐릭터의 기본 앞 방향과 일치. 기본 카메라가 -Z 쪽에 있어 Game 화면에는 뒷모습이 보임 (모델 문제 아님)

## 결과 검증 – Unity 비교 사진 (같은 조명·앞모습)

- **방법**: 편집기 메뉴 `Bakery > 비교 사진 찍기`(AI 작성)로 세 비교 장면과 대표 이미지를 같은 방향의 조명(45°, 150°), 같은 카메라 규칙(얼굴 쪽 정면에서 약간 위, 화각 25°, 모델 전체가 들어오게 자동 거리)으로 1920×1080 촬영
- **진행 중 문제**: ① 첫 실행에서 모델 찾기 방식 오류로 촬영 실패 → 장면의 최상위 오브젝트를 직접 탐색하도록 수정. ② 단팥빵 장면만 실패 → Console 확인 결과 Compare_Anpan 장면에 빵이 저장되지 않은 상태(카메라·조명·바닥만 있음) → 비어 있으면 수정 전·후 단팥빵을 다시 배치하고 저장하도록 도구 수정
- **결과**: `unity_photos/compare_anpan.png`, `compare_swissroll.png`, `compare_croissant.png`, `hero_bakery_models.png` 4장 (화면 왼쪽 = 수정 후, 오른쪽 = 수정 전. 크루아상은 왼쪽부터 수정 후 Walk / 수정 후 Idle / 수정 전)
- **확인한 개선점**: 단팥빵 지름 1m → 0.5m, 롤케이크 1m → 0.8m, 크루아상 가로 1m(키 0.57m) → 키 1m로 아이템과 캐릭터 비율이 정리됨. 수정 전 모델은 측면에 하늘 반사가 강하고, 수정 후 모델은 무광에 가까운 구운 빵 질감으로 세 모델이 같은 톤
