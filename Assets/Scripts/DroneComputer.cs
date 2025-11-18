using UnityEngine;

public class DroneComputer : MonoBehaviour
{
    public DroneController droneController;
    public bool droneReady = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneController = GetComponent<DroneController>();
        droneReady = true;
    }

    public void AddMomentum(Vector3 momentum) {
        if (!droneReady) return;

        droneController.AddMomentum(momentum);
    }

    public void AddRotation(Vector3 rotation) {
        if (!droneReady) return;

        droneController.AddRotation(rotation);
    }
}
