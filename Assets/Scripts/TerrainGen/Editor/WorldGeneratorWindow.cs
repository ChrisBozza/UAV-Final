using UnityEngine;
using UnityEditor;

public class WorldGeneratorWindow : EditorWindow {
    Terrain terrain;
    WorldGenSettings settings;

    [MenuItem("Tools/World Generator")]
    public static void ShowWindow() {
        GetWindow<WorldGeneratorWindow>("World Generator");
    }

    void OnGUI() {
        EditorGUILayout.LabelField("World Generation", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        settings = (WorldGenSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(WorldGenSettings), false);

        EditorGUILayout.Space();

        if (terrain == null)
            EditorGUILayout.HelpBox("Assign a Terrain.", MessageType.Info);

        if (settings == null)
            EditorGUILayout.HelpBox("Assign a WorldGenSettings asset.", MessageType.Info);

        GUI.enabled = (terrain != null && settings != null);

        if (GUILayout.Button("Generate World")) {
            WorldGenerator.GenerateWorld(terrain, settings);
        }

        if (GUILayout.Button("Clear Previous Generation")) {
            WorldGenerator.ClearPreviousGeneration();
        }

        if (GUILayout.Button("Visualize Tree Density Map")) {
            PropPlacer.VisualizeTreeDensity(terrain, settings);
        }

        GUI.enabled = true;
    }
}
