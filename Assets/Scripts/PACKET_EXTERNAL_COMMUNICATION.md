# External-to-Drone Packet Communication

## Overview

AutoSwarm and other external control systems now use **packet-based communication** for all runtime drone commands. This simulates a realistic command-and-control system where the ground station communicates with drones via radio.

## Communication Flow

### Before (Direct Access)
```
AutoSwarm → droneComputer1.PowerOnEngine()
AutoSwarm → droneComputer2.SetTarget(target)
AutoSwarm → formationKeeper3.SetFormationActive(true)
```
❌ **Problem:** Unrealistic - direct method calls, no communication simulation

### After (Packet Communication)
```
AutoSwarm → SendPacket("drone1", "power_on", "")
                ↓
          PacketHandler
                ↓
            Drone 1
                ↓
         PacketReceiver
                ↓
      droneComputer.PowerOnEngine()
```
✅ **Benefit:** Realistic radio communication with delay, loss, and filtering

---

## AutoSwarm Packet Integration

### Packet Communication Toggle

AutoSwarm has a new inspector setting:

**`usePacketCommunication`** (default: true)
- **true:** All runtime commands sent via packets
- **false:** Falls back to direct method calls

### Commands Now Using Packets

| Operation | Packet Type | Data Format |
|-----------|-------------|-------------|
| Set target position | `set_target` | `x,y,z` |
| Enable/disable autopilot | `set_autopilot` | `true` or `false` |
| Power on engine | `power_on` | (empty) |
| Power off engine | `power_off` | (empty) |
| Set speed multiplier | `set_speed_multiplier` | `0.5` |
| Rotate to direction | `rotate_to_direction` | `x,y,z` |
| Formation offset | `formation_offset` | `x,y,z` |
| Formation active | `formation_active` | `true` or `false` |
| Checkpoint reached | `checkpoint_reached` | (empty) |

---

## Example Usage

### 1. Setting Drone Targets

**Old (Direct):**
```csharp
droneComputer1.SetTarget(target1);
droneComputer2.SetTarget(target2);
droneComputer3.SetTarget(target3);
```

**New (Packets):**
```csharp
SendTargetPacket(packetReceiver1, target1);
SendTargetPacket(packetReceiver2, target2);
SendTargetPacket(packetReceiver3, target3);

// Helper method:
void SendTargetPacket(PacketReceiver receiver, Transform target)
{
    string data = $"{target.position.x},{target.position.y},{target.position.z}";
    SendPacketToDrone(receiver, "set_target", data);
}
```

**What happens:**
1. AutoSwarm creates packet with target position encoded as string
2. PacketHandler broadcasts to all drones
3. Target drone's PacketReceiver receives packet
4. Parses position and calls `droneComputer.SetTargetPosition(targetPosition)`
5. Drone flies directly towards those coordinates

### 2. Powering Drones On/Off

**Old (Direct):**
```csharp
droneComputer1.PowerOnEngine();
droneComputer2.PowerOnEngine();
droneComputer3.PowerOnEngine();
```

**New (Packets):**
```csharp
SendPacketToDrone(packetReceiver1, "power_on", "");
SendPacketToDrone(packetReceiver2, "power_on", "");
SendPacketToDrone(packetReceiver3, "power_on", "");
```

**What happens:**
1. AutoSwarm broadcasts `power_on` packet
2. Each drone receives packet
3. Calls `droneComputer.PowerOnEngine()`

### 3. Formation Configuration

**Old (Direct):**
```csharp
formationKeeper2.SetFormationOffset(leftWingOffset);
formationKeeper3.SetFormationOffset(rightWingOffset);
formationKeeper1.SetFormationActive(true);
```

**New (Packets):**
```csharp
string offsetData2 = $"{leftWingOffset.x},{leftWingOffset.y},{leftWingOffset.z}";
string offsetData3 = $"{rightWingOffset.x},{rightWingOffset.y},{rightWingOffset.z}";

SendPacketToDrone(packetReceiver2, "formation_offset", offsetData2);
SendPacketToDrone(packetReceiver3, "formation_offset", offsetData3);
SendPacketToDrone(packetReceiver1, "formation_active", "true");
```

