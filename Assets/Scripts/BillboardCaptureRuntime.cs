using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BillboardCaptureRuntime : MonoBehaviour
{
    public List<GameObject> treePrefabs = new List<GameObject>();
    public int textureSize = 512;
    public string outputFolder = "Assets/Materials/Billboards";
    public float verticalPadding = 0.25f;
    
    private Camera captureCamera;
    private int currentTreeIndex = 0;
    private bool isCapturing = false;

    private void Start()
    {
        SetupCamera();
        StartCoroutine(CaptureAllTrees());
    }

    private void SetupCamera()
    {
        captureCamera = GetComponent<Camera>();
        if (captureCamera == null)
        {
            captureCamera = gameObject.AddComponent<Camera>();
        }

        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0.2f, 0.5f, 0.9f, 1f);
        captureCamera.orthographic = true;
        captureCamera.enabled = true;
    }

    private IEnumerator CaptureAllTrees()
    {
        isCapturing = true;

        foreach (GameObject prefab in treePrefabs)
        {
            if (prefab != null)
            {
                yield return StartCoroutine(CaptureTree(prefab));
            }
        }

        isCapturing = false;
        Debug.Log($"Captured {treePrefabs.Count} tree billboards!");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator CaptureTree(GameObject prefab)
    {
        GameObject tree = Instantiate(prefab);
        tree.transform.position = Vector3.zero;
        tree.transform.rotation = Quaternion.identity;

        yield return null;

        Bounds bounds = CalculateBounds(tree);
        
        bounds.Expand(bounds.size.y * verticalPadding);
        
        Vector3 localBoundsSize = tree.transform.InverseTransformVector(bounds.size);
        float widthInLocal = Mathf.Max(Mathf.Abs(localBoundsSize.x), Mathf.Abs(localBoundsSize.z));
        float heightInLocal = Mathf.Abs(localBoundsSize.y);

        captureCamera.orthographicSize = heightInLocal * 0.65f;
        
        Vector3 cameraOffset = Vector3.back * widthInLocal * 3.0f;
        transform.position = bounds.center + cameraOffset;
        transform.LookAt(bounds.center);

        yield return new WaitForEndOfFrame();

        string filename = $"{outputFolder}/{prefab.name}_Billboard.png";
        ScreenCapture.CaptureScreenshot(filename, 1);
        
        Debug.Log($"Captured: {filename}");

        Destroy(tree);
        
        yield return new WaitForSeconds(0.2f);
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
            if (renderer.name != "Billboard")
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
        localCenter.x = 0;
        localCenter.z = 0;
        bounds.center = obj.transform.TransformPoint(localCenter);

        return bounds;
    }
}
