using System.Collections;
using UnityEngine;

public class DroneComputer : MonoBehaviour
{
    public DroneController droneController;
    public bool droneReady = false;

    public bool autoPilot = false;
    public bool reachedTarget = false;
    public float rotationSpeed = 90f;
    Transform target;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneController = GetComponent<DroneController>();
        droneReady = true;
        StartCoroutine(AutoFly());
    }

    IEnumerator AutoFly() {
        while(!droneReady) yield return new WaitForEndOfFrame();
        
        while (true) {
            yield return new WaitForSeconds(0.1f);
            if (autoPilot && target != null) {
                MoveTowardsPoint(target);
            }
        }
    }

    private void MoveTowardsPoint(Transform point) {
        Vector3 toTarget = point.position - transform.position;
        float dist = toTarget.magnitude;

        
        if (dist < 1f) {
            reachedTarget = true;
        }

        Vector3 dir = toTarget.normalized;
        Vector3 current = droneController.GetMomentum();
        float maxSpeed = droneController.maxSpeed;

        RotateTowardsTarget(dir);

        Vector3 desiredMomentum = ComputeDesiredMomentum(dir, current, maxSpeed);
        droneController.AddMomentum(desiredMomentum);
    }

    private void RotateTowardsTarget(Vector3 targetDirection) {
        droneController.AddVisualRotation(targetDirection, rotationSpeed);
    }

    private Vector3 ComputeDesiredMomentum(Vector3 dir, Vector3 current, float maxSpeed) {
        // Projection of current momentum onto travel direction
        float forwardSpeed = Vector3.Dot(current, dir);

        // If already moving at max speed toward target, do nothing
        if (forwardSpeed >= maxSpeed)
            return Vector3.zero;

        // Choose a simple acceleration rate
        float accel = 10f;

        // Momentum needed to accelerate forward
        Vector3 accelVector = dir * accel * Time.deltaTime;

        // Clamp final momentum (current + accel) to maxSpeed in that direction
        Vector3 predicted = current + accelVector;
        float predictedForward = Vector3.Dot(predicted, dir);

        if (predictedForward > maxSpeed) {
            float extra = predictedForward - maxSpeed;
            accelVector -= dir * extra;
        }

        return accelVector;
    }

    private void ApproachPoint(Transform point) {
        // To be implemented: Fine approach to the target point.
    }

    public void AddMomentum(Vector3 momentum) {
        if (!droneReady) return;

        droneController.AddMomentum(momentum);
    }

    public void AddRotation(Vector3 rotation) {
        if (!droneReady) return;

        droneController.AddRotation(rotation);
    }

    public void SetAutoPilot(bool b) {
        autoPilot = b;
    }

    public void SetTarget(Transform t) {
        reachedTarget = false;
        target = t;
    }
}
