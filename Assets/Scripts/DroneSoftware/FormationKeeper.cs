using UnityEngine;

public class FormationKeeper : MonoBehaviour
{
    public enum FormationRole
    {
        None,
        Leader,
        LeftWing,
        RightWing
    }

    [Header("Formation Role")]
    public FormationRole role = FormationRole.None;

    [Header("Formation Parameters")]
    public DroneComputer leaderDrone;
    public Vector3 localFormationOffset = Vector3.zero;
    public bool formationActive = false;

    [Header("Formation Settings")]
    [SerializeField] float correctionStrength = 2f;
    [SerializeField] float maxCorrectionForce = 1f;
    [SerializeField] float formationThreshold = 0.5f;

    [Header("Leader Behavior")]
    [SerializeField] bool enableLeaderSlowdown = true;
    [SerializeField] float slowdownThreshold = 4f;
    [SerializeField] float slowdownStrength = 3f;
    [SerializeField] float formationImmunityDuration = 2f;

    [Header("Packet Communication")]
    [SerializeField] float leaderBroadcastRate = 0.05f;
    [SerializeField] float leaderDataTimeout = 0.5f;
    public bool usePacketCommunication = true;

    private DroneComputer myComputer;
    private PacketReceiver packetReceiver;
    private DroneComputer[] wingmen;
    private float lastCheckpointTime;
    private bool isInSlowdownImmunity;

    private Vector3 cachedLeaderPosition;
    private Vector3 cachedLeaderVelocity;
    private Vector3 cachedLeaderTargetDirection;
    private float lastLeaderDataTime;
    private float nextLeaderBroadcastTime;

    private string leaderDroneId;
    private string[] wingmenIds;

    void Awake()
    {
        myComputer = GetComponent<DroneComputer>();
        packetReceiver = GetComponent<PacketReceiver>();

        if (packetReceiver == null)
        {
            Debug.LogWarning($"[FormationKeeper] No PacketReceiver found on {gameObject.name}. Packet communication disabled.");
            usePacketCommunication = false;
        }
    }

    void FixedUpdate()
    {
        if (!formationActive) return;

        UpdateSlowdownImmunity();

        switch (role)
        {
            case FormationRole.Leader:
                BroadcastLeaderState();
                ApplyLeaderBehavior();
                break;
            case FormationRole.LeftWing:
            case FormationRole.RightWing:
                ApplyWingmanBehavior();
                break;
        }
    }

    private void BroadcastLeaderState()
    {
        if (!usePacketCommunication || packetReceiver == null) return;
        if (Time.time < nextLeaderBroadcastTime) return;

        Vector3 position = myComputer.GetPosition();
        Vector3 velocity = myComputer.GetVelocity();
        Vector3 targetDir = myComputer.GetTargetDirection();

        string data = $"{position.x},{position.y},{position.z}|{velocity.x},{velocity.y},{velocity.z}|{targetDir.x},{targetDir.y},{targetDir.z}";

        packetReceiver.SendPacket("broadcast", "leader_state", data);

        nextLeaderBroadcastTime = Time.time + leaderBroadcastRate;
    }

    public void OnLeaderStateReceived(string senderId, string data)
    {
        if (role == FormationRole.Leader) return;
        if (leaderDrone == null || senderId != leaderDroneId) return;

        string[] parts = data.Split('|');
        if (parts.Length >= 3)
        {
            cachedLeaderPosition = ParseVector3(parts[0]);
            cachedLeaderVelocity = ParseVector3(parts[1]);
            cachedLeaderTargetDirection = ParseVector3(parts[2]);
            lastLeaderDataTime = Time.time;
        }
    }

    private Vector3 ParseVector3(string vectorString)
    {
        string[] components = vectorString.Split(',');
        if (components.Length >= 3)
        {
            float x = float.Parse(components[0]);
            float y = float.Parse(components[1]);
            float z = float.Parse(components[2]);
            return new Vector3(x, y, z);
        }
        return Vector3.zero;
    }

    private bool HasValidLeaderData()
    {
        return Time.time - lastLeaderDataTime < leaderDataTimeout;
    }

    private void UpdateSlowdownImmunity()
    {
        isInSlowdownImmunity = Time.time - lastCheckpointTime < formationImmunityDuration;
    }

    public void OnCheckpointReached()
    {
        lastCheckpointTime = Time.time;
        isInSlowdownImmunity = true;
    }

    public void SetFormationRole(FormationRole newRole)
    {
        role = newRole;
    }

    public void SetLeaderReference(DroneComputer leader)
    {
        leaderDrone = leader;
        if (leader != null)
        {
            PacketReceiver leaderReceiver = leader.GetComponent<PacketReceiver>();
            if (leaderReceiver != null)
            {
                leaderDroneId = leaderReceiver.receiverId;
            }
        }
    }

    public void SetWingmenReferences(DroneComputer[] wingmenDrones)
    {
        wingmen = wingmenDrones;
        if (wingmenDrones != null && wingmenDrones.Length > 0)
        {
            wingmenIds = new string[wingmenDrones.Length];
            for (int i = 0; i < wingmenDrones.Length; i++)
            {
                if (wingmenDrones[i] != null)
                {
                    PacketReceiver receiver = wingmenDrones[i].GetComponent<PacketReceiver>();
                    if (receiver != null)
                    {
                        wingmenIds[i] = receiver.receiverId;
                    }
                }
            }
        }
    }

