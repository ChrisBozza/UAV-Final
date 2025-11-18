using System.Collections;
using UnityEngine;

public class MovementDecay : MonoBehaviour {
    DroneController droneController;

    void Start() {
        droneController = GetComponent<DroneController>();
        StartCoroutine(DecayHandler());
    }

    private IEnumerator DecayHandler() {
        while (true) {
            yield return new WaitForSeconds(0.1f);
            DecayVerticalMovement();
            DecayHorizontalMovement();
        }
    }

    public void DecayVerticalMovement() {
        Vector3 momentum = droneController.GetMomentum();
        Vector3 engine = droneController.GetEnginePower();

        // If engine has significant vertical power, don't decay Y
        if (Mathf.Abs(engine.y) > 0.1f)
            return;

        momentum.y *= 0.95f;

        if (Mathf.Abs(momentum.y) < 0.01f)
            momentum.y = 0f;

        droneController.SetMomentum(momentum);
    }

    public void DecayHorizontalMovement() {
        Vector3 momentum = droneController.GetMomentum();
        Vector3 engine = droneController.GetEnginePower();

        // Decay X only if the engine isn't pushing strongly on X
        if (Mathf.Abs(engine.x) < 0.1f) {
            momentum.x *= 0.98f;
            if (Mathf.Abs(momentum.x) < 0.01f)
                momentum.x = 0f;
        }

        // Decay Z only if the engine isn't pushing strongly on Z
        if (Mathf.Abs(engine.z) < 0.1f) {
            momentum.z *= 0.98f;
            if (Mathf.Abs(momentum.z) < 0.01f)
                momentum.z = 0f;
        }

        droneController.SetMomentum(momentum);
    }
}
