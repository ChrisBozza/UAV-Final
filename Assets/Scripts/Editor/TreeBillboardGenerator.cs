using UnityEngine;
using UnityEditor;
using System.IO;

public class TreeBillboardGenerator : EditorWindow
{
    private GameObject treePrefab;
    private int textureSize = 512;
    private float billboardDistance = 50f;
    private float fadeDuration = 10f;
    private string outputFolder = "Assets/Materials/Billboards";

    [MenuItem("Tools/Tree Billboard Generator")]
    public static void ShowWindow()
    {
        GetWindow<TreeBillboardGenerator>("Billboard Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tree Billboard LOD Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        treePrefab = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", treePrefab, typeof(GameObject), false);
        textureSize = EditorGUILayout.IntSlider("Texture Size", textureSize, 256, 2048);
        billboardDistance = EditorGUILayout.Slider("Billboard Distance", billboardDistance, 20f, 200f);
        fadeDuration = EditorGUILayout.Slider("Fade Duration", fadeDuration, 5f, 50f);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Billboard & Setup LOD"))
        {
            if (treePrefab != null)
            {
                GenerateBillboard();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a tree prefab.", "OK");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool will:\n1. Capture the tree as a billboard texture\n2. Create a billboard material\n3. Create a billboard quad mesh\n4. Add LOD Group to the prefab\n5. Configure LOD levels", MessageType.Info);
    }

    private void GenerateBillboard()
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string prefabPath = AssetDatabase.GetAssetPath(treePrefab);
        GameObject tempTree = Instantiate(treePrefab);
        tempTree.transform.position = Vector3.zero;

        Bounds bounds = CalculateBounds(tempTree);
        Texture2D billboardTexture = RenderTreeToTexture(tempTree, bounds);

        string texturePath = $"{outputFolder}/{treePrefab.name}_Billboard.png";
        SaveTexture(billboardTexture, texturePath);

        Material billboardMaterial = CreateBillboardMaterial(texturePath);
        GameObject billboardQuad = CreateBillboardQuad(bounds, billboardMaterial);

        SetupLODGroup(prefabPath, billboardQuad);

        DestroyImmediate(tempTree);
        DestroyImmediate(billboardQuad);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Billboard generated for {treePrefab.name}!", "OK");
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

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        camera.orthographicSize = maxSize * 0.6f;
        
        camObj.transform.position = bounds.center + Vector3.back * maxSize * 2f;
        camObj.transform.LookAt(bounds.center);

        RenderTexture rt = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        camera.targetTexture = rt;

        int originalLayer = tree.layer;
        SetLayerRecursively(tree, 31);
        camera.cullingMask = 1 << 31;

        camera.Render();

        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        texture.Apply();

        SetLayerRecursively(tree, originalLayer);
        RenderTexture.active = null;
        camera.targetTexture = null;
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
        File.WriteAllBytes(path, bytes);
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
        material.SetFloat("_Surface", 1);
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

    private GameObject CreateBillboardQuad(Bounds bounds, Material material)
    {
        GameObject quad = new GameObject("BillboardQuad");
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

        float width = bounds.size.x;
        float height = bounds.size.y;
        float yOffset = bounds.min.y;

        Mesh mesh = new Mesh();
        mesh.name = "BillboardQuad";

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

        string meshPath = $"{outputFolder}/{treePrefab.name}_BillboardMesh.asset";
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

        GameObject originalModel = prefabRoot.transform.childCount > 0 ? 
            prefabRoot.transform.GetChild(0).gameObject : prefabRoot;

        GameObject billboardInstance = Instantiate(billboardQuad, prefabRoot.transform);
        billboardInstance.name = "Billboard";

        Renderer[] lod0Renderers = originalModel.GetComponentsInChildren<Renderer>();
        Renderer[] lod1Renderers = billboardInstance.GetComponentsInChildren<Renderer>();

        LOD[] lods = new LOD[2];
        lods[0] = new LOD(billboardDistance / 100f, lod0Renderers);
        lods[1] = new LOD(0.01f, lod1Renderers);

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
        lodGroup.fadeMode = LODFadeMode.CrossFade;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
