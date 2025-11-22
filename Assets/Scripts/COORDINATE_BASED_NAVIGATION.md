# Coordinate-Based Navigation System

## Overview

Drones now support **direct coordinate-based navigation** via packets! Instead of creating temporary Transform objects, drones can fly directly towards Vector3 positions received in packets.

---

## How It Works

### Old Approach (Transform-Based)
```
Packet with position → Create GameObject → Set as target → Fly to Transform
                              ↓
                       (cleanup later)
```
❌ Creates unnecessary GameObjects  
❌ Memory overhead  
❌ Scene clutter  

### New Approach (Coordinate-Based)
```
Packet with position → Parse Vector3 → Fly directly to coordinates
```
✅ No GameObject creation  
✅ Clean and efficient  
✅ Direct navigation  

---

## DroneComputer API

### Set Target by Coordinates

```csharp
public void SetTargetPosition(Vector3 position)
```

**Usage:**
```csharp
droneComputer.SetTargetPosition(new Vector3(10f, 5f, 20f));
```

The drone will fly towards this position using the same autopilot logic as Transform-based targets.

### Set Target by Transform (Still Supported)

```csharp
public void SetTarget(Transform t)
```

**Usage:**
```csharp
droneComputer.SetTarget(checkpointTransform);
```

Both methods work seamlessly - the drone doesn't care which one you use!

---

## Implementation Details

### Internal State

DroneComputer now maintains two possible target types:

```csharp
Transform target;           // Transform-based target (legacy support)
Vector3? targetPosition;    // Coordinate-based target (new)
```

**Priority:**
1. If `target` is set (Transform), use `target.position`
2. If `targetPosition` has value, use `targetPosition.Value`
3. Otherwise, no target

### Helper Methods

**`HasTarget()`** - Check if any target is set
```csharp
private bool HasTarget()
{
    return target != null || targetPosition.HasValue;
}
```

**`GetTargetPosition()`** - Get current target position regardless of type
```csharp
private Vector3 GetTargetPosition()
{
    if (target != null)
        return target.position;
    if (targetPosition.HasValue)
        return targetPosition.Value;
    return droneController.GetPosition();
}
```

**`GetTargetDirection()`** - Get direction to target (public API)
```csharp
public Vector3 GetTargetDirection()
{
    if (!HasTarget()) return transform.forward;
    Vector3 toTarget = GetTargetPosition() - GetPosition();
    Vector3 horizontalDirection = new Vector3(toTarget.x, 0f, toTarget.z);
    return horizontalDirection.magnitude > 0.01f ? horizontalDirection.normalized : transform.forward;
}
```

---

## Packet Communication

### Sending Target Coordinates

**From AutoSwarm:**
```csharp
Vector3 targetPos = new Vector3(10f, 5f, 20f);
string data = $"{targetPos.x},{targetPos.y},{targetPos.z}";
SendPacketToDrone(packetReceiver1, "set_target", data);
```

**From Any External System:**
```csharp
Vector3 destination = new Vector3(15f, 10f, 8f);
string positionData = $"{destination.x},{destination.y},{destination.z}";
Packet packet = new Packet("MyController", "drone1", "set_target", positionData);
PacketHandler.Instance?.BroadcastPacket(packet);
```

### Receiving Target Coordinates

**PacketReceiver Handler:**
```csharp
private void HandleSetTarget(Packet packet)
{
    if (droneComputer != null)
    {
        Vector3 targetPosition = ParseVector3(packet.data);
        droneComputer.SetTargetPosition(targetPosition);
    }
}
```

**ParseVector3 Helper:**
```csharp
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
```

---

## Autopilot Behavior

The autopilot system is **target-type agnostic**:

```csharp
IEnumerator AutoFly() {
    while(!droneReady) yield return new WaitForEndOfFrame();
    
    while (true) {
        yield return new WaitForSeconds(0.1f);
        if (autoPilot && HasTarget()) {
            MoveTowardsPoint();  // Works for both Transform and Vector3
        }
    }
}

private void MoveTowardsPoint() {
    Vector3 toTarget = GetTargetPosition() - droneController.GetPosition();
    // ... same flight logic for both target types
}
```

**The drone doesn't know (or care) if it's flying to a Transform or coordinates!**

---

## Target Reached Detection

```csharp
IEnumerator AutoCheck() {
    while(!droneReady) yield return new WaitForEndOfFrame();

    while(true) {
        yield return null;
        
        if (HasTarget())
        {
            Vector3 toTarget = GetTargetPosition() - droneController.GetPosition();
            float dist = toTarget.magnitude;

            if (dist < 2f) {
                reachedTarget = true;
            }
        }
    }
}
```

Works identically for both target types.

---

## Usage Examples

### Example 1: Command Drone to Coordinates

```csharp
public class DroneCommander : MonoBehaviour
{
    public DroneComputer drone;
    
    void CommandDroneToPosition()
    {
        Vector3 destination = new Vector3(50f, 15f, 30f);
        drone.SetTargetPosition(destination);
        drone.autoPilot = true;
    }
}
```

