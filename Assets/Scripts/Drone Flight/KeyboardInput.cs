using UnityEngine;

public class KeyboardInput : MonoBehaviour {
    [SerializeField] DroneController drone;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 90f; // degrees per second

    void FixedUpdate() {
        if (!drone) return;

        ApplyInputs();  
    }

    public void ApplyInputs() {

        // Movement Momentum
        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) {
            z += 1f;
        }
        if (Input.GetKey(KeyCode.S)) {
            z -= 1f;
        }
        if (Input.GetKey(KeyCode.A)) {
            x -= 1f;
        }
        if (Input.GetKey(KeyCode.D)) {
            x += 1f;
        }
        if (Input.GetKey(KeyCode.Space)) {
            y += 1f;
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            y -= 1f;
        }


        Vector3 momentum = new Vector3(x, y, z).normalized;

        if (momentum.sqrMagnitude > 0f) {
            drone.AddMomentum(momentum * moveSpeed * Time.fixedDeltaTime);
        }


        // Rotation
        Vector3 rotationInput = Vector3.zero;

        if (Input.GetKey(KeyCode.Q)) {
            rotationInput.y -= 1f;
        }
        if (Input.GetKey(KeyCode.E)) {
            rotationInput.y += 1f;
        }

        if (rotationInput != Vector3.zero) {
            drone.AddRotation(rotationInput * rotationSpeed * Time.fixedDeltaTime);
        }


        // Stabilize
        if (Input.GetKeyDown(KeyCode.LeftControl)) {
            drone.SetMomentum(Vector3.zero);
        }


    }
}
