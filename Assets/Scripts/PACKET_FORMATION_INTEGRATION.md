# Packet-Based Formation Communication

## Overview

The FormationKeeper system now uses **packet-based communication** for all drone-to-drone formation coordination. The leader broadcasts its state via radio simulation, and wingmen receive and cache this data for formation calculations.

## How It Works

### Before (Direct Access)
```
Wingman → Direct call → Leader.GetPosition()
Wingman → Direct call → Leader.GetVelocity()
```

**Problem:** Unrealistic - drones can't magically know each other's exact state instantly.

### After (Packet Communication)
```
Leader → Broadcast "leader_state" packet (every 0.05s)
            ↓
      PacketHandler
            ↓
    ┌───────┴───────┐
    ↓               ↓
Wingman 1      Wingman 2
Cache data     Cache data
Use cached     Use cached
```

**Benefit:** Realistic radio communication with configurable delay, loss, and timeout.

## Leader Broadcast System

### Leader Behavior

**Every FixedUpdate (if packet communication enabled):**

1. **Check broadcast timer** - Only broadcast at configured rate (default 20Hz / 0.05s)
2. **Gather state data:**
   - Position: `myComputer.GetPosition()`
   - Velocity: `myComputer.GetVelocity()`
   - Target Direction: `myComputer.GetTargetDirection()`

3. **Encode to string:**
   ```csharp
   string data = $"{pos.x},{pos.y},{pos.z}|{vel.x},{vel.y},{vel.z}|{dir.x},{dir.y},{dir.z}";
   // Example: "10.5,20.3,5.2|2.1,0.5,1.8|0.7,0,0.7"
   ```

4. **Broadcast packet:**
   ```csharp
   packetReceiver.SendPacket("broadcast", "leader_state", data);
   ```

### Broadcast Settings

Configurable in FormationKeeper inspector:

| Setting | Default | Description |
|---------|---------|-------------|
| `leaderBroadcastRate` | 0.05s | Time between broadcasts (20Hz) |
| `usePacketCommunication` | true | Enable/disable packet system |

**Performance Note:** Broadcasting at 20Hz (0.05s) is a good balance between responsiveness and network load.

## Wingman Reception System

### Wingman Behavior

**When `leader_state` packet received:**

1. **Verify sender** - Check packet.sender matches `leaderDroneId`
2. **Parse data** - Extract position, velocity, and target direction
3. **Cache data** - Store in local variables:
   - `cachedLeaderPosition`
   - `cachedLeaderVelocity`
   - `cachedLeaderTargetDirection`
4. **Update timestamp** - `lastLeaderDataTime = Time.time`

**In FixedUpdate (formation calculations):**

1. **Check if packet data is valid:**
   ```csharp
   if (usePacketCommunication && HasValidLeaderData())
   {
       // Use cached data from packets
       leaderPosition = cachedLeaderPosition;
       leaderVelocity = cachedLeaderVelocity;
   }
   else
   {
       // Fallback to direct access
       leaderPosition = leaderDrone.GetPosition();
       leaderVelocity = leaderDrone.GetVelocity();
   }
   ```

2. **Calculate formation position** using cached data
3. **Apply correction forces**

### Data Timeout

Wingmen check if leader data is recent:

```csharp
bool HasValidLeaderData()
{
    return Time.time - lastLeaderDataTime < leaderDataTimeout;
}
```

**Settings:**

| Setting | Default | Description |
|---------|---------|-------------|
| `leaderDataTimeout` | 0.5s | Max age of cached data |

**If data times out:**
- Wingman falls back to direct access (if available)
- Red gizmo sphere appears around wingman (warning indicator)

## Packet Format

### Message Type: `leader_state`

**Sender:** Leader drone (e.g., "drone1")  
**Recipient:** "broadcast" (all drones)  
**Data Format:** `pos.x,pos.y,pos.z|vel.x,vel.y,vel.z|dir.x,dir.y,dir.z`

