using UnityEngine;

public class KeyboardInput : MonoBehaviour {
    [SerializeField] DroneController drone;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 90f; // degrees per second

    MovementTracker movementTracker;

    public void SetMovementTracker(MovementTracker x) {
        movementTracker = x;
    }

    void Update() {
        if (!drone) return;
        if (!movementTracker) return;

        ApplyInputs();  
    }

    public void ApplyInputs() {

        // Clear Movement State
        movementTracker.ClearMovementState();

        // Movement Momentum
        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) {
            z += 1f;
            movementTracker.SetMovementState("forward", true);
        }
        if (Input.GetKey(KeyCode.S)) {
            z -= 1f;
            movementTracker.SetMovementState("backward", true);
        }
        if (Input.GetKey(KeyCode.A)) {
            x -= 1f;
            movementTracker.SetMovementState("left", true);
        }
        if (Input.GetKey(KeyCode.D)) {
            x += 1f;
            movementTracker.SetMovementState("right", true);
        }
        if (Input.GetKey(KeyCode.Space)) {
            y += 1f;
            movementTracker.SetMovementState("up", true);
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            y -= 1f;
            movementTracker.SetMovementState("down", true);
        }


        Vector3 momentum = new Vector3(x, y, z).normalized;

        if (momentum.sqrMagnitude > 0f) {
            drone.AddMomentum(momentum * moveSpeed * Time.deltaTime);
        }


        // Rotation
        Vector3 rotationInput = Vector3.zero;

        if (Input.GetKey(KeyCode.Q)) {
            rotationInput.y -= 1f;
            movementTracker.SetMovementState("yawLeft", true);
        }
        if (Input.GetKey(KeyCode.E)) {
            rotationInput.y += 1f;
            movementTracker.SetMovementState("yawRight", true);
        }

        if (rotationInput != Vector3.zero) {
            drone.AddRotation(rotationInput * rotationSpeed * Time.deltaTime);
        }


        // Stabilize
        if (Input.GetKeyDown(KeyCode.LeftControl)) {
            drone.SetMomentum(Vector3.zero);
        }


    }
}
