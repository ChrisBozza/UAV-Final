using System.Collections;
using UnityEngine;

public class AutoSwarm: MonoBehaviour
{
    [SerializeField] GameObject drone1;
    DroneComputer droneComputer1;
    [SerializeField] GameObject checkpointParent;
    private GameObject[] checkpoints;

    public bool swarmActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        checkpoints = new GameObject[checkpointParent.transform.childCount];
        for (int i = 0; i < checkpointParent.transform.childCount; i++) {
            checkpoints[i] = checkpointParent.transform.GetChild(i).gameObject;
        }

        droneComputer1 = drone1.GetComponent<DroneComputer>();
        StartCoroutine(MissionHandler());

    }

    private IEnumerator MissionHandler() {

        droneComputer1.autoPilot = true;

        for (int i = 0; i < checkpoints.Length; i++) {
            SetNewSwarmTarget(checkpoints[i].transform);

            while (SwarmReachedTarget()) {
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(1f);
        }



        yield return null;
    }

    private void SetNewSwarmTarget(Transform target) {
        droneComputer1.SetTarget(target);
        return;
    }

    private bool SwarmReachedTarget() {
        return droneComputer1.reachedTarget;
    }

    private IEnumerator Step() {
        while (swarmActive) {
            yield return new WaitForSeconds(0.1f);
        }
    }

    void FlyToPoint() {
        return;
    }

    
}
