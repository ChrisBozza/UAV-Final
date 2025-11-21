using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Follow Settings")]
    [SerializeField] float positionSmoothSpeed = 5f;
    [SerializeField] float rotationSmoothSpeed = 5f;

    [Header("Rotation Settings")]
    [SerializeField] bool followYaw = true;
    [SerializeField] float cameraPitch = 15f;

    void LateUpdate()
    {
        if (!target) return;

        UpdatePosition();
        UpdateRotation();
    }

    void UpdatePosition()
    {
        Vector3 targetPosition = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);
    }

    void UpdateRotation()
    {
        Quaternion targetRotation;

        if (followYaw)
        {
            float targetYaw = target.eulerAngles.y;
            Vector3 targetEuler = new Vector3(cameraPitch, targetYaw, 0f);
            targetRotation = Quaternion.Euler(targetEuler);
        }
        else
        {
            targetRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}

