# Drone System Architecture Overview

## 🎯 Three-Layer Architecture

### 1️⃣ Drone Hardware Layer
**Location:** `/Assets/Scripts/DroneHardware/`

Physical drone components that interact directly with Unity's physics system.

```
DroneController.cs              ⭐ Main hardware controller
├─ Rigidbody physics
├─ Force application
├─ Speed limiting
└─ Blade animations

DroneCrashDetection.cs          Collision sensor
DroneStabilization.cs           Gyroscope/auto-leveling
VisualDroneFollower.cs          Visual mesh follower
VisualTilt.cs                   Visual tilt effects
```

**Key Principle:** Only accessed by DroneComputer, never by external systems.

---

### 2️⃣ Drone Software Layer
**Location:** `/Assets/Scripts/DroneSoftware/`

Onboard flight computer and autonomous algorithms running on each drone.

```
DroneComputer.cs                ⭐ Main flight computer
├─ Autopilot
├─ Target following
├─ Speed adaptation
└─ DroneController interface

FormationKeeper.cs              ⭐ Formation coordination (NEW!)
├─ Leader behavior (monitor wingmen)
├─ Wingman behavior (maintain offset)
└─ Peer-to-peer communication

MovementDecay.cs                Momentum decay algorithm
KeyboardInput.cs                Manual control (debug helper)
```

**Key Principle:** Each drone runs its own software independently.

---

### 3️⃣ External Communication Layer
**Location:** `/Assets/Scripts/ExternalComm/`

Ground control systems that coordinate the swarm via high-level commands.

```
AutoSwarm.cs                    ⭐ Mission controller
├─ Checkpoint sequencing
├─ Formation setup
└─ High-level coordination

TriangleFormationGenerator.cs   Formation geometry calculator
CheckpointBehaviorHandler.cs    Checkpoint analysis
DronePathPredictor.cs           Path prediction/visualization
```

**Key Principle:** Only communicates through DroneComputer's public API.

---

## 🔄 Formation Keeping: Distributed Architecture

### Old Centralized Approach ❌
```
┌─────────────────────────────────┐
│      AutoSwarm                  │
│  (External Computer)            │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│   FormationKeeper (External)    │
│   ├─ Calculates all positions   │
│   ├─ Pushes corrections         │
│   └─ Runs every frame           │
└────────────┬────────────────────┘
             │
      ┌──────┴──────┬──────────┐
      ▼             ▼          ▼
   Drone 1      Drone 2    Drone 3
```

### New Distributed Approach ✅
```
┌─────────────────────────────────┐
│      AutoSwarm                  │
│  (External Computer)            │
│  - Configures formation         │
│  - Sets roles once              │
└─────────────────────────────────┘
             │ (one-time setup)
      ┌──────┴──────┬──────────┐
      ▼             ▼          ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Drone 1  │  │ Drone 2  │  │ Drone 3  │
│ (Leader) │◄─┤(LeftWing)│  │(RightWg) │
│          │  │          │  │          │
│ FormKpr  │  │ FormKpr  │◄─┤ FormKpr  │
│ - Monitor│  │ - Offset │  │ - Offset │
│ wingmen  │  │ from ldr │  │ from ldr │
└──────────┘  └──────────┘  └──────────┘
     ▲             ▲              ▲
     └─────────────┴──────────────┘
      Drones query each other directly
```

**Benefits:**
- ✅ Realistic peer-to-peer communication
- ✅ Each drone autonomous
- ✅ Scales better (add more drones easily)
- ✅ No single point of failure
- ✅ Lower communication overhead

---

## 📡 Communication Patterns

### ✅ Allowed
```csharp
// External → DroneComputer
autoSwarm.SetTarget(checkpoint);
droneComputer.PowerOnEngine();
droneComputer.SetSpeedMultiplier(0.5f);

// Drone → Drone (via DroneComputer)
Vector3 leaderPos = leaderDrone.GetPosition();
float leaderSpeed = wingmanDrone.GetMaxSpeed();
```

