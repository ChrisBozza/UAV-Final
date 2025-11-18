using System.Collections;
using UnityEngine;

public class MovementDecay : MonoBehaviour
{

    DroneController droneController;
    MovementTracker movementTracker;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneController = GetComponent<DroneController>();
        StartCoroutine(DecayHandler());
    }

    private IEnumerator DecayHandler() {
        // Wait until movementTracker is initialized
        bool isInitialized = false;
        while (isInitialized) {
            yield return new WaitForSeconds(0.1f);
            if (movementTracker) {
                isInitialized = true;
            }
        }

        while (true) {
            yield return new WaitForSeconds(0.1f);
            DecayVerticalMovement();
            DecayHorizontalMovement();
        }
            
    }

    public void SetMovementTracker(MovementTracker x) {
        movementTracker = x;
    }

    public void DecayVerticalMovement() {

        if (movementTracker.IsMoving("up")) return;
        if (movementTracker.IsMoving("down")) return;

        Vector3 currentMomentum = droneController.GetMomentum();
        currentMomentum.y *= 0.95f; // Reduce vertical velocity by 5%

        if (Mathf.Abs(currentMomentum.y) > 0.01f) {
            droneController.SetMomentum(currentMomentum);
        } else {
            currentMomentum.y = 0f;
            droneController.SetMomentum(currentMomentum);
        }
    }

    public void DecayHorizontalMovement() {
        /* Horizontal decay should always be active to act as friction
        if (movementTracker.IsMoving("forward") ||
            movementTracker.IsMoving("backward") ||
            movementTracker.IsMoving("left") ||
            movementTracker.IsMoving("right")) return;
        */

        Vector3 currentMomentum = droneController.GetMomentum();
        currentMomentum.x *= 0.98f; // Reduce horizontal velocity by 5%
        currentMomentum.z *= 0.98f; // Reduce horizontal velocity by 5%

        if (Mathf.Abs(currentMomentum.x) > 0.01f) {
            droneController.SetMomentum(currentMomentum);
        } else {
            currentMomentum.x = 0f;
            droneController.SetMomentum(currentMomentum);
        }

        if (Mathf.Abs(currentMomentum.z) > 0.01f) {
            droneController.SetMomentum(currentMomentum);
        } else {
            currentMomentum.z = 0f;
            droneController.SetMomentum(currentMomentum);
        }
    }
}
