using UnityEngine;

public class DroneCrashDetection : MonoBehaviour
{
    [Header("Crash Settings")]
    [SerializeField] float crashSpeedThreshold = 2f;
    
    [Header("References")]
    [SerializeField] DroneController droneController;
    
    bool hasCrashed = false;
    
    void OnCollisionEnter(Collision collision)
    {
        
        float impactSpeed = collision.relativeVelocity.magnitude;
        
        if (impactSpeed >= crashSpeedThreshold)
        {
            HandleCrash(collision, impactSpeed);
        }
    }
    
    void HandleCrash(Collision collision, float impactSpeed)
    {
        if (collision.gameObject.name == "DroneLandingPad") {
            droneController.SetMomentum(Vector3.zero);
            return;
        }

        hasCrashed = true;
        Debug.Log($"Drone crashed! Impact speed: {impactSpeed:F2} m/s, Hit object: {collision.gameObject.name}");
        droneController.SetMomentum(Vector3.zero);
        droneController.StopBladeAnimation();

    }

    public bool HasCrashed()
    {
        return hasCrashed;
    }
}