### Example 2: Send Waypoint via Packet

```csharp
public class WaypointController : MonoBehaviour
{
    void SendWaypoint(string droneId, Vector3 waypoint)
    {
        string data = $"{waypoint.x},{waypoint.y},{waypoint.z}";
        Packet packet = new Packet("WaypointController", droneId, "set_target", data);
        PacketHandler.Instance?.BroadcastPacket(packet);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Vector3 randomWaypoint = new Vector3(
                Random.Range(-50f, 50f),
                Random.Range(5f, 20f),
                Random.Range(-50f, 50f)
            );
            SendWaypoint("drone1", randomWaypoint);
        }
    }
}
```

### Example 3: Mix Transform and Coordinate Targets

```csharp
public class MissionController : MonoBehaviour
{
    public DroneComputer drone;
    public Transform checkpoint1;
    
    IEnumerator RunMission()
    {
        // Use Transform target
        drone.SetTarget(checkpoint1);
        drone.autoPilot = true;
        yield return new WaitUntil(() => drone.reachedTarget);
        
        // Use coordinate target
        Vector3 customPosition = new Vector3(25f, 10f, 15f);
        drone.SetTargetPosition(customPosition);
        yield return new WaitUntil(() => drone.reachedTarget);
        
        // Back to Transform
        drone.SetTarget(checkpoint1);
        yield return new WaitUntil(() => drone.reachedTarget);
    }
}
```

---

## Benefits

### Performance
✅ **No GameObject allocation** - Eliminates `new GameObject()` calls  
✅ **No Transform lookups** - Direct Vector3 math  
✅ **No scene clutter** - No temporary objects  
✅ **Faster** - Less overhead per target assignment  

### Code Quality
✅ **Simpler** - No GameObject lifecycle management  
✅ **Cleaner** - Direct coordinate handling  
✅ **Safer** - No dangling references  

### Flexibility
✅ **Procedural waypoints** - Generate coordinates on the fly  
✅ **Calculated positions** - Math-based destinations  
✅ **Dynamic targets** - Update coordinates continuously  

---

## Backward Compatibility

**Transform-based targets still work perfectly!**

```csharp
// Old code - still works
droneComputer.SetTarget(transformTarget);

// New code - also works
droneComputer.SetTargetPosition(vectorTarget);
```

AutoSwarm, FormationGenerator, and all existing code continue to work without changes.

---

## When to Use Each

### Use `SetTargetPosition(Vector3)` when:
- ✅ Receiving coordinates via packets
- ✅ Calculating procedural destinations
- ✅ Working with pure math/algorithms
- ✅ Temporary waypoints
- ✅ Grid-based or geometric patterns

### Use `SetTarget(Transform)` when:
- ✅ Following moving objects
- ✅ Scene-based checkpoints
- ✅ Tracking dynamic entities
- ✅ Formation following (leader transform)
- ✅ GameObject-based level design

---

## Packet Data Format

**Message Type:** `set_target`

**Data Format:** `x,y,z`

**Example:** `"25.5,10.0,42.3"`

**Precision:** Standard float precision (adequate for navigation)

---

## Integration with AutoSwarm

AutoSwarm automatically uses coordinate-based targets when packet communication is enabled:

```csharp
private void SendTargetPacket(PacketReceiver receiver, Transform target)
{
    if (receiver == null || target == null) return;

    // Extract position from Transform
    string data = $"{target.position.x},{target.position.y},{target.position.z}";
    SendPacketToDrone(receiver, "set_target", data);
}
```

**Flow:**
1. AutoSwarm has Transform reference (checkpoint)
2. Extracts position as Vector3
3. Encodes as string
4. Sends via packet
5. Drone receives and uses coordinate-based navigation

**Result:** Realistic communication - only position data transmitted, not object references!

---

## Debugging

### Check Current Target

```csharp
void OnDrawGizmos()
{
    if (droneComputer.HasTarget())
    {
        Vector3 targetPos = droneComputer.GetTargetPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 2f);
        Gizmos.DrawLine(droneComputer.GetPosition(), targetPos);
    }
}
```

### Log Target Changes

```csharp
public void SetTargetPosition(Vector3 position)
{
    reachedTarget = false;
    target = null;
    targetPosition = position;
    Debug.Log($"[{gameObject.name}] New coordinate target: {position}");
}
```

---

## Summary

Drones now support **two target modes**:

| Mode | Method | Use Case |
|------|--------|----------|
| **Transform** | `SetTarget(Transform)` | Scene objects, moving targets |
| **Coordinate** | `SetTargetPosition(Vector3)` | Packet commands, procedural |

**Both modes:**
- ✅ Use the same autopilot logic
- ✅ Have the same reach detection
- ✅ Support all speed control features
- ✅ Work with formation keeping

**Coordinate mode advantages:**
- ✅ No GameObject overhead
- ✅ Perfect for packet communication
- ✅ Clean and efficient

**The drone navigation system is now packet-first by design!** 📡🚁
