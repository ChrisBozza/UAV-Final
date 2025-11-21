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

    private DroneComputer myComputer;
    private DroneComputer[] wingmen;
    private float lastCheckpointTime;
    private bool isInSlowdownImmunity;

    void Awake()
    {
        myComputer = GetComponent<DroneComputer>();
    }

    void FixedUpdate()
    {
        if (!formationActive) return;

        UpdateSlowdownImmunity();

        switch (role)
        {
            case FormationRole.Leader:
                ApplyLeaderBehavior();
                break;
            case FormationRole.LeftWing:
            case FormationRole.RightWing:
                ApplyWingmanBehavior();
                break;
        }
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
    }

    public void SetWingmenReferences(DroneComputer[] wingmenDrones)
    {
        wingmen = wingmenDrones;
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
            float distance = Vector3.Distance(myPosition, wingman.GetPosition());
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

        Vector3 leaderPosition = leaderDrone.GetPosition();
        Vector3 leaderVelocity = leaderDrone.GetVelocity();
        
        Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
        
        Vector3 forwardDirection = horizontalVelocity.magnitude > 0.5f 
            ? horizontalVelocity.normalized 
            : leaderDrone.GetTargetDirection();
        
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
            Vector3 leaderPos = leaderDrone.GetPosition();
            Vector3 leaderVelocity = leaderDrone.GetVelocity();
            
            Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);
            
            Vector3 forwardDirection = horizontalVelocity.magnitude > 0.5f 
                ? horizontalVelocity.normalized 
                : leaderDrone.GetTargetDirection();
            
            Vector3 right = Vector3.Cross(Vector3.up, forwardDirection).normalized;
            Vector3 back = -forwardDirection;
            
            Vector3 worldOffset = (right * localFormationOffset.x) + (back * localFormationOffset.z);
            Vector3 idealPosition = leaderPos + worldOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(idealPosition, 0.3f);
            Gizmos.DrawLine(transform.position, idealPosition);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, leaderPos);
        }
    }
}
