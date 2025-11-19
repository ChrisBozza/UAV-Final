using UnityEngine;
using UnityEditor;

public class BillboardDiagnostics : EditorWindow
{
    private GameObject testTree;
    private Vector2 scrollPos;
    private string diagnosticsText = "";

    [MenuItem("Tools/Billboard Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<BillboardDiagnostics>("Billboard Debug");
    }

    private void OnGUI()
    {
        GUILayout.Label("Billboard Diagnostics", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        testTree = (GameObject)EditorGUILayout.ObjectField("Test Tree", testTree, typeof(GameObject), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Diagnose Selected Tree"))
        {
            if (Selection.activeGameObject != null)
            {
                testTree = Selection.activeGameObject;
            }
        }

        if (GUILayout.Button("Force Show All Billboards (Test)"))
        {
            ForceShowBillboards();
        }

        if (GUILayout.Button("Fix Billboard Layer Issue"))
        {
            FixBillboardLayers();
        }

        EditorGUILayout.Space();

        if (testTree != null)
        {
            if (GUILayout.Button("Copy Diagnostics to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = diagnosticsText;
                Debug.Log("Diagnostics copied to clipboard!");
            }

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            ShowDiagnostics();
            EditorGUILayout.EndScrollView();
        }
    }

    private void ShowDiagnostics()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("=== TREE ANALYSIS ===");
        sb.AppendLine($"Name: {testTree.name}");
        sb.AppendLine($"Active: {testTree.activeSelf}");
        sb.AppendLine($"Layer: {LayerMask.LayerToName(testTree.layer)}");

        EditorGUILayout.LabelField("Tree Analysis:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {testTree.name}");
        EditorGUILayout.LabelField($"Active: {testTree.activeSelf}");
        EditorGUILayout.LabelField($"Layer: {LayerMask.LayerToName(testTree.layer)}");

        LODGroup lodGroup = testTree.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== LOD GROUP ===");
            sb.AppendLine($"Enabled: {lodGroup.enabled}");
            
            LOD[] lods = lodGroup.GetLODs();
            sb.AppendLine($"LOD Count: {lods.Length}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LOD Group Found:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Enabled: {lodGroup.enabled}");
            EditorGUILayout.LabelField($"LOD Count: {lods.Length}");

            for (int i = 0; i < lods.Length; i++)
            {
                sb.AppendLine();
                sb.AppendLine($"LOD {i}:");
                sb.AppendLine($"  Screen Height: {lods[i].screenRelativeTransitionHeight:F3}");
                sb.AppendLine($"  Renderers: {lods[i].renderers.Length}");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"LOD {i}:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Screen Height: {lods[i].screenRelativeTransitionHeight:F3}");
                EditorGUILayout.LabelField($"  Renderers: {lods[i].renderers.Length}");

                foreach (Renderer renderer in lods[i].renderers)
                {
                    if (renderer != null)
                    {
                        sb.AppendLine($"    - {renderer.gameObject.name}");
                        sb.AppendLine($"      Enabled: {renderer.enabled}");
                        sb.AppendLine($"      Active: {renderer.gameObject.activeSelf}");
                        sb.AppendLine($"      Materials: {renderer.sharedMaterials.Length}");

                        EditorGUILayout.LabelField($"    - {renderer.gameObject.name}");
                        EditorGUILayout.LabelField($"      Enabled: {renderer.enabled}");
                        EditorGUILayout.LabelField($"      Active: {renderer.gameObject.activeSelf}");
                        EditorGUILayout.LabelField($"      Materials: {renderer.sharedMaterials.Length}");
                        
                        foreach (Material mat in renderer.sharedMaterials)
                        {
                            if (mat != null)
                            {
                                sb.AppendLine($"        Mat: {mat.name}");
                                sb.AppendLine($"        Shader: {mat.shader.name}");

                                EditorGUILayout.LabelField($"        Mat: {mat.name}");
                                EditorGUILayout.LabelField($"        Shader: {mat.shader.name}");
                            }
                        }

                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            sb.AppendLine($"      Mesh: {meshFilter.sharedMesh.name}");
                            sb.AppendLine($"      Vertices: {meshFilter.sharedMesh.vertexCount}");
                            sb.AppendLine($"      Triangles: {meshFilter.sharedMesh.triangles.Length / 3}");

                            EditorGUILayout.LabelField($"      Mesh: {meshFilter.sharedMesh.name}");
                            EditorGUILayout.LabelField($"      Vertices: {meshFilter.sharedMesh.vertexCount}");
                            EditorGUILayout.LabelField($"      Triangles: {meshFilter.sharedMesh.triangles.Length / 3}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("    - NULL RENDERER!");
                        EditorGUILayout.LabelField("    - NULL RENDERER!", EditorStyles.boldLabel);
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("No LOD Group found!");
            EditorGUILayout.HelpBox("No LOD Group found on this object!", MessageType.Warning);
        }

        Transform billboard = testTree.transform.Find("Billboard");
        if (billboard != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== BILLBOARD CHILD ===");
            sb.AppendLine($"  Active: {billboard.gameObject.activeSelf}");
            sb.AppendLine($"  Position: {billboard.localPosition}");
            sb.AppendLine($"  Scale: {billboard.localScale}");
            sb.AppendLine($"  Layer: {LayerMask.LayerToName(billboard.gameObject.layer)}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Billboard Child:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Active: {billboard.gameObject.activeSelf}");
            EditorGUILayout.LabelField($"  Position: {billboard.localPosition}");
            EditorGUILayout.LabelField($"  Scale: {billboard.localScale}");
            EditorGUILayout.LabelField($"  Layer: {LayerMask.LayerToName(billboard.gameObject.layer)}");
        }
        else
        {
            sb.AppendLine("No Billboard child found!");
            EditorGUILayout.HelpBox("No Billboard child found!", MessageType.Error);
        }

        diagnosticsText = sb.ToString();
    }

    private void ForceShowBillboards()
    {
        LODGroup[] allLODGroups = FindObjectsOfType<LODGroup>();
        int count = 0;

        foreach (LODGroup lodGroup in allLODGroups)
        {
            if (lodGroup.name.Contains("Tree") || lodGroup.name.Contains("tree"))
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods.Length >= 2)
                {
                    lods[0].screenRelativeTransitionHeight = 0.99f;
                    lods[1].screenRelativeTransitionHeight = 0.01f;
                    lodGroup.SetLODs(lods);
                    lodGroup.ForceLOD(1);
                    count++;
                }
            }
        }

        Debug.Log($"Forced {count} trees to show billboards (LOD1). Check Scene view!");
        EditorUtility.DisplayDialog("Billboard Test", 
            $"Forced {count} trees to show billboards.\nLook at Scene view to see if billboards are visible.", 
            "OK");
    }

    private void FixBillboardLayers()
    {
        LODGroup[] allLODGroups = FindObjectsOfType<LODGroup>();
        int count = 0;

        foreach (LODGroup lodGroup in allLODGroups)
        {
            Transform billboard = lodGroup.transform.Find("Billboard");
            if (billboard != null)
            {
                if (billboard.gameObject.layer != lodGroup.gameObject.layer)
                {
                    billboard.gameObject.layer = lodGroup.gameObject.layer;
                    count++;
                }

                if (billboard.gameObject.isStatic != lodGroup.gameObject.isStatic)
                {
                    billboard.gameObject.isStatic = lodGroup.gameObject.isStatic;
                    count++;
                }
            }
        }

        Debug.Log($"Fixed layer/static settings on {count} billboard objects.");
        EditorUtility.DisplayDialog("Fixed", $"Fixed {count} billboard settings.", "OK");
    }
}
