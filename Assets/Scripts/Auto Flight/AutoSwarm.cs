using System.Collections;
using System.Linq;
using UnityEngine;

public class AutoSwarm: MonoBehaviour
{
    [SerializeField] GameObject drone1;
    [SerializeField] GameObject drone2;
    [SerializeField] GameObject drone3;
    DroneComputer droneComputer1;
    DroneComputer droneComputer2;
    DroneComputer droneComputer3;

    [SerializeField] GameObject checkpointParent;
    private GameObject[] checkpoints;

    [Header("Formation System")]
    [SerializeField] TriangleFormationGenerator formationGenerator;

    [Header("Special Behaviors")]
    [SerializeField] CheckpointBehaviorHandler behaviorHandler;
    [SerializeField] float hoverHeightAboveLanding = 2f;
    [SerializeField] float stabilizationTime = 1f;

    [Header("Prediction System")]
    [SerializeField] DronePathPredictor pathPredictor;

    public bool swarmActive = true;

    private DroneComputer[] allDrones;

    void Start()
    {
        checkpoints = new GameObject[checkpointParent.transform.childCount];
        for (int i = 0; i < checkpointParent.transform.childCount; i++) {
            checkpoints[i] = checkpointParent.transform.GetChild(i).gameObject;
        }
        string line = string.Join(", ", checkpoints.Select(c => c.name));
        Debug.Log(line);

        droneComputer1 = drone1.GetComponent<DroneComputer>();
        droneComputer2 = drone2.GetComponent<DroneComputer>();
        droneComputer3 = drone3.GetComponent<DroneComputer>();

        allDrones = new DroneComputer[] { droneComputer1, droneComputer2, droneComputer3 };

        if (formationGenerator == null)
        {
            formationGenerator = gameObject.AddComponent<TriangleFormationGenerator>();
        }

        if (behaviorHandler == null)
        {
            behaviorHandler = gameObject.AddComponent<CheckpointBehaviorHandler>();
        }

        StartCoroutine(MissionHandler());
    }

    private IEnumerator MissionHandler() {

        SetAutoPilot(true);

        for (int i = 0; i < checkpoints.Length; i++) {
            Transform currentCheckpoint = checkpoints[i].transform;
            Transform nextCheckpoint = (i + 1 < checkpoints.Length) ? checkpoints[i + 1].transform : null;
            
            CheckpointBehaviorHandler.CheckpointInfo checkpointInfo = behaviorHandler.AnalyzeCheckpoint(currentCheckpoint);

            switch (checkpointInfo.type)
            {
                case CheckpointBehaviorHandler.CheckpointType.Takeoff:
                    yield return HandleTakeoff(checkpointInfo, nextCheckpoint);
                    break;

                case CheckpointBehaviorHandler.CheckpointType.Landing:
                    yield return HandleLanding(checkpointInfo);
                    break;

                case CheckpointBehaviorHandler.CheckpointType.Normal:
                default:
                    yield return HandleNormalCheckpoint(currentCheckpoint, nextCheckpoint);
                    break;
            }
        }

        yield return null;
    }

    private IEnumerator HandleNormalCheckpoint(Transform currentCheckpoint, Transform nextCheckpoint)
    {
        SetNewSwarmTarget(currentCheckpoint, nextCheckpoint);

        while (!SwarmReachedTarget()) {
            yield return null;
        }
    }

    private IEnumerator HandleTakeoff(CheckpointBehaviorHandler.CheckpointInfo checkpointInfo, Transform nextCheckpoint)
    {
        float targetHeight = checkpointInfo.takeoffCheckpoint.position.y;

        Transform takeoffTarget1 = behaviorHandler.CreateIndividualTakeoffPosition(drone1.transform.position, targetHeight, 0);
        Transform takeoffTarget2 = behaviorHandler.CreateIndividualTakeoffPosition(drone2.transform.position, targetHeight, 1);
        Transform takeoffTarget3 = behaviorHandler.CreateIndividualTakeoffPosition(drone3.transform.position, targetHeight, 2);

        SetIndividualTargets(takeoffTarget1, takeoffTarget2, takeoffTarget3);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        yield return StabilizeSwarm(nextCheckpoint);
    }

    private IEnumerator StabilizeSwarm(Transform nextCheckpoint)
    {
        if (nextCheckpoint != null)
        {
            Vector3 directionToNext = (nextCheckpoint.position - GetAverageSwarmPosition()).normalized;
            Vector3 flatDirection = new Vector3(directionToNext.x, 0f, directionToNext.z).normalized;

            float elapsedTime = 0f;
            while (elapsedTime < stabilizationTime)
            {
                foreach (DroneComputer drone in allDrones)
                {
                    drone.RotateTowardsDirection(flatDirection);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(stabilizationTime);
        }
    }

    private IEnumerator HandleLanding(CheckpointBehaviorHandler.CheckpointInfo checkpointInfo)
    {
        if (checkpointInfo.landingPositions.Length < 3)
        {
            Debug.LogError("Landing checkpoint must have 3 children!");
            yield break;
        }

        Transform[] hoverPositions = behaviorHandler.CreateHoverPositions(checkpointInfo.landingPositions, hoverHeightAboveLanding);

        SetLowSpeedMode(true);

        SetIndividualTargets(hoverPositions[0], hoverPositions[1], hoverPositions[2]);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        SetIndividualTargets(checkpointInfo.landingPositions[0], checkpointInfo.landingPositions[1], checkpointInfo.landingPositions[2]);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        SetLowSpeedMode(false);
    }

    private void SetAutoPilot(bool status) {
        droneComputer1.autoPilot = status;
        droneComputer2.autoPilot = status;
        droneComputer3.autoPilot = status;
    }

    private void SetNewSwarmTarget(Transform currentCheckpoint, Transform nextCheckpoint) {
        Transform[] formationTargets = formationGenerator.GenerateFormationTransforms(currentCheckpoint, nextCheckpoint);
        
        droneComputer1.SetTarget(formationTargets[0]);
        droneComputer2.SetTarget(formationTargets[1]);
        droneComputer3.SetTarget(formationTargets[2]);
        
        if (pathPredictor != null) {
            pathPredictor.UpdatePrediction(currentCheckpoint);
        }
    }

    private void SetIndividualTargets(Transform target1, Transform target2, Transform target3)
    {
        droneComputer1.SetTarget(target1);
        droneComputer2.SetTarget(target2);
        droneComputer3.SetTarget(target3);
    }

    private bool SwarmReachedTarget() {
        return droneComputer1.reachedTarget && droneComputer2.reachedTarget && droneComputer3.reachedTarget;
    }

    private Vector3 GetAverageSwarmPosition()
    {
        Vector3 sum = drone1.transform.position + drone2.transform.position + drone3.transform.position;
        return sum / 3f;
    }

    private void SetLowSpeedMode(bool enable)
    {
        float speedMultiplier = enable ? 0.5f : 1f;
        
        foreach (DroneComputer drone in allDrones)
        {
            drone.droneController.maxSpeed = drone.droneController.maxSpeed * speedMultiplier;
        }
    }

}
