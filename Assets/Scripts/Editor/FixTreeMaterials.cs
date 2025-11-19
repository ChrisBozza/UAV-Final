using UnityEngine;
using UnityEditor;

public class FixTreeMaterials : EditorWindow
{
    [MenuItem("Tools/Fix Tree Materials")]
    public static void ShowWindow()
    {
        GetWindow<FixTreeMaterials>("Fix Materials");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Tree Materials", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("This will convert all materials in LowPoly_ForestPack/Materials from SpeedTree Billboard shader to URP Lit shader.", MessageType.Info);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Materials"))
        {
            FixAllMaterials();
        }
    }

    private static void FixAllMaterials()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/ImportedAssets/LowPoly_ForestPack/Materials" });
        
        int fixedCount = 0;
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (material != null && material.shader.name.Contains("SpeedTree"))
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader != null)
                {
                    Texture mainTexture = material.GetTexture("_MainTex");
                    
                    material.shader = litShader;
                    
                    if (mainTexture != null)
                    {
                        material.SetTexture("_BaseMap", mainTexture);
                    }
                    
                    if (material.name.Contains("Leaves") || material.name.Contains("Bush") || material.name.Contains("Grass"))
                    {
                        material.SetFloat("_Surface", 0);
                        material.SetFloat("_AlphaClip", 1);
                        material.SetFloat("_AlphaCutoff", 0.5f);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.SetFloat("_Cull", 0);
                        material.renderQueue = 2450;
                    }
                    else
                    {
                        material.SetFloat("_Surface", 0);
                        material.SetFloat("_Cull", 2);
                    }
                    
                    EditorUtility.SetDirty(material);
                    fixedCount++;
                    Debug.Log($"Fixed material: {material.name}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Fixed {fixedCount} materials!", "OK");
    }

    [MenuItem("Tools/Fix Tree Materials/Quick Fix All")]
    public static void QuickFixAll()
    {
        FixAllMaterials();
    }
}
