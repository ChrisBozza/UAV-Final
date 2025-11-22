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
    Vector3? targetPosition;

    [Header("Speed Control Settings")]
    public float minSpeedDistance = 2f;
    public float maxSpeedDistance = 10f;
    public float minSpeedMultiplier = 0.3f;
    private float baseMaxSpeed;
    
    [Header("Formation Keeping")]
    public bool formationKeepingEnabled = false;
    public Vector3 formationOffset = Vector3.zero;
    public float formationCorrectionStrength = 2f;
    public float maxFormationCorrectionForce = 1f;
    private Transform formationLeader;


    void Start()
    {
        droneController = GetComponent<DroneController>();
        baseMaxSpeed = droneController.maxSpeed;
        droneReady = true;
        StartCoroutine(AutoFly());
        StartCoroutine(AutoCheck());
    }
    
    void FixedUpdate()
    {
        if (formationKeepingEnabled && formationLeader != null)
        {
            ApplyFormationCorrection();
        }
    }

    IEnumerator AutoFly() {
        while(!droneReady) yield return new WaitForEndOfFrame();
        
        while (true) {
            yield return new WaitForSeconds(0.1f);
            if (autoPilot && HasTarget()) {
                MoveTowardsPoint();
            }
        }
    }

    IEnumerator AutoCheck() {
        while(!droneReady) yield return new WaitForEndOfFrame();

        while(true) {
            yield return null;
            
            if (HasTarget())
            {
                Vector3 toTarget = GetTargetPosition() - droneController.GetPosition();
                float dist = toTarget.magnitude;

                if (dist < 2f) {
                    reachedTarget = true;
                }
                else if (dist > 3f) {
                    reachedTarget = false;
                }
            }
        }
    }

    private void MoveTowardsPoint() {

        Vector3 toTarget = GetTargetPosition() - droneController.GetPosition();
        float distanceToTarget = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;
        Vector3 current = droneController.GetMomentum();
        float currentMaxSpeed = droneController.maxSpeed;

        float effectiveMaxSpeed = CalculateEffectiveMaxSpeed(currentMaxSpeed, distanceToTarget);

        if (!reachedTarget) {
            RotateTowardsTarget(dir);
        }

        Vector3 desiredMomentum = ComputeDesiredMomentum(dir, current, effectiveMaxSpeed, distanceToTarget);
        droneController.AddMomentumWorld(desiredMomentum);
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

    public void RotateTowardsDirection(Vector3 direction) {
        if (!droneReady) return;
        droneController.SetVisualRotation(direction, rotationSpeed);
    }

    public void SetTarget(Transform t) {
        target = t;
        targetPosition = null;
        reachedTarget = false;
    }

    public void SetTargetPosition(Vector3 position)
    {
        target = null;
        targetPosition = position;
        reachedTarget = false;
    }

    public void ClearTarget()
    {
        target = null;
        targetPosition = null;
        reachedTarget = false;
    }

    public Transform GetCurrentTarget() {
        return target;
    }
    
    private bool HasTarget()
    {
        return target != null || targetPosition.HasValue;
    }
    
    private Vector3 GetTargetPosition()
    {
        if (target != null)
            return target.position;
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return droneController.GetPosition();
    }
    
    public Vector3 GetTargetDirection()
    {
        if (!HasTarget()) return transform.forward;
        Vector3 toTarget = GetTargetPosition() - GetPosition();
        Vector3 horizontalDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        return horizontalDirection.magnitude > 0.01f ? horizontalDirection.normalized : transform.forward;
    }
    
    public void PowerOnEngine()
    {
        if (!droneReady) return;
        droneController.PowerOnEngine();
    }
    
    public void PowerOffEngine()
    {
        if (!droneReady) return;
        droneController.PowerOffEngine();
    }
    
    public bool IsEnginePowered()
    {
        if (!droneReady) return false;
        return droneController.IsEnginePowered();
    }
    
    public void SetMaxSpeed(float speed)
    {
        if (!droneReady) return;
        droneController.maxSpeed = speed;
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        if (!droneReady) return;
        droneController.maxSpeed = baseMaxSpeed * multiplier;
    }
    
    public float GetMaxSpeed()
    {
        if (!droneReady) return 0f;
        return droneController.maxSpeed;
    }
    
    public Vector3 GetPosition()
    {
        if (!droneReady) return Vector3.zero;
        return droneController.GetPosition();
    }
    
    public Vector3 GetVelocity()
    {
        if (!droneReady) return Vector3.zero;
        return droneController.GetVelocity();
    }
    
    public void SetFormationKeepingEnabled(bool enabled)
    {
        formationKeepingEnabled = enabled;
    }
    
    public void SetFormationOffset(Vector3 offset, Transform leader)
    {
        formationOffset = offset;
        formationLeader = leader;
    }
    
    private void ApplyFormationCorrection()
    {
        if (formationLeader == null) return;
        
        Vector3 idealPosition = formationLeader.position + formationOffset;
        Vector3 currentPosition = droneController.GetPosition();
        Vector3 offset = idealPosition - currentPosition;
        
        float horizontalOffsetMagnitude = new Vector3(offset.x, 0f, offset.z).magnitude;
        
        if (horizontalOffsetMagnitude > 0.5f)
        {
            Vector3 correctionForce = offset * formationCorrectionStrength * Time.fixedDeltaTime;
            correctionForce = Vector3.ClampMagnitude(correctionForce, maxFormationCorrectionForce * Time.fixedDeltaTime);
            
            AddMomentum(correctionForce);
        }
    }
}