**What happens:**
1. AutoSwarm sends formation configuration packets
2. Wingmen receive offset packets
3. Leader receives activation packet
4. Each drone applies configuration locally

### 4. Stabilization (Rotate to Direction)

**Old (Direct):**
```csharp
foreach (DroneComputer drone in allDrones)
{
    drone.RotateTowardsDirection(flatDirection);
}
```

**New (Packets):**
```csharp
string directionData = $"{flatDirection.x},{flatDirection.y},{flatDirection.z}";
SendPacketToDrone(packetReceiver1, "rotate_to_direction", directionData);
SendPacketToDrone(packetReceiver2, "rotate_to_direction", directionData);
SendPacketToDrone(packetReceiver3, "rotate_to_direction", directionData);
```

**What happens:**
1. AutoSwarm sends rotation command
2. Each drone receives direction vector
3. Calls `droneComputer.RotateTowardsDirection()`

---

## Packet Handler Methods in AutoSwarm

### Core Send Method

```csharp
private void SendPacketToDrone(PacketReceiver receiver, string messageType, string data)
{
    if (receiver == null)
    {
        Debug.LogWarning($"[AutoSwarm] Cannot send packet - receiver is null");
        return;
    }

    Packet packet = new Packet("AutoSwarm", receiver.receiverId, messageType, data);
    PacketHandler.Instance?.BroadcastPacket(packet);
}
```

**Parameters:**
- `receiver` - The drone's PacketReceiver component
- `messageType` - Command type (e.g., "power_on")
- `data` - Command payload (e.g., "true" or "1.5,2.0,3.0")

**Sender ID:** Always `"AutoSwarm"` for external commands

### Target Helper Method

```csharp
private void SendTargetPacket(PacketReceiver receiver, Transform target)
{
    if (receiver == null || target == null) return;

    string data = $"{target.position.x},{target.position.y},{target.position.z}";
    SendPacketToDrone(receiver, "set_target", data);
}
```

Converts Transform to position string for packet transmission.

---

## Packet Receiver Handlers

### Formation Commands

**`formation_offset`** - Set wingman formation offset
```csharp
private void HandleFormationOffset(Packet packet)
{
    if (formationKeeper != null)
    {
        Vector3 offset = ParseVector3(packet.data);
        formationKeeper.SetFormationOffset(offset);
    }
}
```

**`formation_active`** - Enable/disable formation keeping
```csharp
private void HandleFormationActive(Packet packet)
{
    if (formationKeeper != null)
    {
        bool active = packet.data == "true";
        formationKeeper.SetFormationActive(active);
    }
}
```

### Navigation Commands

**`set_target`** - Navigate to position
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

**Note:** Drones now fly directly towards Vector3 coordinates - no temporary GameObjects needed!

**`rotate_to_direction`** - Rotate towards direction
```csharp
private void HandleRotateToDirection(Packet packet)
{
    if (droneComputer != null)
    {
        Vector3 direction = ParseVector3(packet.data);
        droneComputer.RotateTowardsDirection(direction);
    }
}
```

### Power Commands

**`power_on`** - Turn on engine
```csharp
private void HandlePowerOn(Packet packet)
{
    if (droneComputer != null)
    {
        droneComputer.PowerOnEngine();
    }
}
```

**`power_off`** - Turn off engine
```csharp
private void HandlePowerOff(Packet packet)
{
    if (droneComputer != null)
    {
        droneComputer.PowerOffEngine();
    }
}
```

### Autopilot Commands

**`set_autopilot`** - Enable/disable autopilot
```csharp
private void HandleSetAutopilot(Packet packet)
{
    if (droneComputer != null)
    {
        bool autopilot = packet.data == "true";
        droneComputer.autoPilot = autopilot;
    }
}
```

