using UnityEngine;
using UnityEditor;

public class ReplaceMaterialsWithURP : MonoBehaviour {
    [MenuItem("Tools/Replace Materials with URP Versions")]
    static void ReplaceMaterials() {
        foreach (GameObject obj in Selection.gameObjects) {
            // Get all MeshRenderers on the object and its children
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer rend in renderers) {
                Material[] mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    Material oldMat = mats[i];
                    if (oldMat == null) continue;

                    // Build the URP material name
                    string urpMatName = oldMat.name + "_URP";

                    // Search for the URP material in the project
                    string[] guids = AssetDatabase.FindAssets(urpMatName + " t:Material");
                    if (guids.Length > 0) {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        Material urpMat = AssetDatabase.LoadAssetAtPath<Material>(path);

                        if (urpMat != null) {
                            mats[i] = urpMat;
                            Debug.Log($"Replaced {oldMat.name} with {urpMat.name} on {rend.gameObject.name}");
                        }
                    } else {
                        Debug.LogWarning($"Could not find URP material for {oldMat.name}");
                    }
                }

                // Assign the updated materials array back to the renderer
                rend.sharedMaterials = mats;
            }
        }

        Debug.Log("Material replacement complete.");
    }
}
