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
        Vector3 toTarget = point.position - droneController.GetPosition();
        float dist = toTarget.magnitude;

        
        if (dist < 1f) {
            reachedTarget = true;
        }

        Vector3 dir = toTarget.normalized;
        Vector3 current = droneController.GetMomentum();
        float maxSpeed = droneController.maxSpeed;

        RotateTowardsTarget(dir);

        Vector3 desiredMomentum = ComputeDesiredMomentum(dir, current, maxSpeed);
        droneController.AddMomentumRelativeToVisual(desiredMomentum, false);
    }

    private void RotateTowardsTarget(Vector3 targetDirection) {
        droneController.SetVisualRotation(targetDirection, rotationSpeed);
    }

    private Vector3 ComputeDesiredMomentum(Vector3 dir, Vector3 current, float maxSpeed) {
        float forwardSpeed = Vector3.Dot(current, dir);

        if (forwardSpeed >= maxSpeed)
            return Vector3.zero;

        float accel = 10f;

        Vector3 accelVector = dir * accel * Time.deltaTime;

        Vector3 predicted = current + accelVector;
        float predictedForward = Vector3.Dot(predicted, dir);

        if (predictedForward > maxSpeed) {
            float extra = predictedForward - maxSpeed;
            accelVector -= dir * extra;
        }

        return accelVector;
    }

    private void ApproachPoint(Transform point) {
    }

    public void AddMomentum(Vector3 momentum) {
        if (!droneReady) return;

        droneController.AddMomentumRelativeToVisual(momentum, false);
    }

    public void SetAutoPilot(bool b) {
        autoPilot = b;
    }

    public void SetTarget(Transform t) {
        reachedTarget = false;
        target = t;
    }
}
