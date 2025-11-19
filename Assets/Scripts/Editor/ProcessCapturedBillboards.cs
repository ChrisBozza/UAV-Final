using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProcessCapturedBillboards : EditorWindow
{
    private Color backgroundColorToRemove = new Color(0.2f, 0.5f, 0.9f);
    private float alphaThreshold = 0.2f;
    private float billboardScale = 0.7f;

    [MenuItem("Tools/Process Captured Billboards")]
    public static void ShowWindow()
    {
        GetWindow<ProcessCapturedBillboards>("Process Billboards");
    }

    private void OnGUI()
    {
        GUILayout.Label("Process Captured Billboards", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Remove the background color from captured images\n" +
            "2. Create materials with proper alpha clipping\n" +
            "3. Create billboard quad meshes\n" +
            "4. Add LODGroups to prefabs",
            MessageType.Info);

        EditorGUILayout.Space();

        backgroundColorToRemove = EditorGUILayout.ColorField("Background Color to Remove:", backgroundColorToRemove);
        alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0.05f, 0.5f);
        billboardScale = EditorGUILayout.Slider("Billboard Scale", billboardScale, 0.3f, 2.0f);

        EditorGUILayout.Space();

        if (GUILayout.Button("Process All Billboard Images"))
        {
            ProcessAllBillboards();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Make sure the background color matches what you used during capture!\n" +
            "Default is sky blue (0.2, 0.5, 0.9) for good contrast with trees.",
            MessageType.Warning);
    }

    private void ProcessAllBillboards()
    {
        string billboardFolder = "Assets/Materials/Billboards";
        
        if (!Directory.Exists(billboardFolder))
        {
            EditorUtility.DisplayDialog("Error", "Billboards folder doesn't exist!", "OK");
            return;
        }

        string[] pngFiles = Directory.GetFiles(billboardFolder, "*_Billboard.png");
        
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No billboard PNG files found!", "OK");
            return;
        }

        int processedCount = 0;

        foreach (string pngPath in pngFiles)
        {
            string assetPath = pngPath.Replace("\\", "/");
            
            EditorUtility.DisplayProgressBar("Processing Billboards", 
                $"Processing {Path.GetFileName(assetPath)}...", 
                (float)processedCount / pngFiles.Length);

            if (ProcessBillboardImage(assetPath))
            {
                processedCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete!", 
            $"Processed {processedCount} billboard images!\n\n" +
            "Materials, meshes, and LODGroups have been created.", 
            "OK");
    }

    private bool ProcessBillboardImage(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError($"Failed to load texture: {texturePath}");
            return false;
        }

        Color[] pixels = texture.GetPixels();
        bool modified = false;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            
            float colorDiff = Mathf.Abs(pixel.r - backgroundColorToRemove.r) +
                            Mathf.Abs(pixel.g - backgroundColorToRemove.g) +
                            Mathf.Abs(pixel.b - backgroundColorToRemove.b);

            if (colorDiff < alphaThreshold)
            {
                pixels[i] = new Color(pixel.r, pixel.g, pixel.b, 0f);
                modified = true;
            }
        }

        if (modified)
        {
            Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            newTexture.SetPixels(pixels);
            newTexture.Apply();

            byte[] pngData = newTexture.EncodeToPNG();
            File.WriteAllBytes(texturePath, pngData);
            
            DestroyImmediate(newTexture);
            AssetDatabase.ImportAsset(texturePath);
            
            if (importer != null)
            {
                importer.SaveAndReimport();
            }

            Debug.Log($"Removed background from: {Path.GetFileName(texturePath)}");
        }

        if (importer != null)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        string treeName = Path.GetFileNameWithoutExtension(texturePath).Replace("_Billboard", "");
        CreateBillboardAssets(treeName, texturePath);

        return true;
    }

    private void CreateBillboardAssets(string treeName, string texturePath)
    {
        string prefabPath = $"Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/{treeName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"Prefab not found: {prefabPath}");
            return;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        
        Material material = CreateBillboardMaterial(texture, texturePath);
        
        Bounds bounds = CalculatePrefabBounds(prefab);
        Mesh mesh = CreateBillboardMesh(treeName, bounds);
        
        SetupLODGroup(prefabPath, mesh, material);

        Debug.Log($"Created billboard assets for: {treeName}");
    }

    private Material CreateBillboardMaterial(Texture2D texture, string texturePath)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 0);
        material.SetFloat("_AlphaClip", 1);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetFloat("_Cull", 0);
        material.EnableKeyword("_ALPHATEST_ON");
        material.renderQueue = 2450;

        string materialPath = texturePath.Replace(".png", "_Material.mat");
        AssetDatabase.CreateAsset(material, materialPath);
        
        return material;
    }

    private Mesh CreateBillboardMesh(string treeName, Bounds bounds)
    {
        float width = Mathf.Max(bounds.size.x, bounds.size.z) * 0.85f * billboardScale;
        float height = bounds.size.y * billboardScale;
        
        float halfWidth = width / 2f;
        float yOffset = bounds.min.y;

        Mesh mesh = new Mesh();
        mesh.name = $"{treeName}_BillboardMesh";

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfWidth, yOffset, 0),
            new Vector3(halfWidth, yOffset, 0),
            new Vector3(-halfWidth, yOffset + height, 0),
            new Vector3(halfWidth, yOffset + height, 0)
        };

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        Vector3[] normals = new Vector3[4] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        string meshPath = $"Assets/Materials/Billboards/{treeName}_BillboardMesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);

        return mesh;
    }

    private void SetupLODGroup(string prefabPath, Mesh mesh, Material material)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        LODGroup lodGroup = prefabRoot.GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            lodGroup = prefabRoot.AddComponent<LODGroup>();
        }

        Transform existingBillboard = prefabRoot.transform.Find("Billboard");
        if (existingBillboard != null)
        {
            DestroyImmediate(existingBillboard.gameObject);
        }

        GameObject billboard = new GameObject("Billboard");
        billboard.transform.SetParent(prefabRoot.transform);
        billboard.transform.localPosition = Vector3.zero;
        billboard.transform.localRotation = Quaternion.identity;
        billboard.transform.localScale = Vector3.one;

        MeshFilter mf = billboard.AddComponent<MeshFilter>();
        MeshRenderer mr = billboard.AddComponent<MeshRenderer>();
        billboard.AddComponent<BillboardRotation>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;

        Renderer mainRenderer = prefabRoot.GetComponentInChildren<Renderer>(true);
        if (mainRenderer != null && mainRenderer.gameObject != billboard)
        {
            Renderer billboardRenderer = billboard.GetComponent<Renderer>();

            LOD[] lods = new LOD[2];
            lods[0] = new LOD(0.5f, new Renderer[] { mainRenderer });
            lods[1] = new LOD(0.01f, new Renderer[] { billboardRenderer });

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private Bounds CalculatePrefabBounds(GameObject prefab)
    {
        GameObject temp = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        temp.transform.position = Vector3.zero;

        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            DestroyImmediate(temp);
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            if (renderer.name != "Billboard")
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        DestroyImmediate(temp);
        return bounds;
    }
}
