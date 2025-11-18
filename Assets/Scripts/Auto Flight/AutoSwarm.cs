using System.Collections;
using UnityEngine;

public class AutoSwarm: MonoBehaviour
{
    [SerializeField] GameObject drone1;
    DroneComputer droneComputer1;
    [SerializeField] GameObject[] checkpoints;

    public bool swarmActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneComputer1 = drone1.GetComponent<DroneComputer>();
        StartCoroutine(MissionHandler());

    }

    private IEnumerator MissionHandler() {

        droneComputer1.autoPilot = true;
        droneComputer1.SetTarget(checkpoints[0].transform);


        yield return null;
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
