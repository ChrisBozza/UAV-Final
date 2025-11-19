using UnityEngine;

public class VisualTilt : MonoBehaviour {

    [SerializeField] float tiltAmount = 15f;
    [SerializeField] float tiltSpeed = 5f;

    [SerializeField] GameObject droneBody;
    [SerializeField] DroneController droneController;

    [Header("Debug")]
    [SerializeField] bool showDebug = false;

    void Update() {
        UpdateTilt();
    }

    public void UpdateTilt() {
        if (droneController == null || droneBody == null) return;

        Vector3 worldEnginePower = droneController.GetEnginePowerWorldSpace();

        Vector3 localEnginePower = transform.InverseTransformDirection(worldEnginePower);

        Vector3 dir = new Vector3(localEnginePower.x, 0f, localEnginePower.z);

        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        float tiltX = dir.z * tiltAmount;
        float tiltZ = -dir.x * tiltAmount;

        Vector3 tilt = new Vector3(tiltX, 0f, tiltZ);

        Quaternion targetRotation = Quaternion.Euler(tilt);

        droneBody.transform.localRotation =
            Quaternion.Lerp(droneBody.transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);

        if (showDebug && worldEnginePower.sqrMagnitude > 0.001f) {
            Debug.Log($"World Power: {worldEnginePower} | Local Power: {localEnginePower} | Tilt: {tilt} | Body Rotation: {droneBody.transform.localEulerAngles}");
        }
    }

}
