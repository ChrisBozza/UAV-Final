using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainTreeExtractor : EditorWindow
{
    private Terrain terrain;
    private bool makeStatic = true;
    private bool createParentObject = true;
    private bool removeFromTerrain = true;
    private bool enableGPUInstancing = true;
    private string parentObjectName = "ExtractedTrees";

    [MenuItem("Tools/Extract Terrain Trees")]
    public static void ShowWindow()
    {
        GetWindow<TerrainTreeExtractor>("Extract Trees");
    }

    private void OnGUI()
    {
        GUILayout.Label("Extract Terrain Trees to GameObjects", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options:", EditorStyles.boldLabel);
        
        createParentObject = EditorGUILayout.Toggle("Create Parent Object", createParentObject);
        if (createParentObject)
        {
            EditorGUI.indentLevel++;
            parentObjectName = EditorGUILayout.TextField("Parent Name", parentObjectName);
            EditorGUI.indentLevel--;
        }
        
        makeStatic = EditorGUILayout.Toggle("Mark as Static", makeStatic);
        enableGPUInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", enableGPUInstancing);
        removeFromTerrain = EditorGUILayout.Toggle("Remove from Terrain", removeFromTerrain);

        EditorGUILayout.Space();

        if (terrain != null)
        {
            TerrainData terrainData = terrain.terrainData;
            int treeCount = terrainData.treeInstances.Length;
            int prototypeCount = terrainData.treePrototypes.Length;

            EditorGUILayout.HelpBox(
                $"Found {treeCount} tree instances using {prototypeCount} tree prototypes.", 
                MessageType.Info);
        }

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(terrain == null);
        if (GUILayout.Button("Extract All Trees"))
        {
            ExtractTrees();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. Read all tree positions from the terrain\n" +
            "2. Instantiate tree prefabs as GameObjects\n" +
            "3. Preserve positions, rotations, and scales\n" +
            "4. Optionally remove trees from terrain\n" +
            "5. Enable GPU instancing on materials for performance",
            MessageType.Info);
    }

    private void ExtractTrees()
    {
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a terrain.", "OK");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        TreeInstance[] treeInstances = terrainData.treeInstances;

        if (treeInstances.Length == 0)
        {
            EditorUtility.DisplayDialog("No Trees", "No trees found on the terrain.", "OK");
            return;
        }

        GameObject parentObject = null;
        if (createParentObject)
        {
            parentObject = new GameObject(parentObjectName);
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Parent Object");
        }

        if (enableGPUInstancing)
        {
            EnableGPUInstancingOnTreeMaterials(terrainData);
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        Dictionary<int, List<GameObject>> extractedTreesByPrototype = new Dictionary<int, List<GameObject>>();

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance tree = treeInstances[i];

            if (tree.prototypeIndex >= terrainData.treePrototypes.Length)
            {
                Debug.LogWarning($"Tree instance {i} has invalid prototype index {tree.prototypeIndex}");
                continue;
            }

            TreePrototype prototype = terrainData.treePrototypes[tree.prototypeIndex];
            
            if (prototype.prefab == null)
            {
                Debug.LogWarning($"Tree prototype {tree.prototypeIndex} has null prefab");
                continue;
            }

            Vector3 worldPosition = new Vector3(
                terrainPosition.x + tree.position.x * terrainSize.x,
                terrainPosition.y + tree.position.y * terrainSize.y,
                terrainPosition.z + tree.position.z * terrainSize.z
            );

            Quaternion rotation = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
            Vector3 scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

            GameObject treeObject = (GameObject)PrefabUtility.InstantiatePrefab(prototype.prefab);
            treeObject.transform.position = worldPosition;
            treeObject.transform.rotation = rotation;
            treeObject.transform.localScale = Vector3.Scale(treeObject.transform.localScale, scale);

            if (parentObject != null)
            {
                treeObject.transform.SetParent(parentObject.transform);
            }

            if (makeStatic)
            {
                treeObject.isStatic = true;
            }

            treeObject.name = $"{prototype.prefab.name}_{i}";

            Undo.RegisterCreatedObjectUndo(treeObject, "Extract Tree");

            if (!extractedTreesByPrototype.ContainsKey(tree.prototypeIndex))
            {
                extractedTreesByPrototype[tree.prototypeIndex] = new List<GameObject>();
            }
            extractedTreesByPrototype[tree.prototypeIndex].Add(treeObject);

            if (i % 100 == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "Extracting Trees",
                    $"Processing tree {i + 1} of {treeInstances.Length}",
                    (float)(i + 1) / treeInstances.Length
                );
            }
        }

        EditorUtility.ClearProgressBar();

        if (removeFromTerrain)
        {
            Undo.RecordObject(terrainData, "Remove Trees from Terrain");
            terrainData.treeInstances = new TreeInstance[0];
            EditorUtility.SetDirty(terrainData);
        }

        string summary = $"Extracted {treeInstances.Length} trees:\n";
        foreach (var kvp in extractedTreesByPrototype)
        {
            TreePrototype prototype = terrainData.treePrototypes[kvp.Key];
            summary += $"  - {prototype.prefab.name}: {kvp.Value.Count}\n";
        }

        EditorUtility.DisplayDialog("Success", summary, "OK");
        Debug.Log($"Tree Extraction Complete:\n{summary}");
    }

    private void EnableGPUInstancingOnTreeMaterials(TerrainData terrainData)
    {
        HashSet<Material> processedMaterials = new HashSet<Material>();

        foreach (TreePrototype prototype in terrainData.treePrototypes)
        {
            if (prototype.prefab == null) continue;

            Renderer[] renderers = prototype.prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !processedMaterials.Contains(material))
                    {
                        if (material.enableInstancing == false)
                        {
                            material.enableInstancing = true;
                            EditorUtility.SetDirty(material);
                            Debug.Log($"Enabled GPU instancing on material: {material.name}");
                        }
                        processedMaterials.Add(material);
                    }
                }
            }
        }

        if (processedMaterials.Count > 0)
        {
            AssetDatabase.SaveAssets();
        }
    }
}
