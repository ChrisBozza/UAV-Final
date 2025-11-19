using UnityEngine;

public class KeyboardInput : MonoBehaviour {
    [SerializeField] DroneController drone;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 90f;

    void FixedUpdate() {
        if (!drone) return;

        ApplyInputs();  
    }

    void Update() {
        if (!drone) return;

        ApplyRotationInputs();
    }

    public void ApplyInputs() {

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
            drone.AddMomentumRelativeToVisual(momentum * moveSpeed * Time.fixedDeltaTime, true);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl)) {
            drone.SetMomentum(Vector3.zero);
        }
    }

    void ApplyRotationInputs() {
        Vector3 rotationInput = Vector3.zero;

        if (Input.GetKey(KeyCode.Q)) {
            rotationInput.y -= 1f;
        }
        if (Input.GetKey(KeyCode.E)) {
            rotationInput.y += 1f;
        }

        if (rotationInput != Vector3.zero && drone.visualDrone != null) {
            Vector3 currentEuler = drone.visualDrone.eulerAngles;
            currentEuler.y += rotationInput.y * rotationSpeed * Time.deltaTime;
            drone.visualDrone.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        }
    }
}
