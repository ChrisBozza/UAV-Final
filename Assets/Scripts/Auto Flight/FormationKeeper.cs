using UnityEngine;

public class FormationKeeper : MonoBehaviour
{
    [Header("Drone References (Set by AutoSwarm)")]
    private GameObject leaderDrone;
    private GameObject leftWingDrone;
    private GameObject rightWingDrone;

    [Header("Formation Settings")]
    private TriangleFormationGenerator formationGenerator;
    [SerializeField] private float correctionStrength = 2f;
    [SerializeField] private float maxCorrectionForce = 1f;

    [Header("Leader Slowdown")]
    [SerializeField] private bool enableLeaderSlowdown = true;
    [SerializeField] private float slowdownThreshold = 4f;
    [SerializeField] private float slowdownStrength = 3f;
    [SerializeField] private float formationImmunityDuration = 2f;

    [Header("Control")]
    [SerializeField] private bool enableFormationKeeping = true;

    private DroneComputer leaderComputer;
    private DroneComputer leftWingComputer;
    private DroneComputer rightWingComputer;

    private Transform currentCheckpoint;
    private Transform nextCheckpoint;

    private float lastCheckpointTime;
    private bool isInSlowdownImmunity;

    public void Initialize(GameObject leader, GameObject leftWing, GameObject rightWing, TriangleFormationGenerator generator)
    {
        leaderDrone = leader;
        leftWingDrone = leftWing;
        rightWingDrone = rightWing;
        formationGenerator = generator;

        if (leaderDrone != null)
            leaderComputer = leaderDrone.GetComponent<DroneComputer>();
        
        if (leftWingDrone != null)
            leftWingComputer = leftWingDrone.GetComponent<DroneComputer>();
        
        if (rightWingDrone != null)
            rightWingComputer = rightWingDrone.GetComponent<DroneComputer>();
    }

    public void UpdateDroneAssignments(GameObject leader, GameObject leftWing, GameObject rightWing)
    {
        leaderDrone = leader;
        leftWingDrone = leftWing;
        rightWingDrone = rightWing;

        if (leaderDrone != null)
            leaderComputer = leaderDrone.GetComponent<DroneComputer>();
        
        if (leftWingDrone != null)
            leftWingComputer = leftWingDrone.GetComponent<DroneComputer>();
        
        if (rightWingDrone != null)
            rightWingComputer = rightWingDrone.GetComponent<DroneComputer>();
    }

    void FixedUpdate()
    {
        if (!enableFormationKeeping || formationGenerator == null) return;
        if (leaderDrone == null || leftWingDrone == null || rightWingDrone == null) return;
        if (currentCheckpoint == null || nextCheckpoint == null) return;

        UpdateSlowdownImmunity();
        ApplyFormationCorrections();
    }

    private void UpdateSlowdownImmunity()
    {
        if (Time.time - lastCheckpointTime < formationImmunityDuration)
        {
            isInSlowdownImmunity = true;
        }
        else
        {
            isInSlowdownImmunity = false;
        }
    }

    public void OnCheckpointReached()
    {
        lastCheckpointTime = Time.time;
        isInSlowdownImmunity = true;
    }

    public void UpdateFormationTarget(Transform current, Transform next)
    {
        currentCheckpoint = current;
        nextCheckpoint = next;
    }

    public void SetFormationKeepingEnabled(bool enabled)
    {
        enableFormationKeeping = enabled;
    }

    private void ApplyFormationCorrections()
    {
        if (isInSlowdownImmunity) return;

        Vector3 leaderPosition = leaderDrone.transform.position;
        Vector3 directionToNext = nextCheckpoint != null 
            ? (nextCheckpoint.position - leaderPosition).normalized 
            : leaderDrone.transform.forward;

        Vector3[] idealPositions = formationGenerator.GenerateFormationPositions(
            leaderPosition, 
            leaderPosition + directionToNext * 10f
        );

        ApplyCorrectionToDrone(leftWingDrone, leftWingComputer, idealPositions[1]);
        ApplyCorrectionToDrone(rightWingDrone, rightWingComputer, idealPositions[2]);

        if (enableLeaderSlowdown)
        {
            ApplyLeaderSlowdown();
        }
    }

    private void ApplyLeaderSlowdown()
    {
        if (isInSlowdownImmunity) return;
        if (leaderDrone == null || leftWingDrone == null || rightWingDrone == null) return;
        if (leaderComputer == null) return;

        Vector3 leaderPos = leaderDrone.transform.position;
        Vector3 leftWingPos = leftWingDrone.transform.position;
        Vector3 rightWingPos = rightWingDrone.transform.position;

        float leftWingDistance = Vector3.Distance(leaderPos, leftWingPos);
        float rightWingDistance = Vector3.Distance(leaderPos, rightWingPos);

        float maxLagDistance = Mathf.Max(leftWingDistance, rightWingDistance);

        if (maxLagDistance > slowdownThreshold)
        {
            float excessDistance = maxLagDistance - slowdownThreshold;
            float dragMultiplier = Mathf.Clamp01(excessDistance / slowdownThreshold);

            Vector3 leaderVelocity = leaderComputer.GetVelocity();
            Vector3 dragForce = -leaderVelocity.normalized * dragMultiplier * slowdownStrength * Time.fixedDeltaTime;

            leaderComputer.AddMomentum(dragForce);
        }
    }

    private void ApplyCorrectionToDrone(GameObject drone, DroneComputer droneComputer, Vector3 idealPosition)
    {
        if (drone == null || droneComputer == null) return;

        Vector3 currentPosition = drone.transform.position;
        Vector3 offset = idealPosition - currentPosition;

        float horizontalOffsetMagnitude = new Vector3(offset.x, 0f, offset.z).magnitude;

        if (horizontalOffsetMagnitude > 0.5f)
        {
            Vector3 correctionForce = offset * correctionStrength * Time.fixedDeltaTime;
            correctionForce = Vector3.ClampMagnitude(correctionForce, maxCorrectionForce * Time.fixedDeltaTime);

            droneComputer.AddMomentum(correctionForce);
        }
    }

    void OnDrawGizmos()
    {
        if (!enableFormationKeeping || leaderDrone == null) return;
        if (formationGenerator == null || nextCheckpoint == null) return;

        Vector3 leaderPosition = leaderDrone.transform.position;
        Vector3 directionToNext = (nextCheckpoint.position - leaderPosition).normalized;

        Vector3[] idealPositions = formationGenerator.GenerateFormationPositions(
            leaderPosition,
            leaderPosition + directionToNext * 10f
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(idealPositions[0], 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(idealPositions[1], 0.3f);
        Gizmos.DrawWireSphere(idealPositions[2], 0.3f);

        if (leftWingDrone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftWingDrone.transform.position, idealPositions[1]);
        }

        if (rightWingDrone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightWingDrone.transform.position, idealPositions[2]);
        }
    }
}
