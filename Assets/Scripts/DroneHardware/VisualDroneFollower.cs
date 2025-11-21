using UnityEngine;

public class VisualDroneFollower : MonoBehaviour {
    [Header("Follow Settings")]
    [SerializeField] Transform invisDrone;
    [SerializeField] float positionSmoothSpeed = 10f;

    void LateUpdate() {
        if (invisDrone == null) return;

        FollowInvisDrone();
    }

    void FollowInvisDrone() {
        Vector3 targetPosition = invisDrone.position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);
    }

    public void SetInvisDrone(Transform target) {
        invisDrone = target;
    }
}
