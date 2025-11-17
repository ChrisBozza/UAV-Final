using UnityEngine;

public class VisualTilt : MonoBehaviour {

    [SerializeField] GameObject droneBody;
    [SerializeField] float tiltAmount = 15f; // Max tilt angle in degrees
    [SerializeField] float tiltSpeed = 5f;  // How fast the tilt interpolates

    MovementTracker movementTracker;

    public void SetMovementTracker(MovementTracker x) {
        movementTracker = x;
    }

    void Update() {
        UpdateTilt();
    }

    public void UpdateTilt() {
        float tiltX = 0f;
        float tiltZ = 0f;

        if (movementTracker.IsMoving("forward"))
            tiltX = tiltAmount;

        else if (movementTracker.IsMoving("backward"))
            tiltX = -tiltAmount;

        if (movementTracker.IsMoving("left"))
            tiltZ = tiltAmount;

        else if (movementTracker.IsMoving("right"))
            tiltZ = -tiltAmount;

        // Build rotation
        Quaternion targetRotation = Quaternion.Euler(tiltX, 0f, tiltZ);

        // Smooth transition
        droneBody.transform.localRotation = Quaternion.Lerp(droneBody.transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }
}
