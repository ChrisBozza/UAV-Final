using UnityEngine;

public class BillboardRotation : MonoBehaviour
{
    private Camera mainCamera;
    private Transform cameraTransform;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                return;
            }
        }

        Vector3 lookDirection = cameraTransform.position - transform.position;
        lookDirection.y = 0;
        
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}
