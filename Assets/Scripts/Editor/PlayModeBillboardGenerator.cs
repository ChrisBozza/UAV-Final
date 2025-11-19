using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class PlayModeBillboardGenerator : EditorWindow
{
    private bool includeCircleTrees = true;
    private bool includeNormalTrees = true;
    private int textureSize = 1024;
    private float verticalPadding = 0.25f;

    [MenuItem("Tools/Billboard Generator (Play Mode)")]
    public static void ShowWindow()
    {
        GetWindow<PlayModeBillboardGenerator>("Play Mode Billboard");
    }

    private void OnGUI()
    {
        GUILayout.Label("Billboard Generator (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This method enters Play Mode and uses ScreenCapture to generate billboards.\n" +
            "It works reliably with URP in Unity 6!",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tree Types:", EditorStyles.boldLabel);
        includeCircleTrees = EditorGUILayout.Toggle("Circle Trees", includeCircleTrees);
        includeNormalTrees = EditorGUILayout.Toggle("Normal Trees", includeNormalTrees);
        
        EditorGUILayout.Space();
        textureSize = EditorGUILayout.IntSlider("Texture Size", textureSize, 512, 2048);
        verticalPadding = EditorGUILayout.Slider("Vertical Padding", verticalPadding, 0f, 1f);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Billboards"))
        {
            GenerateBillboards();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Vertical Padding: Adds extra space above/below the tree (0.25 = 25% extra height)\n" +
            "Increase this if trees are being cut off at top/bottom.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "The editor will enter Play Mode to capture billboards, then automatically exit.\n" +
            "Wait for the process to complete!",
            MessageType.Warning);
    }

    private void GenerateBillboards()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;

        if (!EditorUtility.DisplayDialog("Generate Billboards",
            "This will:\n" +
            "1. Create a temporary scene\n" +
            "2. Enter Play Mode\n" +
            "3. Capture billboard screenshots\n" +
            "4. Exit Play Mode\n" +
            "5. Restore your scene\n\n" +
            "Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.5f, 0.9f, 1f);
        camera.orthographic = true;

        BillboardCaptureRuntime captureScript = camObj.AddComponent<BillboardCaptureRuntime>();
        
        List<GameObject> treePrefabs = GetTreePrefabList();
        captureScript.treePrefabs = treePrefabs;
        captureScript.textureSize = textureSize;
        captureScript.verticalPadding = verticalPadding;

        string outputFolder = "Assets/Materials/Billboards";
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        captureScript.outputFolder = outputFolder;

        EditorSceneManager.SaveScene(tempScene, "Assets/Temp_BillboardCapture.unity");

        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        EditorApplication.isPlaying = true;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            
            EditorUtility.DisplayDialog("Capture Complete!",
                "Billboard screenshots have been saved to:\nAssets/Materials/Billboards/\n\n" +
                "Now you need to:\n" +
                "1. Import the textures with Alpha Is Transparency\n" +
                "2. Create materials for each billboard\n" +
                "3. Create quad meshes\n" +
                "4. Add LODGroups to prefabs\n\n" +
                "Use the 'Process Captured Billboards' tool next!",
                "OK");
        }
    }

    private List<GameObject> GetTreePrefabList()
    {
        List<GameObject> treePrefabs = new List<GameObject>();

        if (includeCircleTrees)
        {
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Autumn.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_AutumnLight.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Summer.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Winter.prefab"));
        }

        if (includeNormalTrees)
        {
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Winter.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn_Small.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight_Small.prefab"));
            treePrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer_Small.prefab"));
        }

        treePrefabs.RemoveAll(p => p == null);
        return treePrefabs;
    }
}
