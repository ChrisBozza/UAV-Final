using UnityEngine;
using UnityEditor;
using System.IO;

public class FixBillboardMeshes : EditorWindow
{
    [MenuItem("Tools/Regenerate Billboard Meshes")]
    public static void RegenerateMeshes()
    {
        string[] meshPaths = AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/Materials/Billboards" });
        
        int fixedCount = 0;

        foreach (string guid in meshPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("BillboardMesh"))
            {
                Mesh oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (oldMesh != null)
                {
                    string meshName = Path.GetFileNameWithoutExtension(path);
                    string treeName = meshName.Replace("_BillboardMesh", "");
                    
                    string prefabPath = $"Assets/ImportedAssets/LowPoly_ForestPack/Prefabs/{treeName}.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab != null)
                    {
                        Bounds bounds = CalculateBounds(prefab);
                        
                        Mesh newMesh = CreateBillboardMesh(bounds, 1.0f);
                        newMesh.name = oldMesh.name;
                        
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        
                        Debug.Log($"Regenerated mesh: {meshName}");
                        fixedCount++;
                    }
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", 
            $"Regenerated {fixedCount} billboard meshes!", 
            "OK");
    }

    private static Bounds CalculateBounds(GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            DestroyImmediate(temp);
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            if (renderer.name != "Billboard" && !renderer.name.Contains("Billboard"))
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        DestroyImmediate(temp);
        return bounds;
    }

    private static Mesh CreateBillboardMesh(Bounds bounds, float scale)
    {
        float width = Mathf.Max(bounds.size.x, bounds.size.z) * 0.85f * scale;
        float height = bounds.size.y * scale;
        
        float halfWidth = width / 2f;
        float yOffset = bounds.min.y;

        Mesh mesh = new Mesh();
        mesh.name = "BillboardQuad";

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

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };

        Vector3[] normals = new Vector3[4]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        Debug.Log($"Created billboard mesh - Width: {width:F2}, Height: {height:F2}, Vertices: {mesh.vertexCount}");
        Debug.Log($"  Vertex positions: {vertices[0]}, {vertices[1]}, {vertices[2]}, {vertices[3]}");

        return mesh;
    }
}
