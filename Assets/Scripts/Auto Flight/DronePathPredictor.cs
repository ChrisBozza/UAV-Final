using UnityEngine;

public class DronePathPredictor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneController droneController;
    [SerializeField] private GameObject predictionPrefab;

    [Header("Debug Info")]
    [SerializeField] private float simpleAngle;
    [SerializeField] private float simpleDistance;
    [SerializeField] private float currentAngle;
    [SerializeField] private float predictionAngle;

    private Transform currentTarget;
    private GameObject currentPredictionMarker;

    public void UpdatePrediction(Transform newTarget)
    {
        if (droneController == null || newTarget == null)
            return;

        currentTarget = newTarget;
        CalculatePrediction();
        SpawnPredictionMarker();
    }

    private void CalculatePrediction()
    {
        Vector3 dronePosition = droneController.GetPosition();
        Vector3 targetPosition = currentTarget.position;

        Vector3 toTarget = targetPosition - dronePosition;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);

        simpleDistance = toTargetFlat.magnitude;
        simpleAngle = Mathf.Atan2(toTargetFlat.x, toTargetFlat.z) * Mathf.Rad2Deg;

        Vector3 currentVelocity = droneController.GetVelocity();
        Vector3 currentVelocityFlat = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        if (currentVelocityFlat.magnitude > 0.1f)
        {
            currentAngle = Mathf.Atan2(currentVelocityFlat.x, currentVelocityFlat.z) * Mathf.Rad2Deg;
        }
        else
        {
            currentAngle = 0f;
        }

        predictionAngle = simpleAngle + currentAngle;
    }

    private void SpawnPredictionMarker()
    {
        if (predictionPrefab == null)
            return;

        if (currentPredictionMarker != null)
        {
            Destroy(currentPredictionMarker);
        }

        Vector3 dronePosition = droneController.GetPosition();

        float angleInRadians = predictionAngle * Mathf.Deg2Rad;
        Vector3 predictionDirection = new Vector3(Mathf.Sin(angleInRadians), 0f, Mathf.Cos(angleInRadians));
        Vector3 predictionPosition = dronePosition + predictionDirection * simpleDistance;

        currentPredictionMarker = Instantiate(predictionPrefab, predictionPosition, Quaternion.identity);
    }

    public void ClearPrediction()
    {
        if (currentPredictionMarker != null)
        {
            Destroy(currentPredictionMarker);
            currentPredictionMarker = null;
        }
    }

    private void OnDestroy()
    {
        ClearPrediction();
    }
}
