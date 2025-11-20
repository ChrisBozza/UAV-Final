using UnityEngine;

public class TriangleFormationGenerator : MonoBehaviour
{
    [Header("Formation Settings")]
    public float formationWidth = 5f;
    public float formationDepth = 3f;

    private GameObject leftMarker;
    private GameObject rightMarker;

    void Awake()
    {
        leftMarker = new GameObject("Formation_Left");
        leftMarker.transform.SetParent(transform);
        
        rightMarker = new GameObject("Formation_Right");
        rightMarker.transform.SetParent(transform);
    }

    public Vector3[] GenerateFormationPositions(Vector3 currentCheckpoint, Vector3 nextCheckpoint)
    {
        Vector3[] positions = new Vector3[3];
        
        positions[0] = currentCheckpoint;
        
        Vector3 directionToNext = (nextCheckpoint - currentCheckpoint).normalized;
        
        Vector3 right = Vector3.Cross(Vector3.up, directionToNext).normalized;
        
        Vector3 backwardOffset = -directionToNext * formationDepth;
        
        positions[1] = currentCheckpoint + backwardOffset + (right * formationWidth * 0.5f);
        positions[2] = currentCheckpoint + backwardOffset + (-right * formationWidth * 0.5f);
        
        leftMarker.transform.position = positions[1];
        rightMarker.transform.position = positions[2];
        
        return positions;
    }

    public Transform[] GenerateFormationTransforms(Transform currentCheckpoint, Transform nextCheckpoint)
    {
        Vector3 currentPos = currentCheckpoint.position;
        Vector3 nextPos = nextCheckpoint != null ? nextCheckpoint.position : currentPos + currentCheckpoint.forward * 10f;
        
        Vector3[] positions = GenerateFormationPositions(currentPos, nextPos);
        
        Transform[] transforms = new Transform[3];
        transforms[0] = currentCheckpoint;
        
        leftMarker.transform.position = positions[1];
        transforms[1] = leftMarker.transform;
        
        rightMarker.transform.position = positions[2];
        transforms[2] = rightMarker.transform;
        
        return transforms;
    }

    void OnDrawGizmos()
    {
        if (leftMarker != null && rightMarker != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftMarker.transform.position, 0.5f);
            Gizmos.DrawWireSphere(rightMarker.transform.position, 0.5f);
        }
    }
}
