// Bakery3D 포트폴리오용 Unity 편집기 도우미
// 메뉴: Bakery > 롤케이크 비교 장면 만들기
// 하는 일
//  1) After 재질(swissroll_mat, anpan_mat)의 광택(Smoothness)을 0.2로 맞춤
//  2) Before 롤케이크(원본 FBX)에 들어 있는 텍스처를 Before/SwissRoll_textures 폴더로 꺼내고, 노멀맵 종류 지정
//  3) 새 장면 Compare_Swissroll에 바닥판 + 수정 전/후 롤케이크를 나란히 배치하고 저장
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BakerySetup
{
    const string BeforeRoll = "Assets/Before/Swiss Roll Cake-c9c99d9feb4f3a4d63eeb3d7748b31ec.fbx";
    const string AfterRoll = "Assets/After/swissroll/swissroll.fbx";
    const string BeforeTexDir = "Assets/Before/SwissRoll_textures";

    [MenuItem("Bakery/롤케이크 비교 장면 만들기")]
    static void SetupSwissroll()
    {
        // 열려 있는 장면에 저장 안 된 변경이 있으면 먼저 저장할지 묻기
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1) 광택 0.2
        foreach (var p in new[] { "Assets/After/swissroll_mat.mat", "Assets/After/anpan_mat.mat" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { Debug.LogWarning("[Bakery] 재질 없음: " + p); continue; }
            m.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(m);
            Debug.Log("[Bakery] Smoothness 0.2 적용: " + p);
        }
        AssetDatabase.SaveAssets();

        // 2) Before 롤케이크 텍스처 꺼내기 + 노멀맵 지정
        var imp = AssetImporter.GetAtPath(BeforeRoll) as ModelImporter;
        if (imp == null) { Debug.LogError("[Bakery] Before 롤케이크 FBX를 찾을 수 없음: " + BeforeRoll); return; }
        if (!AssetDatabase.IsValidFolder(BeforeTexDir))
            AssetDatabase.CreateFolder("Assets/Before", "SwissRoll_textures");
        bool extracted = imp.ExtractTextures(BeforeTexDir);
        AssetDatabase.Refresh();
        Debug.Log("[Bakery] Before 텍스처 꺼내기: " + (extracted ? "완료" : "꺼낼 텍스처 없음(이미 꺼냈을 수 있음)"));

        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { BeforeTexDir }))
        {
            var tp = AssetDatabase.GUIDToAssetPath(g);
            if (!Path.GetFileNameWithoutExtension(tp).ToLower().Contains("normal")) continue;
            var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
                Debug.Log("[Bakery] 노멀맵으로 지정: " + tp);
            }
        }
        imp.SaveAndReimport();

        // 3) 비교 장면 만들기
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Floor";
        plane.transform.position = Vector3.zero;

        var beforeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BeforeRoll);
        var afterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AfterRoll);
        if (beforeAsset == null || afterAsset == null)
        {
            Debug.LogError("[Bakery] 롤케이크 FBX를 불러오지 못함");
            return;
        }
        var b = (GameObject)PrefabUtility.InstantiatePrefab(beforeAsset);
        b.name = "Before_SwissRoll";
        b.transform.position = new Vector3(-0.8f, 0f, 0f);

        var a = (GameObject)PrefabUtility.InstantiatePrefab(afterAsset);
        a.name = "After_swissroll";
        a.transform.position = new Vector3(0.8f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Compare_Swissroll.unity");
        Selection.activeGameObject = a;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("[Bakery] 완료: Assets/Scenes/Compare_Swissroll.unity");
        EditorUtility.DisplayDialog("Bakery", "롤케이크 비교 장면을 만들었습니다.\nAssets/Scenes/Compare_Swissroll", "확인");
    }

    // 비포 롤케이크에 단팥빵 텍스처가 잘못 붙은 문제 고치기
    // 원인: 원본 FBX 두 개의 텍스처 이름이 똑같이 base_color / normal 이라,
    //       Unity가 롤케이크 재질에 Before 폴더의 단팥빵 base_color.png 를 연결함
    [MenuItem("Bakery/비포 롤케이크 텍스처 바로잡기")]
    static void FixBeforeSwissrollTextures()
    {
        var imp = AssetImporter.GetAtPath(BeforeRoll) as ModelImporter;
        var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeforeTexDir + "/base_color.png");
        var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeforeTexDir + "/normal.png");
        if (imp == null || baseTex == null || normTex == null)
        {
            Debug.LogError("[Bakery] 롤케이크 FBX나 SwissRoll_textures 안의 텍스처를 찾을 수 없음");
            return;
        }
        // FBX 안의 원래 재질을 복사해 따로 저장 (원본 값 유지, 텍스처만 정확히 연결)
        Material src = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(BeforeRoll))
            if (o is Material mm) { src = mm; break; }
        var matPath = BeforeTexDir + "/swissroll_original_mat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", baseTex);
        mat.SetTexture("_MainTex", baseTex);
        mat.SetTexture("_BumpMap", normTex);
        mat.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // FBX의 재질 칸에 이 재질을 연결
        string srcName = src != null ? src.name : "material_0";
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), srcName), mat);
        imp.SaveAndReimport();
        Debug.Log("[Bakery] 비포 롤케이크 재질 연결 완료: " + matPath + " (원래 재질 이름: " + srcName + ")");
        EditorUtility.DisplayDialog("Bakery", "비포 롤케이크 텍스처를 바로잡았습니다.", "확인");
    }
}
