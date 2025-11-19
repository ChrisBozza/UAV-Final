using UnityEngine;
using System.Collections;

public class DroneController : MonoBehaviour {
    [Header("Blade Animators")]
    [SerializeField] Animator blade1;
    [SerializeField] Animator blade2;
    [SerializeField] Animator blade3;
    [SerializeField] Animator blade4;

    [Header("Physics Settings")]
    [SerializeField] float forceMultiplier = 100f;
    [SerializeField] float crashGravityForce = 9.81f;
    public float maxSpeed = 3f;

    [Header("Drone References")]
    public Transform visualDrone;
    public DroneCrashDetection droneCrashDetection;

    Rigidbody rb;

    Vector3 targetVelocity;
    Vector3 enginePower;
    Vector3 enginePowerWorldSpace;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        if (rb) {
            rb.useGravity = false;
        }
    }

    void Start() {
        StartBladeAnimation();
    }

    void FixedUpdate() {
        ApplyMovement();
    }

    void LateUpdate() {
        enginePower = Vector3.zero;
        enginePowerWorldSpace = Vector3.zero;
    }

    void StartBladeAnimation() {
        if (blade1) blade1.speed = 1f;
        if (blade2) blade2.speed = 1f;
        if (blade3) blade3.speed = 1f;
        if (blade4) blade4.speed = 1f;
    }

    public void StopBladeAnimation() {
        if (blade1) blade1.speed = 0f;
        if (blade2) blade2.speed = 0f;
        if (blade3) blade3.speed = 0f;
        if (blade4) blade4.speed = 0f;
    }

    void ApplyMovement() {
        if (!rb) return;
        if (droneCrashDetection.HasCrashed()) {
            ApplyCrashedMovement();
            return;
        }

        Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
        Vector3 force = velocityDifference * forceMultiplier;
        rb.AddForce(force, ForceMode.Force);
    }

    void ApplyCrashedMovement() {
        rb.AddForce(Vector3.down * crashGravityForce, ForceMode.Acceleration);
    }

    public Vector3 GetMomentum() {
        return targetVelocity;
    }
    
    public Vector3 GetEnginePower() {
        if (droneCrashDetection.HasCrashed()) return Vector3.zero;
        return enginePower;
    }

    public Vector3 GetEnginePowerWorldSpace() {
        if (droneCrashDetection.HasCrashed()) return Vector3.zero;
        return enginePowerWorldSpace;
    }

    public void SetMomentum(Vector3 momentum) {
        targetVelocity = momentum;
    }

    public void AddMomentum(Vector3 relMomentum) {
        enginePower += relMomentum;
        Vector3 absMomentum = transform.TransformDirection(relMomentum);
        enginePowerWorldSpace += absMomentum;

        targetVelocity += absMomentum;
        targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxSpeed);
    }

    public void AddMomentumRelativeToVisual(Vector3 relMomentum, bool useVisualRotation) {
        enginePower += relMomentum;
        
        Vector3 absMomentum;
        if (useVisualRotation && visualDrone != null) {
            absMomentum = visualDrone.TransformDirection(relMomentum);
        } else {
            absMomentum = transform.TransformDirection(relMomentum);
        }
        
        enginePowerWorldSpace += absMomentum;
        targetVelocity += absMomentum;
        targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxSpeed);
    }

    public void SetVisualRotation(Vector3 targetDirection, float rotationSpeed) {
        if (visualDrone == null) return;
        if (droneCrashDetection.HasCrashed()) return;

        Vector3 flatTargetDir = new Vector3(targetDirection.x, 0f, targetDirection.z);
        if (flatTargetDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(flatTargetDir, Vector3.up);
        
        float yaw = targetRotation.eulerAngles.y;
        float currentYaw = visualDrone.eulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, yaw, rotationSpeed * Time.deltaTime);
        
        visualDrone.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    public Vector3 GetPosition() {
        return transform.position;
    }

    public Quaternion GetRotation() {
        return transform.rotation;
    }

    public Vector3 GetVelocity() {
        return rb ? rb.linearVelocity : Vector3.zero;
    }
}