**`set_speed_multiplier`** - Adjust speed
```csharp
private void HandleSetSpeedMultiplier(Packet packet)
{
    if (droneComputer != null)
    {
        float multiplier = float.Parse(packet.data);
        droneComputer.SetSpeedMultiplier(multiplier);
    }
}
```

### Event Notifications

**`checkpoint_reached`** - Notify checkpoint arrival
```csharp
private void HandleCheckpointReached(Packet packet)
{
    if (formationKeeper != null)
    {
        formationKeeper.OnCheckpointReached();
    }
}
```

---

## Setup vs Runtime Communication

### Setup (DIRECT - Allowed)

During initialization, AutoSwarm still uses **direct references**:

```csharp
// GameObject and DroneComputer references (SETUP)
droneComputer1 = drone1.GetComponent<DroneComputer>();
packetReceiver1 = drone1.GetComponent<PacketReceiver>();

// Formation role assignment (SETUP)
formationKeeper1.SetFormationRole(FormationKeeper.FormationRole.Leader);
formationKeeper2.SetLeaderReference(droneComputer1);
formationKeeper1.SetWingmenReferences(new DroneComputer[] { droneComputer2, droneComputer3 });
```

**Why?** These are configuration, not runtime communication.

### Runtime (PACKETS - Required)

During mission execution, **all commands use packets**:

```csharp
// Power commands (RUNTIME)
SendPacketToDrone(packetReceiver1, "power_on", "");

// Navigation commands (RUNTIME)
SendTargetPacket(packetReceiver1, targetTransform);

// Formation commands (RUNTIME)
SendPacketToDrone(packetReceiver2, "formation_active", "true");
```

**Why?** Simulates realistic radio communication.

---

## Fallback Mode

AutoSwarm automatically falls back to direct access if:

1. **`usePacketCommunication = false`** - Manual disable
2. **Missing PacketReceiver** - Not all drones have component
3. **PacketHandler not found** - System not set up

**Console Warning:**
```
[AutoSwarm] Packet communication enabled but not all drones have PacketReceiver. Falling back to direct access.
```

**Code Example:**
```csharp
if (usePacketCommunication && (packetReceiver1 == null || packetReceiver2 == null || packetReceiver3 == null))
{
    Debug.LogWarning("[AutoSwarm] Falling back to direct access.");
    usePacketCommunication = false;
}
```

---

## Complete Message Type Reference

### AutoSwarm → Drone Commands

| Message Type | Sender | Recipient | Data | Purpose |
|-------------|--------|-----------|------|---------|
| `set_target` | AutoSwarm | drone1/2/3 | `x,y,z` | Navigate to position |
| `set_autopilot` | AutoSwarm | drone1/2/3 | `true`/`false` | Toggle autopilot |
| `power_on` | AutoSwarm | drone1/2/3 | (empty) | Turn on engine |
| `power_off` | AutoSwarm | drone1/2/3 | (empty) | Turn off engine |
| `set_speed_multiplier` | AutoSwarm | drone1/2/3 | `0.5` | Set speed factor |
| `rotate_to_direction` | AutoSwarm | drone1/2/3 | `x,y,z` | Rotate to direction |
| `formation_offset` | AutoSwarm | drone2/3 | `x,y,z` | Set formation offset |
| `formation_active` | AutoSwarm | drone1/2/3 | `true`/`false` | Enable formation |
| `checkpoint_reached` | AutoSwarm | drone1/2/3 | (empty) | Notify checkpoint |

### Drone → Drone Communication

| Message Type | Sender | Recipient | Data | Purpose |
|-------------|--------|-----------|------|---------|
| `leader_state` | Leader | broadcast | `pos\|vel\|dir` | Broadcast state |
| `position_report` | Any drone | broadcast | `x,y,z` | Share position |
| `slowdown_request` | Wingman | Leader | (empty) | Request slow down |
| `status_request` | Any | Specific drone | (empty) | Request status |
| `status_response` | Any | Requester | `pos\|speed` | Reply with status |

---

## Benefits of External Packet Communication

### Realism
✅ Simulates command-and-control radio links  
✅ Can add signal delay for ground-to-air latency  
✅ Can simulate command packet loss  
✅ Drones operate autonomously with external commands  

