# Formation Orientation Fix

## Problem 1: Wrong Direction ✅ FIXED
The wingman drones (red and green) were flying in the wrong direction because the formation wasn't properly oriented toward the next checkpoint.

## Problem 2: Teleporting Ideal Position ✅ FIXED
The cyan gizmo sphere (ideal position) was constantly teleporting above and below the drones.

## Root Causes

### Issue 1: Static Direction
The onboard `FormationKeeper` was using static `directionToTarget` that was set once during configuration, but formations should dynamically orient based on the **leader's current velocity direction**.

### Issue 2: Vertical Velocity Components
The formation calculation was using the full 3D velocity vector, including vertical (Y) components. Since drones constantly adjust altitude for stabilization, this caused the formation direction to tilt up and down every frame.

## Solution

### Key Change 1: Dynamic Direction Calculation
Instead of using a pre-set direction, wingman drones now calculate their ideal position based on:
1. **Leader's velocity direction** (real-time)
2. **Local formation offset** (left/right, forward/back from leader)

### Key Change 2: Horizontal-Only Velocity
Formation orientation now uses **only horizontal velocity components (X and Z)**, ignoring vertical (Y) motion:

```csharp
// Extract only horizontal velocity
Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);

// Use horizontal velocity for direction
Vector3 forwardDirection = horizontalVelocity.magnitude > 0.1f 
    ? horizontalVelocity.normalized 
    : leaderDrone.transform.forward;
```

This ensures the formation stays level even when the leader is ascending/descending.

### Updated Code Flow

**Before (Static Direction + Full 3D Velocity):**
```csharp
// Set once during configuration - WRONG!
formationKeeper.SetDirectionToTarget(directionToNextCheckpoint);

// Used full velocity including vertical - UNSTABLE!
Vector3 forwardDirection = leaderVelocity.normalized;
Vector3 worldOffset = formationOffset.z * -directionToTarget + formationOffset.x * right;
```

**After (Dynamic Direction + Horizontal Velocity Only):**
```csharp
// Calculate horizontal velocity only
Vector3 horizontalVelocity = new Vector3(leaderVelocity.x, 0f, leaderVelocity.z);

// Use horizontal direction (stays level!)
Vector3 forwardDirection = horizontalVelocity.magnitude > 0.1f 
    ? horizontalVelocity.normalized 
    : leaderDrone.transform.forward;

// Calculate formation position relative to leader's movement
Vector3 right = Vector3.Cross(Vector3.up, forwardDirection).normalized;
Vector3 back = -forwardDirection;
Vector3 worldOffset = (right * localFormationOffset.x) + (back * localFormationOffset.z);
```

### Formation Offset Coordinate System

The `localFormationOffset` uses a local coordinate system relative to the leader:
- **X axis**: Right (+) / Left (-)
- **Y axis**: Up (+) / Down (-) (currently unused)
- **Z axis**: Behind (+) / Forward (-)

**Example:**
- Left Wing: `(2.5, 0, 3)` = 2.5m to the right, 3m behind
- Right Wing: `(-2.5, 0, 3)` = 2.5m to the left, 3m behind

This gets transformed to world space based on leader's current heading.

## Benefits

1. **Dynamic Orientation**: Formation automatically orients to wherever the leader is heading
2. **Smooth Turns**: Formation adjusts during turns without reconfiguration
3. **Stable Positioning**: Ignoring vertical velocity prevents formation from tilting up/down
4. **Realistic**: Mimics real aerial formations that maintain horizontal orientation
5. **Simpler Setup**: AutoSwarm sets offset once, drones handle orientation

## Why Horizontal Velocity Only?

Drones constantly adjust their altitude due to:
- Physics stabilization
- Minor turbulence from Rigidbody
- Autopilot corrections
- Formation corrections

If we used full 3D velocity, the formation would:
- ❌ Tilt up when leader rises slightly
- ❌ Tilt down when leader descends slightly  
- ❌ Cause ideal position to "teleport" vertically
- ❌ Make wingmen chase unstable target positions

By using only horizontal velocity:
- ✅ Formation stays level (like real aircraft)
- ✅ Ideal positions are stable in height
- ✅ Wingmen maintain smooth horizontal formation
- ✅ Vertical spacing is maintained separately

## Files Modified

- `/Assets/Scripts/DroneSoftware/FormationKeeper.cs`
  - Removed `directionToTarget` field
  - Updated `ApplyWingmanBehavior()` to use dynamic direction
  - Updated `OnDrawGizmos()` for accurate visualization

- `/Assets/Scripts/ExternalComm/AutoSwarm.cs`
  - Updated `ConfigureFormation()` to set local offsets directly
  - Removed `SetDirectionToTarget()` calls

## Testing

Run the scene and verify:
1. ✅ All three drones fly toward checkpoints
2. ✅ Wingmen maintain left/right positions relative to leader
3. ✅ Formation rotates smoothly during turns
4. ✅ Cyan gizmo spheres show correct ideal positions for wingmen