**Example:**
```
Packet ID: 3f2a8b9d-4e1c-4a8f-9d3e-1a2b3c4d5e6f
Timestamp: 15.32
Sender: drone1
Recipient: broadcast
Type: leader_state
Data: 12.5,20.8,6.3|2.3,0.1,1.9|0.707,0,0.707
```

**Parsing:**
```csharp
string[] parts = data.Split('|');
// parts[0] = "12.5,20.8,6.3"      → Position
// parts[1] = "2.3,0.1,1.9"        → Velocity
// parts[2] = "0.707,0,0.707"      → Target Direction

Vector3 position = ParseVector3(parts[0]);
// position = (12.5, 20.8, 6.3)
```

## Fallback Mechanism

The system maintains **dual-mode operation** for robustness:

### Packet Mode (Primary)
- Leader broadcasts via packets
- Wingmen use cached packet data
- Realistic communication simulation

### Direct Access Mode (Fallback)
- If `usePacketCommunication = false`
- If PacketReceiver not found
- If packet data times out
- Uses direct `leaderDrone.GetPosition()` calls

**Automatic fallback:** If packet data becomes stale, wingman automatically switches to direct access without crashing.

## Visual Debugging

### Gizmos Indicators

**Leader (Green Sphere):**
- Green wire sphere at leader position
- Yellow lines to each wingman

**Wingman (Normal Operation):**
- Cyan sphere at ideal formation position
- Yellow line to leader position
- White line from self to ideal position

**Wingman (Data Timeout - RED ALERT):**
- **Red wire sphere around wingman** - Data is stale!
- This means packets aren't being received or are timing out

### Troubleshooting Red Gizmo

If you see a red sphere around a wingman:

1. **Check PacketHandler exists** - Must be in scene
2. **Check PacketReceiver on both drones** - Leader and wingman need it
3. **Check receiverId is set** - Leader needs unique ID
4. **Check broadcast rate** - May be too slow (increase rate)
5. **Check timeout setting** - May be too strict (increase timeout)
6. **Enable packet logging** - See if packets are being sent/received

## Setup Checklist

### 1. Scene Setup

✅ Add PacketHandler to scene (e.g., on Swarm Controller)

### 2. Drone Setup

For each drone, add PacketReceiver:

```
BlueDrone/BlueInvisDrone
  - DroneComputer
  - FormationKeeper
  - PacketReceiver ← Set receiverId = "drone1"
```

### 3. FormationKeeper Settings

**Leader Drone:**
- Role: Leader
- Use Packet Communication: ✓
- Leader Broadcast Rate: 0.05
- (Other wingmen/slowdown settings as needed)

**Wingman Drones:**
- Role: LeftWing or RightWing
- Use Packet Communication: ✓
- Leader Data Timeout: 0.5
- Leader Drone: (reference to leader GameObject)
- Local Formation Offset: (set by AutoSwarm)

### 4. AutoSwarm Integration

AutoSwarm continues to set up references normally:
```csharp
formationKeeper.SetLeaderReference(leaderDroneComputer);
formationKeeper.SetWingmenReferences(wingmen);
```

This is **allowed** as setup configuration - the packet system handles runtime communication.

## Performance Considerations

### Broadcast Frequency

| Rate (seconds) | Frequency | Use Case |
|----------------|-----------|----------|
| 0.02 | 50 Hz | Very responsive, higher load |
| **0.05** | **20 Hz** | **Default - good balance** |
| 0.1 | 10 Hz | Lower load, slight lag |
| 0.2 | 5 Hz | Minimal load, noticeable lag |

### Network Load

With 3 drones in formation:
- 1 leader broadcasting at 20Hz = **20 packets/sec**
- Each packet delivered to 3 receivers = **60 deliveries/sec**

This is very lightweight for a simulation!

### Optimization Tips

1. **Only leader broadcasts** - Wingmen only listen
2. **Broadcast on timer** - Not every frame
3. **Simple data format** - String encoding is fast enough
4. **Timeout allows skipped packets** - Not every packet must arrive

## Testing

### Enable Debug Logging

**PacketHandler (in Inspector):**
- ✓ Log All Packets

**PacketReceiver (on each drone):**
- ✓ Log Received Packets

