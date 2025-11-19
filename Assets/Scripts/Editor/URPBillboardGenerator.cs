using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class URPBillboardGenerator : EditorWindow
{
    private bool includeCircleTrees = true;
    private bool includeNormalTrees = true;
    private bool includeDeadTrees = true;
    private int textureSize = 512;
    private float billboardScale = 0.7f;

    [MenuItem("Tools/URP Billboard Generator (Fixed)")]
    public static void ShowWindow()
    {
        GetWindow<URPBillboardGenerator>("URP Billboard Gen");
    }

    private void OnGUI()
    {
        GUILayout.Label("URP Billboard Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This uses a different rendering approach that works with URP in Unity 6.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tree Types:", EditorStyles.boldLabel);
        includeCircleTrees = EditorGUILayout.Toggle("Circle Trees", includeCircleTrees);
        includeNormalTrees = EditorGUILayout.Toggle("Normal Trees", includeNormalTrees);
        includeDeadTrees = EditorGUILayout.Toggle("Dead Trees", includeDeadTrees);
        
        EditorGUILayout.Space();
        textureSize = EditorGUILayout.IntSlider("Texture Size", textureSize, 256, 2048);
        billboardScale = EditorGUILayout.Slider("Billboard Scale", billboardScale, 0.3f, 2.0f);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Billboards (Scene-Based Method)"))
        {
            GenerateWithSceneMethod();
        }
    }

    private void GenerateWithSceneMethod()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;
        
        if (!EditorUtility.DisplayDialog("Generate Billboards",
            "This will create a temporary scene for rendering. Your current scene will be restored afterward.\n\nContinue?",
            "Yes", "Cancel"))
        {
            return;
        }

        List<string> treePrefabs = GetTreePrefabList();

        string tempScenePath = "Assets/Temp_BillboardCapture.unity";
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        GameObject camObj = new GameObject("Main Camera");
        Camera camera = camObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.orthographic = true;

        int successCount = 0;

        foreach (string prefabPath in treePrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                EditorUtility.DisplayProgressBar("Generating Billboards", 
                    $"Processing {prefab.name}...", 
                    (float)successCount / treePrefabs.Count);

                if (CaptureTreeInScene(prefab, camera, camObj.transform))
                {
                    successCount++;
                }
            }
        }

        EditorUtility.ClearProgressBar();

        if (!string.IsNullOrEmpty(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene);
        }
        else
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete", 
            $"Successfully generated {successCount} out of {treePrefabs.Count} billboards!\n\nCheck Assets/Materials/Billboards/", 
            "OK");
    }

    private bool CaptureTreeInScene(GameObject prefab, Camera camera, Transform cameraTransform)
    {
        GameObject tree = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        tree.transform.position = Vector3.zero;
        tree.transform.rotation = Quaternion.identity;

        Bounds bounds = CalculateBounds(tree);
        
        Vector3 localBoundsSize = tree.transform.InverseTransformVector(bounds.size);
        float widthInLocal = Mathf.Max(Mathf.Abs(localBoundsSize.x), Mathf.Abs(localBoundsSize.z));
        float heightInLocal = Mathf.Abs(localBoundsSize.y);
        
        float aspectRatio = widthInLocal / heightInLocal;
        
        int rtWidth = textureSize;
        int rtHeight = textureSize;
        
        if (aspectRatio < 1f)
        {
            rtWidth = Mathf.RoundToInt(textureSize * aspectRatio);
        }
        else
        {
            rtHeight = Mathf.RoundToInt(textureSize / aspectRatio);
        }
        
        rtWidth = Mathf.Max(rtWidth, 256);
        rtHeight = Mathf.Max(rtHeight, 256);

        camera.orthographicSize = heightInLocal * 0.55f;
        camera.aspect = (float)rtWidth / rtHeight;
        
        Vector3 cameraOffset = Vector3.back * widthInLocal * 2.5f;
        cameraTransform.position = bounds.center + cameraOffset;
        cameraTransform.LookAt(bounds.center);

        camera.Render();
        
        RenderTexture rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        camera.targetTexture = rt;
        
        camera.Render();
        
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(rtWidth, rtHeight, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
        texture.Apply();
        
        int opaquePixels = CountOpaquePixels(texture);
        Debug.Log($"Captured {prefab.name}: {rtWidth}x{rtHeight}, Opaque pixels: {opaquePixels}");

        camera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(tree);

        if (opaquePixels < 100)
        {
            Debug.LogWarning($"{prefab.name} captured with very few opaque pixels!");
            return false;
        }

        string outputFolder = "Assets/Materials/Billboards";
        if (!System.IO.Directory.Exists(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
        }

        string texturePath = $"{outputFolder}/{prefab.name}_Billboard.png";
        SaveTexture(texture, texturePath);

        Material billboardMaterial = CreateBillboardMaterial(texturePath);
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject billboardQuad = CreateBillboardQuad(prefab.name, bounds, billboardMaterial, outputFolder);
        SetupLODGroup(prefabPath, billboardQuad);
        
        DestroyImmediate(billboardQuad);
        DestroyImmediate(texture);

        return true;
    }

    private int CountOpaquePixels(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        int count = 0;
        foreach (Color p in pixels)
        {
            if (p.a > 0.1f) count++;
        }
        return count;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
        localCenter.x = 0;
        localCenter.z = 0;
        bounds.center = obj.transform.TransformPoint(localCenter);
        
        return bounds;
    }

    private void SaveTexture(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private Material CreateBillboardMaterial(string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
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

    private GameObject CreateBillboardQuad(string treeName, Bounds bounds, Material material, string outputFolder)
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

        string meshPath = $"{outputFolder}/{treeName}_BillboardMesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject billboard = new GameObject("Billboard");
        MeshFilter mf = billboard.AddComponent<MeshFilter>();
        MeshRenderer mr = billboard.AddComponent<MeshRenderer>();
        billboard.AddComponent<BillboardRotation>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;

        return billboard;
    }

    private void SetupLODGroup(string prefabPath, GameObject billboardQuad)
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

        GameObject newBillboard = Instantiate(billboardQuad);
        newBillboard.name = "Billboard";
        newBillboard.transform.SetParent(prefabRoot.transform);
        newBillboard.transform.localPosition = Vector3.zero;
        newBillboard.transform.localRotation = Quaternion.identity;
        newBillboard.transform.localScale = Vector3.one;

        Renderer mainRenderer = prefabRoot.GetComponentInChildren<Renderer>();
        Renderer billboardRenderer = newBillboard.GetComponent<Renderer>();

        LOD[] lods = new LOD[2];
        lods[0] = new LOD(0.5f, new Renderer[] { mainRenderer });
        lods[1] = new LOD(0.01f, new Renderer[] { billboardRenderer });

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private List<string> GetTreePrefabList()
    {
        List<string> treePrefabs = new List<string>();

        if (includeCircleTrees)
        {
            treePrefabs.AddRange(new string[]
            {
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Autumn.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_AutumnLight.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Summer.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/CircleTree_Winter.prefab"
            });
        }

        if (includeNormalTrees)
        {
            treePrefabs.AddRange(new string[]
            {
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Winter.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Autumn_Small.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_AutumnLight_Small.prefab",
                "Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Tree_Summer_Small.prefab"
            });
        }

        if (includeDeadTrees)
        {
            treePrefabs.Add("Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/Dead_Tree.prefab");
        }

        return treePrefabs;
    }
}
