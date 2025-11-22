# Autonomous Checkpoint System

## Overview

The checkpoint system has been updated to be fully autonomous. Drones now manage their own `reachedTarget` flags without requiring external packet notifications or manual flag resets.

## How It Works

### Hysteresis-Based Target Detection

The system uses **hysteresis** to prevent flag flickering and ensure reliable checkpoint detection:

```
Distance:  |-------|-------|-------|-------|-------|
           0m      2m      3m      4m      5m

Flag:      TRUE    TRUE    ???     FALSE   FALSE
                   ↑       ↑       ↑
                   Set     Dead    Reset
                   True    Zone    False
```

**Behavior:**
- **< 2m**: Flag set to `true` (target reached)
- **2m - 3m**: Flag unchanged (hysteresis zone)
- **> 3m**: Flag reset to `false` (target not reached)

**Why Hysteresis?**
1. **Prevents Flickering**: Drone hovering at 2.1m won't cause flag to toggle rapidly
2. **Auto-Reset**: When new far target assigned, flag automatically resets when distance > 3m
3. **Stable Detection**: Clear thresholds for "reached" vs "not reached" states

### Drone Self-Management

**DroneComputer** autonomously handles target reaching:

1. **Target Assignment** - When a new target is set, the drone resets its flag:
   ```csharp
   public void SetTarget(Transform t) {
       target = t;
       targetPosition = null;
       reachedTarget = false;  // Explicit reset on assignment
   }
   ```

2. **Target Monitoring** - The `AutoCheck()` coroutine continuously monitors distance with hysteresis:
   ```csharp
   if (HasTarget())
   {
       Vector3 toTarget = GetTargetPosition() - droneController.GetPosition();
       float dist = toTarget.magnitude;
       
       if (dist < 2f) {
           reachedTarget = true;   // Target reached
       }
       else if (dist > 3f) {
           reachedTarget = false;  // Too far from target
       }
       // Between 2m-3m: no change (hysteresis prevents flickering)
   }
   ```

3. **Swarm Coordination** - AutoSwarm simply checks if all drones reached their targets:
   ```csharp
   while (!SwarmReachedTarget()) {
       yield return null;
   }
   ```

## Changes Made

### DroneComputer.cs

**Added hysteresis-based flag management in AutoCheck():**
```csharp
// Old: Flag only set to true, never reset
if (dist < 2f) {
    reachedTarget = true;
}

// New: Dual-threshold with hysteresis
if (dist < 2f) {
    reachedTarget = true;   // Reached threshold
}
else if (dist > 3f) {
    reachedTarget = false;  // Reset threshold
}
// 2m-3m: hysteresis zone (no change)
```

**Moved flag reset to end of SetTarget methods:**
- `reachedTarget = false` is the **last operation** in `SetTarget()` and `SetTargetPosition()`
- This ensures the flag is reset **after** the target is assigned
- Works as backup/immediate reset when new target assigned

**Dual Reset Mechanism:**
1. **Explicit Reset**: `SetTarget()` / `SetTargetPosition()` immediately sets `reachedTarget = false`
2. **Continuous Monitoring**: `AutoCheck()` continuously resets flag when `dist > 3m`
3. **Redundancy**: Even if one mechanism fails, the other ensures correct behavior

### AutoSwarm.cs

**Removed manual flag management:**
- ❌ Removed `ResetReachedTargetFlags()` method
- ❌ Removed `NotifyCheckpointReached()` method
- ✅ `SetNewSwarmTarget()` no longer manually resets flags
- ✅ `SetIndividualTargets()` no longer manually resets flags

**Simplified checkpoint flow:**
```csharp
// Old system (packet-based notification):
SetIndividualTargets(...);
while (!SwarmReachedTarget()) { yield return null; }
NotifyCheckpointReached();  // ❌ No longer needed

// New system (autonomous):
SetIndividualTargets(...);
while (!SwarmReachedTarget()) { yield return null; }
// Drones already know they reached target ✅
```

### PacketReceiver.cs

**Removed obsolete packet handler:**
- ❌ Removed `checkpoint_reached` case from switch statement
- ❌ Removed `HandleCheckpointReached()` method
- The `set_target` packet handler already calls `SetTargetPosition()`, which resets the flag

## Packet Communication Flow

### Without Packet System (Direct Mode)

```
AutoSwarm.SetIndividualTargets()
  └─> droneComputer.SetTarget(transform)
       ├─> target = transform
       ├─> targetPosition = null
       └─> reachedTarget = false ✅
```

### With Packet System

```
AutoSwarm.SendTargetPacket()
  └─> PacketHandler.BroadcastPacket("set_target", position)
       └─> PacketReceiver.HandleSetTarget()
            └─> droneComputer.SetTargetPosition(position)
                 ├─> target = null
                 ├─> targetPosition = position
                 └─> reachedTarget = false ✅
```

Both paths automatically reset the flag when a new target is assigned!

## Benefits

### 1. Autonomous Behavior
- Drones manage their own state
- No external coordination required
- Works identically in packet and direct modes

### 2. Latency Resilient
- No waiting for `checkpoint_reached` packets
- Works perfectly with distance-based packet delays
- Each drone independently determines when it reaches target

### 3. Simpler Code
- Removed 2 methods from AutoSwarm
- Removed 1 packet handler from PacketReceiver
- Clearer responsibility separation

### 4. Consistent State
- Flag reset happens atomically with target assignment
- No race conditions between reset and target setting
- Single source of truth per drone

## Testing Scenarios

### Scenario 1: Normal Checkpoint Navigation
```
1. AutoSwarm sets formation targets
2. Each drone receives target (via packet or direct)
3. DroneComputer.SetTargetPosition() resets reachedTarget = false
4. Drone flies toward target
5. When distance < 2m, AutoCheck() sets reachedTarget = true
6. AutoSwarm.SwarmReachedTarget() detects all drones ready
7. AutoSwarm proceeds to next checkpoint
```

### Scenario 2: Takeoff Sequence
```
1. AutoSwarm creates individual takeoff positions
2. SetIndividualTargets() sends positions
3. Each drone resets its own flag when receiving target
4. Drones ascend independently
5. Each sets reachedTarget = true when within 2m of hover position
6. AutoSwarm waits for all flags, then stabilizes swarm
```

### Scenario 3: Landing Sequence
```
1. AutoSwarm sets hover positions above landing
2. Drones reset flags and fly to hover positions
3. StabilizeForLanding() waits for position stability
4. AutoSwarm sets final landing positions
5. Drones reset flags again and descend
6. Each drone sets reachedTarget = true at ground
7. AutoSwarm powers off all drones
```

### Scenario 4: With Packet Delays (343 m/s propagation)
```
Drone 1 is 100m from AutoSwarm → receives target in 291ms
Drone 2 is 300m from AutoSwarm → receives target in 875ms
Drone 3 is 500m from AutoSwarm → receives target in 1458ms

Result:
- Each drone resets its flag when IT receives the packet
- Drone 1 starts flying 584ms before Drone 2
- Drone 3 starts flying 1.17 seconds after Drone 1
- All drones still coordinate via reachedTarget flags
- AutoSwarm correctly waits for all drones to reach their targets
```

## Key Principle

**Drones are autonomous agents.** They manage their own navigation state based on sensor data (distance to target). The swarm controller (AutoSwarm) only coordinates movement and waits for consensus, but doesn't micromanage individual drone state.

This matches real-world swarm behavior where:
- Each agent has local sensors and decision-making
- Communication is for coordination, not state control
- Latency doesn't break the system
- Agents operate independently and synchronize via observed state
