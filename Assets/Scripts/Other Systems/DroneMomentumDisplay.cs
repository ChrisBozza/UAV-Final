using UnityEngine;
using TMPro;

public class DroneMomentumDisplay : MonoBehaviour
{
    [SerializeField] DroneController droneController;
    [SerializeField] TMP_Text momentumText;
    [SerializeField] TMP_Text velocityText;
    [SerializeField] TMP_Text enginePowerText;

    void Update()
    {
        if (!droneController) return;

        Vector3 momentum = droneController.GetMomentum();
        Vector3 enginePower = droneController.GetEnginePower();
        
        Rigidbody rb = droneController.GetComponent<Rigidbody>();
        Vector3 velocity = rb ? rb.linearVelocity : Vector3.zero;

        if (momentumText)
        {
            momentumText.text = $"Target Velocity:\n" +
                                $"X:   {momentum.x,7:F3}\n" +
                                $"Y:   {momentum.y,7:F3}\n" +
                                $"Z:   {momentum.z,7:F3}\n" +
                                $"Mag: {momentum.magnitude,7:F3}";
        }

        if (velocityText)
        {
            velocityText.text = $"Actual Velocity:\n" +
                                $"X:   {velocity.x,7:F3}\n" +
                                $"Y:   {velocity.y,7:F3}\n" +
                                $"Z:   {velocity.z,7:F3}\n" +
                                $"Mag: {velocity.magnitude,7:F3}";
        }

        if (enginePowerText)
        {
            enginePowerText.text = $"Engine Power:\n" +
                                   $"X:   {enginePower.x,7:F3}\n" +
                                   $"Y:   {enginePower.y,7:F3}\n" +
                                   $"Z:   {enginePower.z,7:F3}\n" +
                                   $"Mag: {enginePower.magnitude,7:F3}";
        }
    }
}
