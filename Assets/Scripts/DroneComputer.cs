using System.Collections;
using System.Drawing;
using UnityEngine;

public class DroneComputer : MonoBehaviour
{
    public DroneController droneController;
    public bool droneReady = false;

    public bool autoPilot = false;
    public bool reachedTarget = false;
    public float rotationSpeed = 90f;
    Transform target;

    [Header("Speed Control Settings")]
    public float minSpeedDistance = 2f;
    public float maxSpeedDistance = 10f;
    public float minSpeedMultiplier = 0.3f;


    void Start()
    {
        droneController = GetComponent<DroneController>();
        droneReady = true;
        StartCoroutine(AutoFly());
        StartCoroutine(AutoCheck());
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

    IEnumerator AutoCheck() {
        while(!droneReady) yield return new WaitForEndOfFrame();

        while(true) {
            yield return null;
            Vector3 toTarget = target.position - droneController.GetPosition();
            float dist = toTarget.magnitude;


            if (dist < 2f) {
                reachedTarget = true;
            }

        }
    }

    private void MoveTowardsPoint(Transform point) {

        Vector3 toTarget = target.position - droneController.GetPosition();
        float distanceToTarget = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;
        Vector3 current = droneController.GetMomentum();
        float baseMaxSpeed = droneController.maxSpeed;

        float effectiveMaxSpeed = CalculateEffectiveMaxSpeed(baseMaxSpeed, distanceToTarget);

        RotateTowardsTarget(dir);

        Vector3 desiredMomentum = ComputeDesiredMomentum(dir, current, effectiveMaxSpeed, distanceToTarget);
        droneController.AddMomentumRelativeToVisual(desiredMomentum, false);
    }

    private float CalculateEffectiveMaxSpeed(float baseMaxSpeed, float distance) {
        if (distance >= maxSpeedDistance) {
            return baseMaxSpeed;
        }
        
        if (distance <= minSpeedDistance) {
            return baseMaxSpeed * minSpeedMultiplier;
        }
        
        float t = (distance - minSpeedDistance) / (maxSpeedDistance - minSpeedDistance);
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, 1f, t);
        
        return baseMaxSpeed * speedMultiplier;
    }

    private void RotateTowardsTarget(Vector3 targetDirection) {
        droneController.SetVisualRotation(targetDirection, rotationSpeed);
    }

    private Vector3 ComputeDesiredMomentum(Vector3 dir, Vector3 current, float maxSpeed, float distanceToTarget) {
        float forwardSpeed = Vector3.Dot(current, dir);

        if (forwardSpeed >= maxSpeed)
        {
            if (distanceToTarget < minSpeedDistance && forwardSpeed > maxSpeed * 0.5f) {
                return -dir * 5f * Time.deltaTime;
            }
            return Vector3.zero;
        }

        float accel = 10f;

        if (distanceToTarget < minSpeedDistance * 1.5f) {
            accel = 5f;
        }

        Vector3 accelVector = dir * accel * Time.deltaTime;

        Vector3 predicted = current + accelVector;
        float predictedForward = Vector3.Dot(predicted, dir);

        if (predictedForward > maxSpeed) {
            float extra = predictedForward - maxSpeed;
            accelVector -= dir * extra;
        }

        return accelVector;
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

    public Transform GetCurrentTarget() {
        return target;
    }
}