### Modularity
✅ Easy to add new external systems (other controllers)  
✅ Multiple ground stations can send commands  
✅ Commands are logged and traceable  
✅ External systems don't need drone references  

### Testing
✅ Can replay command sequences from logs  
✅ Can inject test commands at runtime  
✅ Can monitor all external communication  
✅ Can simulate communication failures  

### Future Extensions
✅ Command queuing and prioritization  
✅ Command acknowledgment/confirmation  
✅ Encrypted command validation  
✅ Multi-swarm coordination  

---

## Performance

### Packet Load (3 drones)

**Formation Flight:**
- Leader broadcasts at 20 Hz = 20 packets/sec
- AutoSwarm commands = ~5 packets/sec
- **Total:** ~25 packets/sec broadcast
- **Deliveries:** ~75 deliveries/sec (25 × 3 receivers)

**Very lightweight!** ✅

### Optimization Tips

1. **Batch commands** - Send multiple settings in one packet
2. **Command only on change** - Don't resend identical commands
3. **Use broadcast wisely** - Target specific drones when possible
4. **Throttle non-critical commands** - Position reports, status updates

---

## Example: Custom External System

Want to create your own drone controller? Here's how:

```csharp
using UnityEngine;

public class MyDroneController : MonoBehaviour
{
    void CommandDroneTo(string droneId, Vector3 position)
    {
        string data = $"{position.x},{position.y},{position.z}";
        Packet packet = new Packet("MyController", droneId, "set_target", data);
        PacketHandler.Instance?.BroadcastPacket(packet);
    }

    void PowerOnAllDrones()
    {
        Packet packet = new Packet("MyController", "broadcast", "power_on", "");
        PacketHandler.Instance?.BroadcastPacket(packet);
    }

    void RequestDroneStatus(string droneId)
    {
        Packet packet = new Packet("MyController", droneId, "status_request", "");
        PacketHandler.Instance?.BroadcastPacket(packet);
    }
}
```

**No PacketReceiver needed!** External systems just send packets directly via PacketHandler.

---

## Debugging External Communication

### Enable Logging

**PacketHandler:**
- ✓ Log All Packets

**Console Output:**
```
[PacketHandler] Broadcasting: Packet[abc123] from:AutoSwarm to:drone1 type:set_target @10.5s
[PacketHandler] Broadcasting: Packet[def456] from:AutoSwarm to:drone2 type:power_on @10.6s
```

**PacketReceiver:**
- ✓ Log Received Packets

**Console Output:**
```
[drone1] Received: Packet[abc123] from:AutoSwarm to:drone1 type:set_target @10.5s
[drone2] Received: Packet[def456] from:AutoSwarm to:drone2 type:power_on @10.6s
```

### Verify Commands

Check that:
1. AutoSwarm sends packets (PacketHandler logs)
2. Drones receive packets (PacketReceiver logs)
3. Drones execute commands (DroneComputer behavior)

---

## Migration Guide

### Old AutoSwarm Code

```csharp
droneComputer1.SetTarget(target);
droneComputer1.PowerOnEngine();
droneComputer1.autoPilot = true;
```

### New AutoSwarm Code

```csharp
SendTargetPacket(packetReceiver1, target);
SendPacketToDrone(packetReceiver1, "power_on", "");
SendPacketToDrone(packetReceiver1, "set_autopilot", "true");
```

### Keep Setup Code

```csharp
// STILL OKAY - this is setup, not runtime communication:
droneComputer1 = drone1.GetComponent<DroneComputer>();
formationKeeper1.SetFormationRole(FormationKeeper.FormationRole.Leader);
```

---

## Summary

AutoSwarm and external systems now communicate with drones via packets:

✅ **All runtime commands** use packet system  
✅ **Setup/configuration** still uses direct references  
✅ **Fallback mode** if packets unavailable  
✅ **Easy to extend** with new commands  
✅ **Realistic simulation** of radio communication  

The ground station talks to drones like a real command-and-control system! 📡🚁
