using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixLODTransitions : EditorWindow
{
    private float lod0Percentage = 0.6f;
    private float lod1Percentage = 0.15f;
    private bool fixPrefabs = true;
    private bool fixSceneObjects = true;

    [MenuItem("Tools/Fix LOD Transitions")]
    public static void ShowWindow()
    {
        GetWindow<FixLODTransitions>("Fix LOD Transitions");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Tree LOD Transitions", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Current issue: LOD transitions are set incorrectly.\n" +
            "LOD0 (3D model) should transition to LOD1 (billboard) at a reasonable distance.",
            MessageType.Warning);

        EditorGUILayout.Space();

        lod0Percentage = EditorGUILayout.Slider("LOD0 ends at", lod0Percentage, 0.1f, 0.9f);
        EditorGUILayout.LabelField($"  (3D model visible until {lod0Percentage * 100:F0}% screen height)");
        
        EditorGUILayout.Space();
        
        lod1Percentage = EditorGUILayout.Slider("LOD1 ends at", lod1Percentage, 0.01f, 0.5f);
        EditorGUILayout.LabelField($"  (Billboard visible from {lod0Percentage * 100:F0}% to {lod1Percentage * 100:F0}%)");

        EditorGUILayout.Space();

        fixPrefabs = EditorGUILayout.Toggle("Fix Tree Prefabs", fixPrefabs);
        fixSceneObjects = EditorGUILayout.Toggle("Fix Scene Trees", fixSceneObjects);

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply LOD Fixes"))
        {
            ApplyFixes();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Recommended settings:\n" +
            "LOD0: 0.5-0.7 (billboard starts when tree is medium distance)\n" +
            "LOD1: 0.1-0.2 (billboard visible until tree is far away)",
            MessageType.Info);
    }

    private void ApplyFixes()
    {
        int fixedCount = 0;

        if (fixPrefabs)
        {
            fixedCount += FixTreePrefabs();
        }

        if (fixSceneObjects)
        {
            fixedCount += FixSceneTrees();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            $"Fixed LOD transitions on {fixedCount} objects!", 
            "OK");
    }

    private int FixTreePrefabs()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Autumn.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_AutumnLight.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Summer.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Winter.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Winter.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn_Small.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight_Small.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer_Small.prefab",
            "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Dead_Tree.prefab"
        };

        int count = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                LODGroup lodGroup = prefabInstance.GetComponent<LODGroup>();

                if (lodGroup != null && FixLODGroup(lodGroup))
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                    count++;
                    Debug.Log($"Fixed LOD on prefab: {prefab.name}");
                }

                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        return count;
    }

    private int FixSceneTrees()
    {
        LODGroup[] allLODGroups = FindObjectsOfType<LODGroup>();
        int count = 0;

        foreach (LODGroup lodGroup in allLODGroups)
        {
            if (lodGroup.name.Contains("Tree") || lodGroup.name.Contains("tree"))
            {
                if (FixLODGroup(lodGroup))
                {
                    EditorUtility.SetDirty(lodGroup);
                    count++;
                }
            }
        }

        return count;
    }

    private bool FixLODGroup(LODGroup lodGroup)
    {
        LOD[] lods = lodGroup.GetLODs();

        if (lods.Length < 2)
        {
            return false;
        }

        bool changed = false;

        if (Mathf.Abs(lods[0].screenRelativeTransitionHeight - lod0Percentage) > 0.01f)
        {
            lods[0].screenRelativeTransitionHeight = lod0Percentage;
            changed = true;
        }

        if (Mathf.Abs(lods[1].screenRelativeTransitionHeight - lod1Percentage) > 0.01f)
        {
            lods[1].screenRelativeTransitionHeight = lod1Percentage;
            changed = true;
        }

        if (changed)
        {
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        return changed;
    }

    [MenuItem("Tools/Fix LOD Transitions/Quick Fix (Default Settings)")]
    public static void QuickFix()
    {
        FixLODTransitions window = CreateInstance<FixLODTransitions>();
        window.lod0Percentage = 0.6f;
        window.lod1Percentage = 0.15f;
        window.fixPrefabs = true;
        window.fixSceneObjects = true;
        window.ApplyFixes();
    }
}
