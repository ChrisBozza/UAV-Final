using UnityEngine;
using System.Collections.Generic;
public class MovementTracker : MonoBehaviour
{

    Dictionary<string, bool> isMoving = new Dictionary<string, bool>();
    List<string> keys;

    public MovementDecay movementDecay;
    public KeyboardInput keyboardInput;
    public VisualTilt visualTilt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        isMoving["forward"] = false;
        isMoving["backward"] = false;
        isMoving["left"] = false;
        isMoving["right"] = false;
        isMoving["up"] = false;
        isMoving["down"] = false;

        isMoving["yawLeft"] = false;
        isMoving["yawRight"] = false;
        keys = new List<string>(isMoving.Keys);

        keyboardInput = GetComponent<KeyboardInput>();
        movementDecay = GetComponent<MovementDecay>();
        visualTilt = GetComponent<VisualTilt>();

        keyboardInput.SetMovementTracker(this);
        movementDecay.SetMovementTracker(this);
        visualTilt.SetMovementTracker(this);
    }

    public void SetMovementState(string direction, bool moving) {
        isMoving[direction] = moving;
    }

    public void ClearMovementState() {
        foreach (string key in keys) {
            isMoving[key] = false;
        }
    }

    public bool IsMoving(string direction) {
        return isMoving[direction];
    }
}
