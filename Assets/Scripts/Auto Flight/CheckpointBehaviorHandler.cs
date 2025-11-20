using UnityEngine;

public class CheckpointBehaviorHandler : MonoBehaviour
{
    public enum CheckpointType
    {
        Normal,
        Takeoff,
        Landing
    }

    public class CheckpointInfo
    {
        public CheckpointType type;
        public Transform takeoffCheckpoint;
        public Transform[] landingPositions;
        public Transform originalCheckpoint;
    }

    private GameObject takeoffMarker;
    private GameObject[] individualTakeoffMarkers;

    void Awake()
    {
        takeoffMarker = new GameObject("Takeoff_Marker");
        takeoffMarker.transform.SetParent(transform);

        individualTakeoffMarkers = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            individualTakeoffMarkers[i] = new GameObject($"Takeoff_Marker_{i}");
            individualTakeoffMarkers[i].transform.SetParent(transform);
        }
    }

    public CheckpointInfo AnalyzeCheckpoint(Transform checkpoint)
    {
        CheckpointInfo info = new CheckpointInfo
        {
            originalCheckpoint = checkpoint
        };

        string checkpointName = checkpoint.name;

        if (checkpointName.StartsWith("Takeoff"))
        {
            info.type = CheckpointType.Takeoff;
            info.takeoffCheckpoint = checkpoint;
        }
        else if (checkpointName.StartsWith("Landing"))
        {
            info.type = CheckpointType.Landing;
            info.landingPositions = GetLandingPositions(checkpoint);
        }
        else
        {
            info.type = CheckpointType.Normal;
        }

        return info;
    }

    private Transform[] GetLandingPositions(Transform checkpoint)
    {
        if (checkpoint.childCount < 3)
        {
            Debug.LogWarning($"Landing checkpoint {checkpoint.name} should have 3 children but has {checkpoint.childCount}");
            return new Transform[0];
        }

        Transform[] positions = new Transform[3];
        for (int i = 0; i < 3 && i < checkpoint.childCount; i++)
        {
            positions[i] = checkpoint.GetChild(i);
        }

        return positions;
    }

    public Transform CreateTakeoffPosition(Vector3 currentPosition, Transform takeoffCheckpoint)
    {
        float targetHeight = takeoffCheckpoint.position.y;
        takeoffMarker.transform.position = new Vector3(currentPosition.x, targetHeight, currentPosition.z);
        return takeoffMarker.transform;
    }

    public Transform CreateIndividualTakeoffPosition(Vector3 dronePosition, float targetHeight, int droneIndex)
    {
        individualTakeoffMarkers[droneIndex].transform.position = new Vector3(dronePosition.x, targetHeight, dronePosition.z);
        return individualTakeoffMarkers[droneIndex].transform;
    }

    public Transform[] CreateHoverPositions(Transform[] landingPositions, float hoverHeight)
    {
        Transform[] hoverTransforms = new Transform[3];
        
        for (int i = 0; i < landingPositions.Length; i++)
        {
            GameObject hoverMarker = new GameObject($"Hover_Marker_{i}");
            hoverMarker.transform.SetParent(transform);
            
            Vector3 landingPos = landingPositions[i].position;
            hoverMarker.transform.position = new Vector3(landingPos.x, landingPos.y + hoverHeight, landingPos.z);
            
            hoverTransforms[i] = hoverMarker.transform;
        }
        
        return hoverTransforms;
    }
}