### ❌ Forbidden
```csharp
// External → DroneController (NEVER!)
droneController.PowerOnEngine();  // ❌
droneController.maxSpeed = 5f;    // ❌

// External → FormationKeeper direct control
formationKeeper.ApplyCorrections(); // ❌ (now runs autonomously)
```

---

## 🚁 How Formation Keeping Works

### Setup Phase (AutoSwarm)
```csharp
// 1. Assign roles
formationKeeper1.SetFormationRole(FormationRole.Leader);
formationKeeper2.SetFormationRole(FormationRole.LeftWing);
formationKeeper3.SetFormationRole(FormationRole.RightWing);

// 2. Set references (who talks to who)
formationKeeper2.SetLeaderReference(droneComputer1);
formationKeeper3.SetLeaderReference(droneComputer1);
formationKeeper1.SetWingmenReferences(new[] { droneComputer2, droneComputer3 });

// 3. Configure formation parameters
formationKeeper2.SetFormationOffset(new Vector3(2.5f, 0, -1.5f));
formationKeeper3.SetFormationOffset(new Vector3(-2.5f, 0, -1.5f));

// 4. Activate
formationKeeper1.SetFormationActive(true);
formationKeeper2.SetFormationActive(true);
formationKeeper3.SetFormationActive(true);
```

### Runtime (Autonomous)
Each drone's FormationKeeper runs independently every FixedUpdate:

**Leader:**
1. Queries wingmen positions
2. Calculates lag distance
3. Applies slowdown if wingmen are far behind

**Wingman:**
1. Queries leader position
2. Calculates ideal formation position
3. Applies correction force to maintain offset

---

## 📂 Complete File Structure

```
/Assets/Scripts/
├─ DroneHardware/               # Layer 1: Physics & Sensors
│  ├─ DroneController.cs        ⭐ Main hardware
│  ├─ DroneCrashDetection.cs
│  ├─ DroneStabilization.cs
│  ├─ VisualDroneFollower.cs
│  └─ VisualTilt.cs
│
├─ DroneSoftware/               # Layer 2: Onboard Algorithms
│  ├─ DroneComputer.cs          ⭐ Flight computer
│  ├─ FormationKeeper.cs        ⭐ Formation (NEW!)
│  ├─ MovementDecay.cs
│  └─ KeyboardInput.cs
│
├─ ExternalComm/                # Layer 3: Ground Control
│  ├─ AutoSwarm.cs              ⭐ Mission control
│  ├─ TriangleFormationGenerator.cs
│  ├─ CheckpointBehaviorHandler.cs
│  ├─ DronePathPredictor.cs
│  └─ FormationKeeperOLD.cs     (deprecated)
│
└─ Other Systems/               # Non-drone systems
   ├─ CameraFollow.cs
   ├─ BillboardRotation.cs
   └─ DroneMomentumDisplay.cs
```

---

## 🎓 Design Principles

1. **Hardware Isolation**: DroneController only accessed by DroneComputer
2. **Software Autonomy**: Each drone runs its own decision-making algorithms
3. **Minimal External Control**: External systems send high-level commands, not micro-management
4. **Peer Communication**: Drones communicate directly for formation keeping
5. **Clean Interfaces**: All communication through well-defined public methods

---

## 🔧 Migration from Old System

If you have existing scenes with the old centralized FormationKeeper:

1. Remove `FormationKeeper` component from AutoSwarm GameObject
2. Add `FormationKeeper` to each Drone GameObject
3. AutoSwarm will automatically configure them in `Start()`
4. Old `FormationKeeperOLD.cs` marked as deprecated for reference

---

## 📊 Performance Benefits

**Old System:**
- 1 external FormationKeeper updates 3 drones every frame
- Constant query: external → drone computer → drone controller
- High communication overhead

**New System:**
- Each drone's FormationKeeper runs independently
- Direct peer queries: drone computer → drone computer
- Lower latency, more responsive formation keeping

---

*Last Updated: [Current Date]*
*Architecture Version: 2.0 - Distributed Formation Keeping*
