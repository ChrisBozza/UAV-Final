# Drone Architecture Refactoring

## Overview
Refactored the drone system to enforce proper architectural boundaries:
- **DroneController** = Hardware layer (physics, motors, sensors)
- **DroneComputer** = Onboard software (autopilot, formation keeping)
- **AutoSwarm/FormationKeeper/TriangleFormationGenerator** = External control systems

## Key Changes

### 1. DroneComputer - New Interface Methods
Added proper interface methods to DroneComputer so external systems don't need to access DroneController:

**Power Control:**
- `PowerOnEngine()` - Turn on the drone engines
- `PowerOffEngine()` - Turn off the drone engines
- `IsEnginePowered()` - Check engine status

**Speed Control:**
- `SetMaxSpeed(float speed)` - Set absolute max speed
- `SetSpeedMultiplier(float multiplier)` - Set speed as multiplier of base speed
- `GetMaxSpeed()` - Get current max speed

**State Queries:**
- `GetPosition()` - Get drone position
- `GetVelocity()` - Get drone velocity

**Formation Keeping (Onboard):**
- `SetFormationKeepingEnabled(bool enabled)` - Enable/disable formation corrections
- `SetFormationOffset(Vector3 offset, Transform leader)` - Set desired offset from leader

### 2. Formation Keeping Now Runs Onboard
Formation keeping logic moved into DroneComputer's FixedUpdate:
- Each drone maintains its own formation offset
- Corrections applied automatically when enabled
- No external system needs to constantly push corrections

### 3. External Systems Use Clean Interfaces
- AutoSwarm now uses `drone.PowerOnEngine()` instead of `drone.droneController.PowerOnEngine()`
- FormationKeeper uses `drone.GetVelocity()` instead of `drone.droneController.GetMomentum()`
- All speed changes go through DroneComputer

## Migration Notes

If you want to fully move formation keeping to DroneComputer:
1. External systems set formation parameters via `SetFormationOffset()`
2. Enable formation keeping via `SetFormationKeepingEnabled(true)`
3. DroneComputer handles corrections automatically
4. FormationKeeper can be simplified or removed entirely

## Benefits
- **Clear separation of concerns**: Hardware vs. Software vs. External Control
- **Realistic simulation**: External computer can't directly manipulate hardware
- **Easier testing**: Can test DroneComputer without DroneController
- **More maintainable**: Changes to DroneController don't affect external systems
