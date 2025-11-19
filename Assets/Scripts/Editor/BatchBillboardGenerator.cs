using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BatchBillboardGenerator : EditorWindow
{
    private bool includeCircleTrees = true;
    private bool includeNormalTrees = true;
    private bool includeDeadTrees = true;
    private int textureSize = 512;
    private float billboardDistance = 50f;
    private float billboardScale = 1.0f;

    [MenuItem("Tools/Batch Billboard Generator")]
    public static void ShowWindow()
    {
        GetWindow<BatchBillboardGenerator>("Batch Billboard");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Billboard Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Tree Types to Process:", EditorStyles.boldLabel);
        includeCircleTrees = EditorGUILayout.Toggle("Circle Trees", includeCircleTrees);
        includeNormalTrees = EditorGUILayout.Toggle("Normal Trees", includeNormalTrees);
        includeDeadTrees = EditorGUILayout.Toggle("Dead Trees", includeDeadTrees);
        
        EditorGUILayout.Space();
        textureSize = EditorGUILayout.IntSlider("Texture Size", textureSize, 256, 2048);
        billboardDistance = EditorGUILayout.Slider("Billboard Distance", billboardDistance, 20f, 200f);
        billboardScale = EditorGUILayout.Slider("Billboard Scale", billboardScale, 0.3f, 2.0f);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Billboards for All Selected Trees"))
        {
            BatchGenerate();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will process all selected tree types and generate billboards with LOD groups.", MessageType.Info);
    }

    private void BatchGenerate()
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

        int successCount = 0;
        int totalCount = treePrefabs.Count;

        foreach (string prefabPath in treePrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                EditorUtility.DisplayProgressBar("Generating Billboards", 
                    $"Processing {prefab.name}...", 
                    (float)successCount / totalCount);

                if (ProcessTreePrefab(prefab, prefabPath))
                {
                    successCount++;
                }
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete", 
            $"Successfully processed {successCount} out of {totalCount} tree prefabs!", 
            "OK");
    }

    private bool ProcessTreePrefab(GameObject treePrefab, string prefabPath)
    {
        try
        {
            string outputFolder = "Assets/Materials/Billboards";
            if (!System.IO.Directory.Exists(outputFolder))
            {
                System.IO.Directory.CreateDirectory(outputFolder);
            }

            GameObject tempTree = Instantiate(treePrefab);
            tempTree.transform.position = Vector3.zero;

            Bounds bounds = CalculateBounds(tempTree);
            Texture2D billboardTexture = RenderTreeToTexture(tempTree, bounds);

            string texturePath = $"{outputFolder}/{treePrefab.name}_Billboard.png";
            SaveTexture(billboardTexture, texturePath);

            Material billboardMaterial = CreateBillboardMaterial(texturePath);
            GameObject billboardQuad = CreateBillboardQuad(treePrefab.name, bounds, billboardMaterial, outputFolder, billboardScale);

            SetupLODGroup(prefabPath, billboardQuad);

            DestroyImmediate(tempTree);
            DestroyImmediate(billboardQuad);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to process {treePrefab.name}: {e.Message}");
            return false;
        }
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

    private Texture2D RenderTreeToTexture(GameObject tree, Bounds bounds)
    {
        GameObject camObj = new GameObject("BillboardCamera");
        Camera camera = camObj.AddComponent<Camera>();
        
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.orthographic = true;
        camera.enabled = false;
        camera.allowHDR = false;
        camera.allowMSAA = true;

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
        
        Vector3 cameraOffset = tree.transform.TransformDirection(Vector3.back * widthInLocal * 2.5f);
        camObj.transform.position = bounds.center + cameraOffset;
        camObj.transform.LookAt(bounds.center);

        RenderTexture rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        rt.Create();
        camera.targetTexture = rt;

        int originalLayer = tree.layer;
        SetLayerRecursively(tree, 31);
        camera.cullingMask = 1 << 31;

        RenderTexture.active = rt;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        
        camera.Render();
        
        Texture2D texture = new Texture2D(rtWidth, rtHeight, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0, false);
        texture.Apply();

        int opaquePixels = 0;
        Color[] pixels = texture.GetPixels();
        foreach (Color p in pixels)
        {
            if (p.a > 0.1f) opaquePixels++;
        }
        Debug.Log($"Rendered {tree.name}: {rtWidth}x{rtHeight}, Opaque pixels: {opaquePixels}/{pixels.Length}");

        SetLayerRecursively(tree, originalLayer);
        RenderTexture.active = null;
        camera.targetTexture = null;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(camObj);

        return texture;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
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
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Surface", 0);
        material.SetFloat("_AlphaClip", 1);
        material.SetFloat("_AlphaCutoff", 0.5f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetFloat("_Cull", 0);
        material.renderQueue = 2450;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.SetTexture("_BaseMap", texture);

        string materialPath = texturePath.Replace(".png", "_Material.mat");
        AssetDatabase.CreateAsset(material, materialPath);
        
        return material;
    }

    private GameObject CreateBillboardQuad(string treeName, Bounds bounds, Material material, string outputFolder, float scale)
    {
        GameObject quad = new GameObject("BillboardQuad");
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

        float width = Mathf.Max(bounds.size.x, bounds.size.z) * 0.85f * scale;
        float height = bounds.size.y * scale;
        
        Vector3 boundsMin = bounds.min;
        boundsMin.x = 0;
        boundsMin.z = 0;
        float yOffset = boundsMin.y;

        Mesh mesh = new Mesh();
        mesh.name = $"{treeName}_BillboardMesh";

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width * 0.5f, yOffset, 0),
            new Vector3(width * 0.5f, yOffset, 0),
            new Vector3(-width * 0.5f, yOffset + height, 0),
            new Vector3(width * 0.5f, yOffset + height, 0)
        };

        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        BillboardRotation billboard = quad.AddComponent<BillboardRotation>();

        string meshPath = $"{outputFolder}/{treeName}_BillboardMesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);

        return quad;
    }

    private void SetupLODGroup(string prefabPath, GameObject billboardQuad)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        LODGroup lodGroup = prefabRoot.GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            lodGroup = prefabRoot.AddComponent<LODGroup>();
        }

        Transform billboardChild = prefabRoot.transform.Find("Billboard");
        if (billboardChild != null)
        {
            DestroyImmediate(billboardChild.gameObject);
        }

        GameObject billboardInstance = Instantiate(billboardQuad, prefabRoot.transform);
        billboardInstance.name = "Billboard";

        List<Renderer> lod0Renderers = new List<Renderer>();
        foreach (Transform child in prefabRoot.transform)
        {
            if (child.name != "Billboard")
            {
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                lod0Renderers.AddRange(renderers);
            }
        }

        if (lod0Renderers.Count == 0)
        {
            Renderer rootRenderer = prefabRoot.GetComponent<Renderer>();
            if (rootRenderer != null)
            {
                lod0Renderers.Add(rootRenderer);
            }
        }

        Renderer[] lod1Renderers = billboardInstance.GetComponentsInChildren<Renderer>();

        LOD[] lods = new LOD[2];
        lods[0] = new LOD(billboardDistance / 100f, lod0Renderers.ToArray());
        lods[1] = new LOD(0.01f, lod1Renderers);

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
        lodGroup.fadeMode = LODFadeMode.CrossFade;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
