using UnityEngine;
using UnityEditor;

public class RestoreBillboardMaterials : EditorWindow
{
    [MenuItem("Tools/Restore Billboard Materials")]
    public static void RestoreMaterials()
    {
        string[] materialPaths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Billboards" });
        int count = 0;

        foreach (string guid in materialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.name.Contains("Billboard_Material"))
            {
                mat.SetFloat("_AlphaClip", 1);
                mat.SetFloat("_Cutoff", 0.5f);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Restored alpha clipping on {count} billboard materials");
        EditorUtility.DisplayDialog("Materials Restored", 
            $"Restored {count} materials to proper alpha clipping settings.", 
            "OK");
    }
}
