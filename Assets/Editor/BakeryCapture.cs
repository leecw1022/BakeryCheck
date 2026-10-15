// 메뉴: Bakery > 비교 사진 찍기 (앞모습, 같은 조명)
// 세 비교 장면 + 대표 이미지를 같은 조명·같은 방식의 카메라로 찍어서
// 바탕화면\Bakery3D\records\unity_photos 폴더에 PNG로 저장합니다. 장면 파일은 바꾸지 않습니다.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BakeryCapture
{
    const int W = 1920, H = 1080;
    static readonly Quaternion SunRot = Quaternion.Euler(45f, 150f, 0f); // 앞쪽 위에서 비스듬히 비추는 같은 조명

    static string OutDir()
    {
        var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Bakery3D", "records", "unity_photos");
        Directory.CreateDirectory(d);
        return d;
    }

    static Bounds ModelBounds()
    {
        // 열린 장면의 최상위 오브젝트에서 바닥·카메라·조명을 뺀 모든 모델의 크기 합치기
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        var rs = roots.Where(g => g.name != "Floor" && g.name != "Plane" && !g.name.StartsWith("__"))
                      .SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToList();
        if (rs.Count == 0) throw new Exception("장면에서 모델을 찾지 못했습니다: " + string.Join(", ", roots.Select(g => g.name)));
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void SameLight()
    {
        foreach (var l in EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)))
            if (l.type == LightType.Directional) { l.transform.rotation = SunRot; l.intensity = 1.2f; l.shadows = LightShadows.Soft; }
    }

    // 얼굴(+Z) 쪽 정면에서, 살짝 위에서 내려다보게
    static void Shoot(string fileName)
    {
        SameLight();
        var b = ModelBounds();
        var go = new GameObject("__CaptureCam");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 25f;
        cam.clearFlags = CameraClearFlags.Skybox;
        float aspect = (float)W / H;
        float vHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * aspect);
        float dist = Mathf.Max(b.extents.x / Mathf.Tan(hHalf), b.extents.y / Mathf.Tan(vHalf)) * 1.25f + b.extents.z;
        var target = b.center;
        var dir = new Vector3(0f, 0.28f, 1f).normalized;           // +Z 정면, 약간 위
        go.transform.position = target + dir * dist;
        go.transform.LookAt(target);
        cam.nearClipPlane = 0.05f; cam.farClipPlane = dist * 10f;

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        var path = Path.Combine(OutDir(), fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
        Debug.Log("[Bakery] 사진 저장: " + path);
    }

    static AnimationClip Clip(string name)
    {
        var p = AssetDatabase.FindAssets("croissant t:Model", new[] { "Assets/After" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        return p == null ? null : AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
    }

    static GameObject Place(string folder, string nameContains, Vector3 pos)
    {
        var p = AssetDatabase.FindAssets("t:Model", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(x => Path.GetFileName(x).ToLower().Contains(nameContains));
        if (p == null) return null;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
        g.transform.position = pos;
        return g;
    }

    [MenuItem("Bakery/비교 사진 찍기 (앞모습, 같은 조명)")]
    static void CaptureAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var current = EditorSceneManager.GetActiveScene().path;
        int n = 0;
        OutDir();

        foreach (var (scene, file) in new[] {
            ("Assets/Scenes/Compare_Anpan.unity", "compare_anpan.png"),
            ("Assets/Scenes/Compare_Swissroll.unity", "compare_swissroll.png"),
            ("Assets/Scenes/Compare_Croissant.unity", "compare_croissant.png") })
        {
            if (!File.Exists(scene)) { Debug.LogWarning("[Bakery] 장면 없음: " + scene); continue; }
            var sc = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            // 단팥빵 장면에 모델이 저장되어 있지 않으면 다시 배치하고 저장 (수정 전 -0.8, 수정 후 +0.8)
            if (scene.Contains("Anpan") && !sc.GetRootGameObjects().Any(g => g.GetComponentsInChildren<Renderer>(true).Length > 0 && g.name != "Plane" && g.name != "Floor"))
            {
                var b0 = Place("Assets/Before", "anpan", new Vector3(-0.8f, 0f, 0f));
                var a0 = Place("Assets/After", "anpan", new Vector3(0.8f, 0f, 0f));
                if (b0) b0.name = "Before_Anpan";
                if (a0) a0.name = "After_anpan";
                EditorSceneManager.SaveScene(sc);
                Debug.Log("[Bakery] Compare_Anpan 장면에 단팥빵 2개를 다시 배치하고 저장했습니다.");
            }
            if (scene.Contains("Croissant"))
            {
                var walk = GameObject.Find("After_croissant_Walk"); var wc = Clip("Walk");
                if (walk && wc) wc.SampleAnimation(walk, wc.length * 0.25f);   // 한 발 내딛은 순간
            }
            try { Shoot(file); n++; }
            catch (Exception e) { Debug.LogError("[Bakery] " + scene + " 촬영 실패: " + e); }
        }

        // 대표 이미지: 수정 후 모델 3개
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Floor";
        Place("Assets/After", "croissant", new Vector3(0f, 0f, 0f));
        Place("Assets/After", "anpan", new Vector3(-1.35f, 0f, 0.55f));
        Place("Assets/After", "swissroll", new Vector3(1.35f, 0f, 0.55f));
        try { Shoot("hero_bakery_models.png"); n++; }
        catch (Exception e) { Debug.LogError("[Bakery] 대표 이미지 촬영 실패: " + e); }

        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        EditorUtility.RevealInFinder(Path.Combine(OutDir(), "hero_bakery_models.png"));
        EditorUtility.DisplayDialog("Bakery", $"사진 {n}장을 저장했습니다.\n바탕화면\\Bakery3D\\records\\unity_photos", "확인");
    }
}
