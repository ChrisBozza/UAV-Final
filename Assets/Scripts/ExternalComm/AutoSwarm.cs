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
    private FormationKeeper formationKeeper1;
    private FormationKeeper formationKeeper2;
    private FormationKeeper formationKeeper3;

    [Header("Special Behaviors")]
    [SerializeField] CheckpointBehaviorHandler behaviorHandler;
    [SerializeField] float hoverHeightAboveLanding = 2f;
    [SerializeField] float stabilizationTime = 1f;
    [SerializeField] float landingStabilizationRadius = 1f;
    [SerializeField] float landingStabilizationTime = 1f;
    [SerializeField] float pauseAfterLandingDuration = 3f;

    [Header("Prediction System")]
    [SerializeField] DronePathPredictor pathPredictor;

    [Header("Packet Communication")]
    [SerializeField] bool usePacketCommunication = true;

    [Header("Checkpoint Optimization")]
    [SerializeField] float duplicateCheckpointThreshold = 0.1f;

    public bool swarmActive = true;

    private DroneComputer[] allDrones;
    private PacketReceiver packetReceiver1;
    private PacketReceiver packetReceiver2;
    private PacketReceiver packetReceiver3;
    private PacketReceiver autoSwarmReceiver;
    private System.Collections.Generic.List<Vector3> visitedCheckpointPositions = new System.Collections.Generic.List<Vector3>();

    void Start()
    {
        checkpoints = new GameObject[checkpointParent.transform.childCount];
        for (int i = 0; i < checkpointParent.transform.childCount; i++) {
            checkpoints[i] = checkpointParent.transform.GetChild(i).gameObject;
        }

        droneComputer1 = drone1.GetComponent<DroneComputer>();
        droneComputer2 = drone2.GetComponent<DroneComputer>();
        droneComputer3 = drone3.GetComponent<DroneComputer>();

        allDrones = new DroneComputer[] { droneComputer1, droneComputer2, droneComputer3 };

        packetReceiver1 = drone1.GetComponent<PacketReceiver>();
        packetReceiver2 = drone2.GetComponent<PacketReceiver>();
        packetReceiver3 = drone3.GetComponent<PacketReceiver>();
        autoSwarmReceiver = GetComponent<PacketReceiver>();

        if (usePacketCommunication)
        {
            if (packetReceiver1 == null || packetReceiver2 == null || packetReceiver3 == null)
            {
                Debug.LogWarning("[AutoSwarm] Packet communication enabled but not all drones have PacketReceiver. Falling back to direct access.");
                usePacketCommunication = false;
            }
            else if (autoSwarmReceiver == null)
            {
                Debug.LogWarning("[AutoSwarm] Packet communication enabled but AutoSwarm doesn't have PacketReceiver. Adding one.");
                autoSwarmReceiver = gameObject.AddComponent<PacketReceiver>();
                autoSwarmReceiver.receiverId = "AutoSwarm";
            }
            
            if (usePacketCommunication)
            {
                Debug.Log($"[AutoSwarm] Packet communication enabled. Receiver IDs: {packetReceiver1.receiverId}, {packetReceiver2.receiverId}, {packetReceiver3.receiverId}");
                Debug.Log($"[AutoSwarm] AutoSwarm receiver ID: {autoSwarmReceiver.receiverId}");
            }
        }

        if (formationGenerator == null)
        {
            formationGenerator = gameObject.AddComponent<TriangleFormationGenerator>();
        }

        InitializeFormationKeeping();

        if (behaviorHandler == null)
        {
            behaviorHandler = gameObject.AddComponent<CheckpointBehaviorHandler>();
        }

        StartCoroutine(DelayedMissionStart());
    }

    private IEnumerator DelayedMissionStart()
    {
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("[AutoSwarm] Starting mission...");
        StartCoroutine(MissionHandler());
    }

    private void InitializeFormationKeeping()
    {
        formationKeeper1 = drone1.GetComponent<FormationKeeper>();
        formationKeeper2 = drone2.GetComponent<FormationKeeper>();
        formationKeeper3 = drone3.GetComponent<FormationKeeper>();

        if (formationKeeper1 == null)
            formationKeeper1 = drone1.AddComponent<FormationKeeper>();
        if (formationKeeper2 == null)
            formationKeeper2 = drone2.AddComponent<FormationKeeper>();
        if (formationKeeper3 == null)
            formationKeeper3 = drone3.AddComponent<FormationKeeper>();

        formationKeeper1.SetFormationRole(FormationKeeper.FormationRole.Leader);
        formationKeeper2.SetFormationRole(FormationKeeper.FormationRole.LeftWing);
        formationKeeper3.SetFormationRole(FormationKeeper.FormationRole.RightWing);

        formationKeeper2.SetLeaderReference(droneComputer1);
        formationKeeper3.SetLeaderReference(droneComputer1);

        formationKeeper1.SetWingmenReferences(new DroneComputer[] { droneComputer2, droneComputer3 });
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

        InitializeFormationKeeping();
    }

    private IEnumerator MissionHandler() {

        SetAutoPilot(true);

        for (int i = 0; i < checkpoints.Length; i++) {
            Transform currentCheckpoint = checkpoints[i].transform;
            Transform nextCheckpoint = (i + 1 < checkpoints.Length) ? checkpoints[i + 1].transform : null;
            
            if (IsCheckpointAlreadyVisited(currentCheckpoint.position))
            {
                Debug.Log($"[AutoSwarm] Skipping checkpoint {i}/{checkpoints.Length - 1}: {currentCheckpoint.name} - Already visited this location!");
                continue;
            }
            
            CheckpointBehaviorHandler.CheckpointInfo checkpointInfo = behaviorHandler.AnalyzeCheckpoint(currentCheckpoint);

            Debug.Log($"[AutoSwarm] Processing checkpoint {i}/{checkpoints.Length - 1}: {currentCheckpoint.name} (Type: {checkpointInfo.type})");

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
            
            visitedCheckpointPositions.Add(currentCheckpoint.position);
            Debug.Log($"[AutoSwarm] Completed checkpoint {i}: {currentCheckpoint.name}");
        }

        yield return null;
    }

    private IEnumerator HandleNormalCheckpoint(Transform currentCheckpoint, Transform nextCheckpoint)
    {
        SetNewSwarmTarget(currentCheckpoint, nextCheckpoint);

        Vector3 directionToNext = nextCheckpoint != null 
            ? (nextCheckpoint.position - currentCheckpoint.position).normalized 
            : Vector3.forward;

        ConfigureFormation(directionToNext, true);

        while (!SwarmReachedTarget()) {
            yield return null;
        }

        ConfigureFormation(directionToNext, false);
    }

    private void ConfigureFormation(Vector3 directionToTarget, bool enable)
    {
        Vector3 right = Vector3.Cross(Vector3.up, directionToTarget).normalized;
        
        float formationWidth = formationGenerator.formationWidth;
        float formationDepth = formationGenerator.formationDepth;
        
        Vector3 leftWingOffset = new Vector3(formationWidth * 0.5f, 0f, formationDepth);
        Vector3 rightWingOffset = new Vector3(-formationWidth * 0.5f, 0f, formationDepth);
        
        if (usePacketCommunication)
        {
            string offsetData2 = $"{leftWingOffset.x},{leftWingOffset.y},{leftWingOffset.z}";
            string offsetData3 = $"{rightWingOffset.x},{rightWingOffset.y},{rightWingOffset.z}";
            
            SendPacketToDrone(packetReceiver2, "formation_offset", offsetData2);
            SendPacketToDrone(packetReceiver3, "formation_offset", offsetData3);
            
            string activeData = enable ? "true" : "false";
            SendPacketToDrone(packetReceiver1, "formation_active", activeData);
            SendPacketToDrone(packetReceiver2, "formation_active", activeData);
            SendPacketToDrone(packetReceiver3, "formation_active", activeData);
        }
        else
        {
            formationKeeper2.SetFormationOffset(leftWingOffset);
            formationKeeper3.SetFormationOffset(rightWingOffset);

            formationKeeper1.SetFormationActive(enable);
            formationKeeper2.SetFormationActive(enable);
            formationKeeper3.SetFormationActive(enable);
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

        yield return StabilizeSwarm(nextCheckpoint);
    }

    private IEnumerator StabilizeSwarm(Transform nextCheckpoint)
    {
        if (nextCheckpoint != null)
        {
            Vector3 directionToNext = (nextCheckpoint.position - GetAverageSwarmPosition()).normalized;
            Vector3 flatDirection = new Vector3(directionToNext.x, 0f, directionToNext.z).normalized;

            if (usePacketCommunication)
            {
                string directionData = $"{flatDirection.x},{flatDirection.y},{flatDirection.z}";
                
                float elapsedTime = 0f;
                while (elapsedTime < stabilizationTime)
                {
                    SendPacketToDrone(packetReceiver1, "rotate_to_direction", directionData);
                    SendPacketToDrone(packetReceiver2, "rotate_to_direction", directionData);
                    SendPacketToDrone(packetReceiver3, "rotate_to_direction", directionData);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
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
        if (usePacketCommunication)
        {
            SendPacketToDrone(packetReceiver1, "power_off", "");
            SendPacketToDrone(packetReceiver2, "power_off", "");
            SendPacketToDrone(packetReceiver3, "power_off", "");
        }
        else
        {
            foreach (DroneComputer drone in allDrones)
            {
                if (drone != null)
                {
                    drone.PowerOffEngine();
                }
            }
        }
    }

    private void PowerOnAllDrones()
    {
        if (usePacketCommunication)
        {
            SendPacketToDrone(packetReceiver1, "power_on", "");
            SendPacketToDrone(packetReceiver2, "power_on", "");
            SendPacketToDrone(packetReceiver3, "power_on", "");
        }
        else
        {
            foreach (DroneComputer drone in allDrones)
            {
                if (drone != null)
                {
                    drone.PowerOnEngine();
                }
            }
        }
    }

    private void SetAutoPilot(bool status)
    {
        if (usePacketCommunication)
        {
            string data = status ? "true" : "false";
            SendPacketToDrone(packetReceiver1, "set_autopilot", data);
            SendPacketToDrone(packetReceiver2, "set_autopilot", data);
            SendPacketToDrone(packetReceiver3, "set_autopilot", data);
        }
        else
        {
            droneComputer1.autoPilot = status;
            droneComputer2.autoPilot = status;
            droneComputer3.autoPilot = status;
        }
    }

    private void SetNewSwarmTarget(Transform currentCheckpoint, Transform nextCheckpoint)
    {
        Transform[] formationTargets = formationGenerator.GenerateFormationTransforms(currentCheckpoint, nextCheckpoint);
        
        if (usePacketCommunication)
        {
            droneComputer1.ClearTarget();
            droneComputer2.ClearTarget();
            droneComputer3.ClearTarget();
            
            SendTargetPacket(packetReceiver1, formationTargets[0]);
            SendTargetPacket(packetReceiver2, formationTargets[1]);
            SendTargetPacket(packetReceiver3, formationTargets[2]);
        }
        else
        {
            droneComputer1.SetTarget(formationTargets[0]);
            droneComputer2.SetTarget(formationTargets[1]);
            droneComputer3.SetTarget(formationTargets[2]);
        }
        
        if (pathPredictor != null) {
            pathPredictor.UpdatePrediction(currentCheckpoint);
        }
    }

    private void SetIndividualTargets(Transform target1, Transform target2, Transform target3)
    {
        if (usePacketCommunication)
        {
            droneComputer1.reachedTarget = false;
            droneComputer2.reachedTarget = false;
            droneComputer3.reachedTarget = false;
            
            SendTargetPacket(packetReceiver1, target1);
            SendTargetPacket(packetReceiver2, target2);
            SendTargetPacket(packetReceiver3, target3);
        }
        else
        {
            droneComputer1.SetTarget(target1);
            droneComputer2.SetTarget(target2);
            droneComputer3.SetTarget(target3);
        }
    }

    private bool IsCheckpointAlreadyVisited(Vector3 checkpointPosition)
    {
        foreach (Vector3 visitedPos in visitedCheckpointPositions)
        {
            if (Vector3.Distance(visitedPos, checkpointPosition) < duplicateCheckpointThreshold)
            {
                return true;
            }
        }
        return false;
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
        
        if (usePacketCommunication)
        {
            string data = speedMultiplier.ToString();
            SendPacketToDrone(packetReceiver1, "set_speed_multiplier", data);
            SendPacketToDrone(packetReceiver2, "set_speed_multiplier", data);
            SendPacketToDrone(packetReceiver3, "set_speed_multiplier", data);
        }
        else
        {
            foreach (DroneComputer drone in allDrones)
            {
                if (drone != null)
                {
                    drone.SetSpeedMultiplier(speedMultiplier);
                }
            }
        }
    }

    private void SendPacketToDrone(PacketReceiver receiver, string messageType, string data)
    {
        if (receiver == null)
        {
            Debug.LogWarning($"[AutoSwarm] Cannot send packet - receiver is null");
            return;
        }

        if (autoSwarmReceiver != null)
        {
            autoSwarmReceiver.SendPacket(receiver.receiverId, messageType, data);
        }
        else
        {
            Debug.LogError("[AutoSwarm] AutoSwarm PacketReceiver is null! Cannot send packet.");
        }
    }

    private void SendTargetPacket(PacketReceiver receiver, Transform target)
    {
        if (receiver == null || target == null) return;

        string data = $"{target.position.x},{target.position.y},{target.position.z}";
        Debug.Log($"[AutoSwarm] Sending set_target packet to {receiver.receiverId} → position {target.position}");
        SendPacketToDrone(receiver, "set_target", data);
    }

}
