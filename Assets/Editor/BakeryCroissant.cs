// 메뉴: Bakery > 크루아상 비교 장면 만들기
// 하는 일
//  1) After 크루아상: 노멀맵 종류 지정, 재질 꺼내기 + 광택 0.2, 동작 이름 Idle/Walk + 반복 재생, Animator Controller 만들기
//  2) Before 크루아상(원본): 텍스처를 Before/Croissant_textures 로 꺼내 자기 텍스처를 정확히 연결 (이름 겹침 방지)
//  3) Compare_Croissant 장면: 수정 전(정지) / 수정 후 Idle / 수정 후 Walk 를 나란히 배치
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BakeryCroissant
{
    static string FindModel(string folder, string nameContains)
    {
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileName(p).ToLower().Contains(nameContains.ToLower())) return p;
        }
        return null;
    }

    static Texture2D FindTex(string folder, string nameContains)
    {
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p).ToLower().Contains(nameContains)) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }

    static void MakeNormal(Texture2D t)
    {
        if (t == null) return;
        var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));
        if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
    }

    // FBX 안의 재질을 밖으로 꺼내 지정한 텍스처를 연결
    static Material ExternalMaterial(string fbxPath, string matPath, Texture2D baseTex, Texture2D normTex, float? smooth)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        Material src = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().FirstOrDefault();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        if (baseTex) { mat.SetTexture("_BaseMap", baseTex); mat.SetTexture("_MainTex", baseTex); }
        if (normTex) { mat.SetTexture("_BumpMap", normTex); mat.EnableKeyword("_NORMALMAP"); }
        if (smooth.HasValue) mat.SetFloat("_Smoothness", smooth.Value);
        EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src != null ? src.name : "croissant_mat"), mat);
        imp.SaveAndReimport();
        return mat;
    }

    static AnimatorController Controller(string path, params AnimationClip[] clips)
    {
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = c.layers[0].stateMachine;
        AnimatorState first = null;
        foreach (var clip in clips)
        {
            var s = sm.AddState(clip.name); s.motion = clip;
            if (first == null) first = s;
        }
        sm.defaultState = first;
        if (clips.Length == 2)  // Idle <-> Walk 전환 (Walking 체크로 전환)
        {
            c.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var idle = sm.states[0].state; var walk = sm.states[1].state;
            var t1 = idle.AddTransition(walk); t1.AddCondition(AnimatorConditionMode.If, 0, "Walking"); t1.hasExitTime = false; t1.duration = 0.15f;
            var t2 = walk.AddTransition(idle); t2.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking"); t2.hasExitTime = false; t2.duration = 0.15f;
        }
        return c;
    }

    [MenuItem("Bakery/크루아상 비교 장면 만들기")]
    static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ---------- After ----------
        string after = FindModel("Assets/After", "croissant");
        if (after == null) { EditorUtility.DisplayDialog("Bakery", "After 폴더에 croissant.fbx 가 없습니다. 먼저 끌어다 넣어 주세요.", "확인"); return; }
        string afterDir = Path.GetDirectoryName(after).Replace('\\', '/');
        var aBase = FindTex(afterDir, "croissant_base_color");
        var aNorm = FindTex(afterDir, "croissant_normal");
        MakeNormal(aNorm);

        var imp = (ModelImporter)AssetImporter.GetAtPath(after);
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.importAnimation = true;
        imp.SaveAndReimport();
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            if (c.name.Contains("Idle")) c.name = "Idle";
            else if (c.name.Contains("Walk")) c.name = "Walk";
            c.loopTime = true;
            c.lastFrame = Mathf.Max(c.firstFrame + 1, c.lastFrame - 1);  // 마지막 장면 = 첫 장면이라 하나 빼서 이음새 매끄럽게
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();

        ExternalMaterial(after, afterDir + "/croissant_mat.mat", aBase, aNorm, 0.2f);

        var all = AssetDatabase.LoadAllAssetsAtPath(after).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
        var idleClip = all.FirstOrDefault(c => c.name == "Idle");
        var walkClip = all.FirstOrDefault(c => c.name == "Walk");
        if (idleClip == null || walkClip == null) { Debug.LogError("[Bakery] Idle/Walk 동작을 찾지 못함: " + string.Join(", ", all.Select(c => c.name))); return; }
        var mainCtrl = Controller(afterDir + "/croissant_anim.controller", idleClip, walkClip);
        var walkCtrl = Controller(afterDir + "/croissant_walk_preview.controller", walkClip);

        // ---------- Before ----------
        string before = FindModel("Assets/Before", "croissant");
        GameObject beforeAsset = null;
        if (before != null)
        {
            const string dir = "Assets/Before/Croissant_textures";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Before", "Croissant_textures");
            var bimp = (ModelImporter)AssetImporter.GetAtPath(before);
            bimp.ExtractTextures(dir); AssetDatabase.Refresh();
            var bBase = FindTex(dir, "base_color"); var bNorm = FindTex(dir, "normal");
            MakeNormal(bNorm);
            ExternalMaterial(before, dir + "/croissant_original_mat.mat", bBase, bNorm, null);
            beforeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(before);
        }
        else Debug.LogWarning("[Bakery] Before 폴더에 원본 크루아상 FBX가 없어 수정 후 모델만 배치합니다.");

        // ---------- 장면 ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Floor";
        var afterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(after);

        if (beforeAsset != null)
        {
            var b = (GameObject)PrefabUtility.InstantiatePrefab(beforeAsset); b.name = "Before_Croissant";
            b.transform.position = new Vector3(-2.2f, 0, 0);
        }
        var ai = (GameObject)PrefabUtility.InstantiatePrefab(afterAsset); ai.name = "After_croissant_Idle";
        ai.transform.position = Vector3.zero;
        var anim1 = ai.GetComponent<Animator>(); if (anim1 == null) anim1 = ai.AddComponent<Animator>(); anim1.runtimeAnimatorController = mainCtrl;

        var aw = (GameObject)PrefabUtility.InstantiatePrefab(afterAsset); aw.name = "After_croissant_Walk";
        aw.transform.position = new Vector3(2.2f, 0, 0);
        var anim2 = aw.GetComponent<Animator>(); if (anim2 == null) anim2 = aw.AddComponent<Animator>(); anim2.runtimeAnimatorController = walkCtrl;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Compare_Croissant.unity");
        Selection.activeGameObject = ai;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("[Bakery] 크루아상 완료: 동작 " + string.Join(", ", all.Select(c => c.name)));
        EditorUtility.DisplayDialog("Bakery", "크루아상 비교 장면을 만들었습니다.\n위쪽 ▶(Play)를 누르면 움직임을 볼 수 있습니다.", "확인");
    }
}