### Expected Console Output

**Leader broadcasting:**
```
[PacketHandler] Broadcasting: Packet[abc123] from:drone1 to:broadcast type:leader_state @15.32s
```

**Wingman receiving:**
```
[drone2] Received: Packet[abc123] from:drone1 to:broadcast type:leader_state @15.32s
[drone3] Received: Packet[abc123] from:drone1 to:broadcast type:leader_state @15.32s
```

### Verify Formation Works

1. **Run the scene**
2. **Watch for red gizmos** - Should not appear
3. **Check formation behavior** - Wingmen should maintain positions
4. **Monitor console** - Should see leader_state packets if logging enabled

## Advantages of Packet System

### Realism
- ✅ Simulates actual radio communication
- ✅ Can add signal delay, loss, interference
- ✅ Drones coordinate via broadcast, not magic

### Flexibility
- ✅ Easy to add new message types
- ✅ Can extend to more complex swarms
- ✅ External systems can listen/inject packets

### Debugging
- ✅ See all communication in logs
- ✅ Track packet delivery statistics
- ✅ Simulate communication failures

### Future Extensions
- ✅ Add packet encryption/authentication
- ✅ Implement signal range limits
- ✅ Simulate bandwidth constraints
- ✅ Add mesh networking between drones

## Disabling Packet Communication

To revert to direct access mode:

**In FormationKeeper Inspector:**
- Uncheck `Use Packet Communication`

The system will automatically fall back to direct `leaderDrone.GetPosition()` calls.

**Use cases for disabling:**
- Debugging formation logic without network complexity
- Performance testing direct access vs packets
- Comparing behavior between modes

## Common Issues

### Issue: Wingmen don't follow leader

**Check:**
1. PacketHandler exists in scene
2. PacketReceiver on all drones
3. receiverId set correctly
4. usePacketCommunication is true
5. No red gizmo spheres (data timeout)

### Issue: Red gizmo appears around wingman

**Cause:** Packet data is timing out

**Solutions:**
1. Increase `leaderDataTimeout` (e.g., 1.0s)
2. Decrease `leaderBroadcastRate` (e.g., 0.02s for 50Hz)
3. Check if leader is broadcasting (enable logging)
4. Verify PacketHandler is active

### Issue: Formation is jerky/laggy

**Cause:** Broadcast rate too slow or packets being delayed

**Solutions:**
1. Decrease `leaderBroadcastRate` (e.g., 0.02s)
2. Check PacketHandler `signalDelay` (should be 0 for instant)
3. Ensure FixedUpdate rate is adequate

### Issue: High CPU usage

**Cause:** Broadcasting too frequently

**Solutions:**
1. Increase `leaderBroadcastRate` (e.g., 0.1s for 10Hz)
2. Consider only broadcasting when state changes significantly
3. Optimize packet parsing

## Advanced: Custom Packet Communication

You can extend the system for other drone-to-drone messages:

### Example: Wingman Requests Slowdown

```csharp
// In wingman's FormationKeeper:
if (isFallingBehind)
{
    packetReceiver.SendPacket(leaderDroneId, "slowdown_request", "");
}
```

### Example: Leader Acknowledges

```csharp
// In leader's PacketReceiver or FormationKeeper:
private void HandleSlowdownRequest(Packet packet)
{
    Debug.Log($"Slowdown requested by {packet.sender}");
    // Apply extra slowdown
}
```

### Example: Wingman Reports Status

```csharp
// Wingman broadcasts its status:
string status = $"fuel:{fuel},battery:{battery}";
packetReceiver.SendPacket("broadcast", "status_report", status);
```

## Summary

The packet-based formation system provides:

✅ **Realistic communication** - Simulates radio broadcasts  
✅ **Configurable performance** - Adjust rate and timeout  
✅ **Robust fallback** - Direct access if packets fail  
✅ **Visual debugging** - Gizmos show communication health  
✅ **Future-proof** - Easy to extend with new messages  

The leader broadcasts at 20Hz, wingmen cache and use the data, and the formation behaves identically to the direct access version but with realistic communication simulation!
