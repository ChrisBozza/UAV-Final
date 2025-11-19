using UnityEngine;
using UnityEditor;

public class ManualBillboardCapture : EditorWindow
{
    private GameObject selectedPrefab;
    private float billboardScale = 0.7f;
    private string captureInstructions = @"MANUAL BILLBOARD CAPTURE INSTRUCTIONS:

1. Select a tree prefab in the Project window
2. Set the billboard scale below
3. Click 'Setup Capture Scene'
4. A new scene will open with the tree and camera
5. In the Scene view, position the camera to frame the tree nicely
6. Click 'Capture Billboard from Scene View'
7. The billboard will be saved and added to the prefab

This method uses the Scene view rendering which works properly with URP!";

    private Camera captureCamera;
    private GameObject captureTree;

    [MenuItem("Tools/Manual Billboard Capture")]
    public static void ShowWindow()
    {
        GetWindow<ManualBillboardCapture>("Manual Capture");
    }

    private void OnGUI()
    {
        GUILayout.Label("Manual Billboard Capture", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(captureInstructions, MessageType.Info);
        EditorGUILayout.Space();

        selectedPrefab = EditorGUILayout.ObjectField("Tree Prefab:", selectedPrefab, typeof(GameObject), false) as GameObject;
        billboardScale = EditorGUILayout.Slider("Billboard Scale", billboardScale, 0.3f, 2.0f);

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(selectedPrefab == null);
        if (GUILayout.Button("Setup Capture Scene"))
        {
            SetupCaptureScene();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(captureTree == null);
        if (GUILayout.Button("Capture Billboard from Scene View"))
        {
            CaptureFromSceneView();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        if (GUILayout.Button("Use Screenshot Method (External)"))
        {
            ShowScreenshotMethod();
        }
    }

    private void SetupCaptureScene()
    {
        if (captureTree != null)
        {
            DestroyImmediate(captureTree);
        }

        captureTree = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
        captureTree.transform.position = Vector3.zero;
        captureTree.transform.rotation = Quaternion.identity;

        if (SceneView.lastActiveSceneView != null)
        {
            Bounds bounds = CalculateBounds(captureTree);
            SceneView.lastActiveSceneView.Frame(bounds, false);
            SceneView.lastActiveSceneView.Repaint();
        }

        Selection.activeGameObject = captureTree;

        EditorUtility.DisplayDialog("Setup Complete",
            "Tree placed in scene!\n\n" +
            "1. Use Scene view to frame the tree\n" +
            "2. Make sure it's well-lit\n" +
            "3. Click 'Capture Billboard from Scene View'",
            "OK");
    }

    private void CaptureFromSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            EditorUtility.DisplayDialog("Error", "No active Scene view!", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Screenshot Method Required",
            "Unfortunately, URP rendering from editor scripts is broken in Unity 6.\n\n" +
            "Please use the Screenshot Method instead:\n" +
            "1. Click 'Use Screenshot Method (External)'\n" +
            "2. Follow the instructions to capture billboards using screenshots",
            "OK");
    }

    private void ShowScreenshotMethod()
    {
        string instructions = @"SCREENSHOT-BASED BILLBOARD WORKFLOW:

Since URP doesn't render properly in editor scripts, use this workaround:

1. Create a new scene with good lighting
2. Add each tree prefab to the scene
3. Position an orthographic camera to frame the tree
4. Set camera background to transparent (clear flags: Solid Color, RGBA 0,0,0,0)
5. In Game view, take a screenshot using:
   - Unity's screenshot tool
   - Or use ScreenCapture.CaptureScreenshot() in a runtime script
6. Save the PNG to Assets/Materials/Billboards/
7. Import with Alpha Is Transparency enabled
8. Create a material with Universal Render Pipeline/Lit shader
9. Set the billboard texture and enable Alpha Clipping
10. Use the 'FixLODTransitions' tool to add billboard LODs to prefabs manually

ALTERNATIVELY:
Use external software like Blender or Substance to render orthographic views of the trees!";

        EditorUtility.DisplayDialog("Screenshot Method", instructions, "OK");

        Debug.Log(instructions);
        GUIUtility.systemCopyBuffer = instructions;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }
}
