using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TreeConverter : MonoBehaviour
{
    public Terrain terrain;  // Assign your terrain
    public bool deleteTerrainTreesAfter = true;

#if UNITY_EDITOR
    [ContextMenu("Convert Terrain Trees to Prefabs")]
    public void ConvertTrees()
    {
        if (terrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        TerrainData data = terrain.terrainData;

        TreeInstance[] treeInstances = data.treeInstances;
        TreePrototype[] prototypes = data.treePrototypes;

        Debug.Log("Converting " + treeInstances.Length + " trees...");

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance inst = treeInstances[i];

            // Find prefab
            GameObject prefab = prototypes[inst.prototypeIndex].prefab;

            if (prefab == null)
            {
                Debug.LogWarning("Tree prototype has no prefab! Skipping.");
                continue;
            }

            // Convert Terrain local position → world position
            Vector3 worldPos = 
                Vector3.Scale(inst.position, data.size) + terrain.transform.position;

            // Instantiate the prefab as a real GameObject in the scene
            GameObject treeObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            treeObj.transform.position = worldPos;

            // Apply Terrain scale & random scale
            treeObj.transform.localScale = prefab.transform.localScale * inst.widthScale;

            // Apply rotation
            treeObj.transform.rotation = Quaternion.Euler(0f, inst.rotation * Mathf.Rad2Deg, 0f);

            // Optional: Add a visibility script automatically
            // treeObj.AddComponent<TreeVisibility>();

            Undo.RegisterCreatedObjectUndo(treeObj, "Create Tree Instance");
        }

        Debug.Log("Finished converting trees!");

        if (deleteTerrainTreesAfter)
        {
            data.treeInstances = new TreeInstance[0];
            Debug.Log("Terrain trees removed.");
        }
    }
#endif
}