using UnityEngine;
using UnityEditor;

public class TestBillboardVisibility : EditorWindow
{
    [MenuItem("Tools/Test Billboard Visibility")]
    public static void ShowWindow()
    {
        GetWindow<TestBillboardVisibility>("Billboard Test");
    }

    private void OnGUI()
    {
        GUILayout.Label("Billboard Visibility Tests", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "These tests will help identify why billboards aren't visible.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Make All Billboards Face Camera (Edit Mode Fix)"))
        {
            MakeBillboardsFaceSceneCamera();
        }

        if (GUILayout.Button("2. Temporarily Disable LOD0 (Show Only Billboards)"))
        {
            DisableLOD0();
        }

        if (GUILayout.Button("3. Re-enable LOD0 (Restore Normal)"))
        {
            EnableLOD0();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("4. Check If Textures Are Transparent"))
        {
            CheckTextureAlpha();
        }

        if (GUILayout.Button("5. Set Material to Fully Opaque (Test)"))
        {
            SetMaterialsOpaque();
        }
    }

    private void MakeBillboardsFaceSceneCamera()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            EditorUtility.DisplayDialog("Error", "No active Scene view found!", "OK");
            return;
        }

        Camera sceneCamera = sceneView.camera;
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        int count = 0;

        foreach (Transform t in allTransforms)
        {
            if (t.name == "Billboard")
            {
                Vector3 lookDirection = sceneCamera.transform.position - t.position;
                lookDirection.y = 0;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    t.rotation = Quaternion.LookRotation(lookDirection);
                    EditorUtility.SetDirty(t.gameObject);
                    count++;
                }
            }
        }

        Debug.Log($"Rotated {count} billboards to face Scene camera at {sceneCamera.transform.position}");
        EditorUtility.DisplayDialog("Success", 
            $"Rotated {count} billboards to face Scene camera.\nCheck Scene view now!", 
            "OK");
    }

    private void DisableLOD0()
    {
        LODGroup[] lodGroups = FindObjectsOfType<LODGroup>();
        int count = 0;

        foreach (LODGroup lodGroup in lodGroups)
        {
            Renderer[] renderers = lodGroup.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.name != "Billboard" && !renderer.name.Contains("Billboard"))
                {
                    renderer.enabled = false;
                    count++;
                }
            }
        }

        Debug.Log($"Disabled {count} LOD0 renderers. Only billboards should be visible now.");
        EditorUtility.DisplayDialog("LOD0 Disabled", 
            $"Disabled {count} tree renderers.\nOnly billboards should be visible.\nCheck Scene view!", 
            "OK");
    }

    private void EnableLOD0()
    {
        LODGroup[] lodGroups = FindObjectsOfType<LODGroup>();
        int count = 0;

        foreach (LODGroup lodGroup in lodGroups)
        {
            Renderer[] renderers = lodGroup.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    count++;
                }
            }
        }

        Debug.Log($"Re-enabled {count} renderers.");
        EditorUtility.DisplayDialog("Restored", 
            $"Re-enabled {count} renderers.", 
            "OK");
    }

    private void CheckTextureAlpha()
    {
        string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Materials/Billboards" });
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== BILLBOARD TEXTURE ANALYSIS ===");

        foreach (string guid in texturePaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Billboard.png"))
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    sb.AppendLine($"\n{texture.name}:");
                    sb.AppendLine($"  Size: {texture.width}x{texture.height}");
                    sb.AppendLine($"  Format: {texture.format}");
                    sb.AppendLine($"  Alpha Source: {importer.alphaSource}");
                    sb.AppendLine($"  Readable: {importer.isReadable}");
                }
            }
        }

        string output = sb.ToString();
        Debug.Log(output);
        GUIUtility.systemCopyBuffer = output;

        EditorUtility.DisplayDialog("Texture Analysis", 
            "Results logged to Console and copied to clipboard!", 
            "OK");
    }

    private void SetMaterialsOpaque()
    {
        string[] materialPaths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Billboards" });
        int count = 0;

        foreach (string guid in materialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.name.Contains("Billboard_Material"))
            {
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_Cutoff", 0);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Disabled alpha clipping on {count} billboard materials (test mode)");
        EditorUtility.DisplayDialog("Materials Updated", 
            $"Set {count} materials to fully opaque.\nThis is just a test - billboards may look wrong but should be visible.", 
            "OK");
    }
}
