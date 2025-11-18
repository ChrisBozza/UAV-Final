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

    Rigidbody rb;

    Vector3 targetVelocity;
    Vector3 rotationVelocity;

    Vector3 enginePower;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void Start() {
        StartBladeAnimation();
    }

    void FixedUpdate() {
        ApplyHoverForce();
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

        if (targetVelocity.sqrMagnitude > 0.0001f) {
            Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
            Vector3 force = velocityDifference * forceMultiplier;
            rb.AddForce(force, ForceMode.Force);
        }

        if (rotationVelocity.sqrMagnitude > 0.0001f) {
            Quaternion delta = Quaternion.Euler(rotationVelocity * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * delta);
            rotationVelocity = Vector3.zero;
        }

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
    }

    public void AddRotation(Vector3 relRotation) {
        Vector3 absRotation = transform.TransformDirection(relRotation);
        rotationVelocity += absRotation;
    }

    public IEnumerator CalculateMovement(Vector3 targetPosition) {
        while ((transform.position - targetPosition).sqrMagnitude > 0.01f) {
            Vector3 direction = (targetPosition - transform.position).normalized;
            targetVelocity = direction;
            yield return null;
        }

        targetVelocity = Vector3.zero;
    }
}