    public void SetFormationOffset(Vector3 offset)
    {
        localFormationOffset = offset;
    }

    public void SetFormationActive(bool active)
    {
        formationActive = active;
    }

    private void ApplyLeaderBehavior()
    {
        if (!enableLeaderSlowdown || isInSlowdownImmunity) return;
        if (wingmen == null || wingmen.Length == 0) return;

        Vector3 myPosition = myComputer.GetPosition();
        float maxLagDistance = 0f;

        foreach (DroneComputer wingman in wingmen)
        {
            if (wingman == null) continue;
            
            Vector3 wingmanPosition = wingman.GetPosition();
            float distance = Vector3.Distance(myPosition, wingmanPosition);
            
            if (distance > maxLagDistance)
                maxLagDistance = distance;
        }

        if (maxLagDistance > slowdownThreshold)
        {
            float excessDistance = maxLagDistance - slowdownThreshold;
            float dragMultiplier = Mathf.Clamp01(excessDistance / slowdownThreshold);

            Vector3 myVelocity = myComputer.GetVelocity();
            Vector3 dragForce = -myVelocity.normalized * dragMultiplier * slowdownStrength * Time.fixedDeltaTime;

            myComputer.AddMomentum(dragForce);
        }
    }

    private void ApplyWingmanBehavior()
    {
        if (isInSlowdownImmunity) return;
        if (leaderDrone == null) return;

        Vector3 leaderPosition;
        Vector3 leaderVelocity;
        Vector3 forwardDirection;

        if (usePacketCommunication && HasValidLeaderData())
        {
            leaderPosition = cachedLeaderPosition;
            leaderVelocity = cachedLeaderVelocity;
            
            Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
            
            forwardDirection = horizontalVelocity.magnitude > 0.5f 
                ? horizontalVelocity.normalized 
                : cachedLeaderTargetDirection;
        }
        else
        {
            leaderPosition = leaderDrone.GetPosition();
            leaderVelocity = leaderDrone.GetVelocity();
            
            Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
            
            forwardDirection = horizontalVelocity.magnitude > 0.5f 
                ? horizontalVelocity.normalized 
                : leaderDrone.GetTargetDirection();
        }
        
        Vector3 right = Vector3.Cross(Vector3.up, forwardDirection).normalized;
        Vector3 back = -forwardDirection;
        
        Vector3 worldOffset = (right * localFormationOffset.x) + (back * localFormationOffset.z);
        
        Vector3 idealPosition = leaderPosition + worldOffset;
        Vector3 myPosition = myComputer.GetPosition();
        Vector3 offset = idealPosition - myPosition;

        float horizontalOffsetMagnitude = new Vector3(offset.x, 0f, offset.z).magnitude;

        if (horizontalOffsetMagnitude > formationThreshold)
        {
            Vector3 correctionForce = offset * correctionStrength * Time.fixedDeltaTime;
            correctionForce = Vector3.ClampMagnitude(correctionForce, maxCorrectionForce * Time.fixedDeltaTime);

            myComputer.AddMomentum(correctionForce);
        }
    }

    void OnDrawGizmos()
    {
        if (!formationActive) return;

        if (role == FormationRole.Leader && wingmen != null)
        {
            Gizmos.color = Color.green;
            Vector3 myPos = transform.position;
            Gizmos.DrawWireSphere(myPos, 0.4f);

            foreach (DroneComputer wingman in wingmen)
            {
                if (wingman != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(myPos, wingman.GetPosition());
                }
            }
        }
        else if ((role == FormationRole.LeftWing || role == FormationRole.RightWing) && leaderDrone != null)
        {
            Vector3 leaderPos;
            Vector3 leaderVelocity;
            Vector3 forwardDirection;

            if (usePacketCommunication && HasValidLeaderData())
            {
                leaderPos = cachedLeaderPosition;
                leaderVelocity = cachedLeaderVelocity;
                
                Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
                
                forwardDirection = horizontalVelocity.magnitude > 0.5f 
                    ? horizontalVelocity.normalized 
                    : cachedLeaderTargetDirection;
            }
            else
            {
                leaderPos = leaderDrone.GetPosition();
                leaderVelocity = leaderDrone.GetVelocity();
                
                Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
                
                forwardDirection = horizontalVelocity.magnitude > 0.5f 
                    ? horizontalVelocity.normalized 
                    : leaderDrone.GetTargetDirection();
            }
            
            Vector3 right = Vector3.Cross(Vector3.up, forwardDirection).normalized;
            Vector3 back = -forwardDirection;
            
            Vector3 worldOffset = (right * localFormationOffset.x) + (back * localFormationOffset.z);
            Vector3 idealPosition = leaderPos + worldOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(idealPosition, 0.3f);
            Gizmos.DrawLine(transform.position, idealPosition);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, leaderPos);
            
            if (usePacketCommunication && !HasValidLeaderData())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
    }
}
