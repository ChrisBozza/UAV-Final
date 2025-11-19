using UnityEngine;
using UnityEditor;

public class InspectBillboardMesh : EditorWindow
{
    [MenuItem("Tools/Inspect Billboard Mesh Data")]
    public static void ShowWindow()
    {
        InspectMesh();
    }

    private static void InspectMesh()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Materials/Billboards/Tree_Summer_BillboardMesh.asset");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Billboards/Tree_Summer_Billboard_Material.mat");

        if (mesh != null)
        {
            sb.AppendLine("=== MESH DATA ===");
            sb.AppendLine($"Vertex Count: {mesh.vertexCount}");
            sb.AppendLine($"Triangle Count: {mesh.triangles.Length / 3}");
            sb.AppendLine($"Bounds: {mesh.bounds}");

            Vector3[] vertices = mesh.vertices;
            sb.AppendLine("Vertices:");
            for (int i = 0; i < vertices.Length; i++)
            {
                sb.AppendLine($"  [{i}] {vertices[i]}");
            }

            Vector2[] uvs = mesh.uv;
            sb.AppendLine("UVs:");
            for (int i = 0; i < uvs.Length; i++)
            {
                sb.AppendLine($"  [{i}] {uvs[i]}");
            }

            int[] triangles = mesh.triangles;
            sb.AppendLine("Triangles:");
            sb.AppendLine($"  {triangles[0]}, {triangles[1]}, {triangles[2]}");
            sb.AppendLine($"  {triangles[3]}, {triangles[4]}, {triangles[5]}");
        }
        else
        {
            sb.AppendLine("ERROR: Could not load mesh!");
        }

        if (mat != null)
        {
            sb.AppendLine("");
            sb.AppendLine("=== MATERIAL DATA ===");
            sb.AppendLine($"Shader: {mat.shader.name}");
            sb.AppendLine($"Render Queue: {mat.renderQueue}");
            
            if (mat.HasProperty("_Cutoff"))
                sb.AppendLine($"Alpha Cutoff: {mat.GetFloat("_Cutoff")}");
            
            if (mat.HasProperty("_AlphaClip"))
                sb.AppendLine($"Alpha Clip Enabled: {mat.GetFloat("_AlphaClip")}");
            
            if (mat.HasProperty("_BaseMap"))
            {
                Texture tex = mat.GetTexture("_BaseMap");
                sb.AppendLine($"Base Texture: {(tex != null ? tex.name : "NULL")}");
            }

            if (mat.HasProperty("_Cull"))
                sb.AppendLine($"Cull Mode: {mat.GetFloat("_Cull")}");

            if (mat.HasProperty("_Surface"))
                sb.AppendLine($"Surface Type: {mat.GetFloat("_Surface")}");
        }
        else
        {
            sb.AppendLine("ERROR: Could not load material!");
        }

        string output = sb.ToString();
        Debug.Log(output);
        GUIUtility.systemCopyBuffer = output;

        EditorUtility.DisplayDialog("Mesh Inspection", 
            "Data logged to console and copied to clipboard!", 
            "OK");
    }
}
