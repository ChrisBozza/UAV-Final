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
    [SerializeField] FormationKeeper formationKeeper;

    [Header("Special Behaviors")]
    [SerializeField] CheckpointBehaviorHandler behaviorHandler;
    [SerializeField] float hoverHeightAboveLanding = 2f;
    [SerializeField] float stabilizationTime = 1f;
    [SerializeField] float landingStabilizationRadius = 1f;
    [SerializeField] float landingStabilizationTime = 1f;
    [SerializeField] float pauseAfterLandingDuration = 3f;

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
        // string line = string.Join(", ", checkpoints.Select(c => c.name));
        // Debug.Log(line);

        droneComputer1 = drone1.GetComponent<DroneComputer>();
        droneComputer2 = drone2.GetComponent<DroneComputer>();
        droneComputer3 = drone3.GetComponent<DroneComputer>();

        allDrones = new DroneComputer[] { droneComputer1, droneComputer2, droneComputer3 };

        if (formationGenerator == null)
        {
            formationGenerator = gameObject.AddComponent<TriangleFormationGenerator>();
        }

        if (formationKeeper == null)
        {
            formationKeeper = gameObject.AddComponent<FormationKeeper>();
        }
        formationKeeper.Initialize(drone1, drone2, drone3, formationGenerator);

        if (behaviorHandler == null)
        {
            behaviorHandler = gameObject.AddComponent<CheckpointBehaviorHandler>();
        }

        StartCoroutine(MissionHandler());
    }

    public void ReassignDroneRoles(GameObject newLeader, GameObject newLeftWing, GameObject newRightWing)
    {
        drone1 = newLeader;
        drone2 = newLeftWing;
        drone3 = newRightWing;

        droneComputer1 = drone1.GetComponent<DroneComputer>();
        droneComputer2 = drone2.GetComponent<DroneComputer>();
        droneComputer3 = drone3.GetComponent<DroneComputer>();

        allDrones = new DroneComputer[] { droneComputer1, droneComputer2, droneComputer3 };

        if (formationKeeper != null)
        {
            formationKeeper.UpdateDroneAssignments(drone1, drone2, drone3);
        }
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

        if (formationKeeper != null)
        {
            formationKeeper.UpdateFormationTarget(currentCheckpoint, nextCheckpoint);
            formationKeeper.SetFormationKeepingEnabled(true);
        }

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        if (formationKeeper != null)
        {
            formationKeeper.OnCheckpointReached();
            formationKeeper.SetFormationKeepingEnabled(false);
        }
    }

    private IEnumerator HandleTakeoff(CheckpointBehaviorHandler.CheckpointInfo checkpointInfo, Transform nextCheckpoint)
    {
        PowerOnAllDrones();

        float targetHeight = checkpointInfo.takeoffCheckpoint.position.y;

        Transform takeoffTarget1 = behaviorHandler.CreateIndividualTakeoffPosition(drone1.transform.position, targetHeight, 0);
        Transform takeoffTarget2 = behaviorHandler.CreateIndividualTakeoffPosition(drone2.transform.position, targetHeight, 1);
        Transform takeoffTarget3 = behaviorHandler.CreateIndividualTakeoffPosition(drone3.transform.position, targetHeight, 2);

        SetIndividualTargets(takeoffTarget1, takeoffTarget2, takeoffTarget3);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        if (formationKeeper != null)
        {
            formationKeeper.OnCheckpointReached();
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

        yield return StabilizeForLanding(hoverPositions);

        SetIndividualTargets(checkpointInfo.landingPositions[0], checkpointInfo.landingPositions[1], checkpointInfo.landingPositions[2]);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        PowerOffAllDrones();

        yield return new WaitForSeconds(pauseAfterLandingDuration);

        SetLowSpeedMode(false);
    }

    private IEnumerator StabilizeForLanding(Transform[] hoverPositions)
    {
        float[] timeStablePerDrone = new float[3];

        while (true)
        {
            bool allStable = true;

            for (int i = 0; i < 3; i++)
            {
                GameObject drone = (i == 0) ? drone1 : (i == 1) ? drone2 : drone3;
                Transform hoverPos = hoverPositions[i];

                float distance = Vector3.Distance(drone.transform.position, hoverPos.position);

                if (distance <= landingStabilizationRadius)
                {
                    timeStablePerDrone[i] += Time.deltaTime;

                    if (timeStablePerDrone[i] < landingStabilizationTime)
                    {
                        allStable = false;
                    }
                }
                else
                {
                    timeStablePerDrone[i] = 0f;
                    allStable = false;
                }
            }

            if (allStable)
            {
                break;
            }

            yield return null;
        }
    }

    private void PowerOffAllDrones()
    {
        foreach (DroneComputer drone in allDrones)
        {
            if (drone != null && drone.droneController != null)
            {
                drone.droneController.PowerOffEngine();
            }
        }
    }

    private void PowerOnAllDrones()
    {
        foreach (DroneComputer drone in allDrones)
        {
            if (drone != null && drone.droneController != null)
            {
                drone.droneController.PowerOnEngine();
            }
        }
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
