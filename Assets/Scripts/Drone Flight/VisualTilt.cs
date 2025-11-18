using UnityEngine;

public class VisualTilt : MonoBehaviour {

    [SerializeField] float tiltAmount = 15f; // Max tilt angle in degrees
    [SerializeField] float tiltSpeed = 5f;  // How fast the tilt interpolates

    [SerializeField] GameObject droneBody;
    DroneController droneController;

    void Start() {
        droneController = GetComponent<DroneController>();
    }

    void Update() {
        UpdateTilt();
    }

    public void UpdateTilt() {
        Vector3 enginePower = droneController.GetEnginePower();

        // Horizontal direction only
        Vector3 dir = new Vector3(enginePower.x, 0f, enginePower.z);

        // Normalize so tilt amount stays consistent
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        // Convert movement direction into tilt
        // Forward movement means tilt forward (positive X tilt)
        float tiltX = dir.z * tiltAmount;
        float tiltZ = -dir.x * tiltAmount;

        Vector3 tilt = new Vector3(tiltX, 0f, tiltZ);

        Quaternion targetRotation = Quaternion.Euler(tilt);

        droneBody.transform.localRotation =
            Quaternion.Lerp(droneBody.transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

}
