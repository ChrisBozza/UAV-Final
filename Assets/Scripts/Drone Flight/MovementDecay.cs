using UnityEngine;

public class MovementDecay : MonoBehaviour
{

    DroneController droneController;
    MovementTracker movementTracker;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneController = GetComponent<DroneController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!movementTracker) return;
        if (movementTracker.IsMoving("up")) return;
        if (movementTracker.IsMoving("down")) return;

        DecayVerticalMovement();
    }

    public void SetMovementTracker(MovementTracker x) {
        movementTracker = x;
    }

    public void DecayVerticalMovement() {
        Vector3 currentMomentum = droneController.GetMomentum();
        currentMomentum.y *= 0.95f; // Reduce vertical velocity by 5%

        if (currentMomentum.y > 0.01f || currentMomentum.y < -0.01f) {
            droneController.SetMomentum(currentMomentum);
        } else {
            currentMomentum.y = 0f;
            droneController.SetMomentum(currentMomentum);
        }
    }
}
