using UnityEngine;

public class DroneStabilization : MonoBehaviour
{
    [Header("Stabilization Settings")]
    [SerializeField] float stabilizationSpeed = 5f;
    [SerializeField] float angularDamping = 0.9f;
    [SerializeField] bool usePhysicsStabilization = true;

    [Header("Emergency Reset")]
    [SerializeField] float maxTiltAngle = 60f;
    [SerializeField] float emergencyResetSpeed = 10f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!rb) return;

        DampenAngularVelocity();

        float currentTilt = GetTiltAngle();

        if (currentTilt > maxTiltAngle)
        {
            EmergencyReset();
        }
        else if (usePhysicsStabilization)
        {
            ApplyStabilizationTorque();
        }
        else
        {
            ApplyDirectStabilization();
        }
    }

    void DampenAngularVelocity()
    {
        rb.angularVelocity *= angularDamping;
    }

    void ApplyStabilizationTorque()
    {
        Vector3 currentUp = transform.up;
        Vector3 targetUp = Vector3.up;

        Vector3 axis = Vector3.Cross(currentUp, targetUp);
        float angle = Vector3.Angle(currentUp, targetUp);

        if (axis.sqrMagnitude > 0.001f && angle > 0.1f)
        {
            Vector3 torque = axis.normalized * angle * stabilizationSpeed;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }

    void ApplyDirectStabilization()
    {
        Vector3 euler = rb.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;

        Quaternion targetRotation = Quaternion.Euler(euler);
        Quaternion newRotation = Quaternion.Lerp(rb.rotation, targetRotation, stabilizationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRotation);
    }

    void EmergencyReset()
    {
        Vector3 euler = rb.rotation.eulerAngles;
        euler.x = 0;
        euler.z = 0;

        Quaternion targetRotation = Quaternion.Euler(euler);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, emergencyResetSpeed * Time.fixedDeltaTime));

        rb.angularVelocity = Vector3.zero;
    }

    float GetTiltAngle()
    {
        return Vector3.Angle(transform.up, Vector3.up);
    }
}
