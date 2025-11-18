using System.Collections;
using UnityEngine;

public class AutoSwarm: MonoBehaviour
{
    [SerializeField] GameObject drone1;
    [SerializeField] GameObject[] checkpoints;

    DroneController droneController;

    public bool swarmActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        droneController = drone1.GetComponent<DroneController>();
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
