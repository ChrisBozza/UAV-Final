using UnityEngine;
using System.Collections;

public class DroneController : MonoBehaviour {
    [SerializeField] Animator blade1;
    [SerializeField] Animator blade2;
    [SerializeField] Animator blade3;
    [SerializeField] Animator blade4;

    Vector3 velocity;
    Vector3 rotationVelocity; // Rotation in degrees per frame
    float stabilizationStrength = 2f;

    void Start() {
        StartBladeAnimation();
    }

    void Update() {
        ApplyMovement();
    }

    void StartBladeAnimation() {
        if (blade1) blade1.SetBool("Active", true);
        if (blade2) blade2.SetBool("Active", true);
        if (blade3) blade3.SetBool("Active", true);
        if (blade4) blade4.SetBool("Active", true);
    }

    void ApplyMovement() {
        // Apply Movement
        if (velocity.sqrMagnitude > 0.0001f)
            transform.position += velocity * Time.deltaTime;

        // Apply Rotation
        if (rotationVelocity.sqrMagnitude > 0.0001f) {
            transform.Rotate(rotationVelocity, Space.Self);
            rotationVelocity = Vector3.zero;
        }

        // Stabalize
        if (velocity.sqrMagnitude < 0.01f) {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }

    public Vector3 GetMomentum() {
        return velocity;
    }

    public void SetMomentum(Vector3 momentum) {
        velocity = momentum;
    }

    public void AddMomentum(Vector3 relMomentum) {
        Vector3 absMomentum  = transform.TransformDirection(relMomentum);
        velocity += absMomentum;
    }

    public void AddRotation(Vector3 relRotation) {
        Vector3 absRotation = transform.TransformDirection(relRotation);
        rotationVelocity += absRotation;
    }

    public IEnumerator CalculateMovement(Vector3 targetPosition) {
        while ((transform.position - targetPosition).sqrMagnitude > 0.01f) {
            Vector3 direction = (targetPosition - transform.position).normalized;
            velocity = direction;
            yield return null;
        }

        velocity = Vector3.zero;
    }
}
