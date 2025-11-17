using UnityEngine;
using UnityEditor;

public class ConvertMaterialsToURP : MonoBehaviour {
    [MenuItem("Tools/Convert Standard to URP Lit")]
    static void ConvertSelectedMaterials() {
        foreach (var obj in Selection.objects) {
            if (obj is Material oldMat && oldMat.shader.name == "Standard") {
                // Create a new URP Lit material
                Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                // Copy the color
                if (oldMat.HasProperty("_Color"))
                    newMat.color = oldMat.color;

                // Optionally copy metallic/smoothness
                if (oldMat.HasProperty("_Metallic"))
                    newMat.SetFloat("_Metallic", oldMat.GetFloat("_Metallic"));
                if (oldMat.HasProperty("_Glossiness"))
                    newMat.SetFloat("_Smoothness", oldMat.GetFloat("_Glossiness"));

                // Save the new material in the same folder
                string path = AssetDatabase.GetAssetPath(oldMat);
                string folder = System.IO.Path.GetDirectoryName(path);
                string name = oldMat.name + "_URP";
                AssetDatabase.CreateAsset(newMat, folder + "/" + name + ".mat");
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Conversion complete.");
    }
}
