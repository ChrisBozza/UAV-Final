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
    [SerializeField] float hoverForce = 9.81f;
    public float maxSpeed = 3f;

    [Header("Visual Rotation")]
    public Transform droneRender;
    public float rotationMatchThreshold = 1f;

    Rigidbody rb;

    Vector3 targetVelocity;
    Vector3 rotationVelocity;

    Vector3 enginePower;

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

    void StartBladeAnimation() {
        if (blade1) blade1.SetBool("Active", true);
        if (blade2) blade2.SetBool("Active", true);
        if (blade3) blade3.SetBool("Active", true);
        if (blade4) blade4.SetBool("Active", true);
    }

    void ApplyHoverForce() {
        if (!rb) return;
        rb.AddForce(Vector3.up * hoverForce * rb.mass, ForceMode.Force);
    }

    void ApplyMovement() {
        if (!rb) return;

        Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
        Vector3 force = velocityDifference * forceMultiplier;
        rb.AddForce(force, ForceMode.Force);

        if (rotationVelocity.sqrMagnitude > 0.0001f) {
            Quaternion delta = Quaternion.Euler(rotationVelocity * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * delta);
            rotationVelocity = Vector3.zero;
        }

        enginePower = Vector3.zero;
    }

    public Vector3 GetMomentum() {
        return targetVelocity;
    }
    public Vector3 GetEnginePower() {
        return enginePower;
    }

    public void SetMomentum(Vector3 momentum) {
        targetVelocity = momentum;
    }

    public void AddMomentum(Vector3 relMomentum) {
        enginePower += relMomentum;
        Vector3 absMomentum = transform.TransformDirection(relMomentum);
        targetVelocity += absMomentum;
        targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxSpeed);
    }

    public void AddRotation(Vector3 relRotation) {
        if (!RotationsMatch()) {
            SwitchActiveController();
        }
        
        Vector3 absRotation = transform.TransformDirection(relRotation);
        rotationVelocity += absRotation;
    }

    public void AddVisualRotation(Vector3 targetDirection, float rotationSpeed) {
        if (droneRender == null) return;

        Vector3 flatTargetDir = new Vector3(targetDirection.x, 0f, targetDirection.z);
        if (flatTargetDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(flatTargetDir, Vector3.up);
        
        float yaw = targetRotation.eulerAngles.y;
        float currentYaw = droneRender.localEulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, yaw, rotationSpeed * Time.deltaTime);
        
        droneRender.localRotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    public void SwitchActiveController() {
        if (droneRender == null) return;
        
        rb.MoveRotation(droneRender.rotation);
    }

    bool RotationsMatch() {
        if (droneRender == null) return true;
        
        float angle = Quaternion.Angle(transform.rotation, droneRender.rotation);
        return angle < rotationMatchThreshold;
    }

    // Currently unused???
    // Drone bugs rn out idk why
    public IEnumerator CalculateMovement(Vector3 targetPosition) {
        while ((transform.position - targetPosition).sqrMagnitude > 0.01f) {
            Vector3 direction = (targetPosition - transform.position).normalized;
            targetVelocity = direction;
            yield return null;
        }

        targetVelocity = Vector3.zero;
    }
}
