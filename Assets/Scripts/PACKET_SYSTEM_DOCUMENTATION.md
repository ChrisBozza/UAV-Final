# Packet Communication System

## Overview

The packet system simulates radio/wireless communication between drones and external systems. All communication is broadcast-based, meaning every receiver gets every packet and must filter for relevant messages.

## Architecture

```
External System or Drone A
         ↓
    Sends Packet
         ↓
  PacketHandler (Singleton)
    Broadcasts to ALL
         ↓
    ┌────┴────┬────────┐
    ↓         ↓        ↓
Receiver 1  Receiver 2  Receiver 3
    ↓         ↓        ↓
 Filter    Filter    Filter
 Process   Ignore    Process
```

## Components

### 1. Packet (Data Structure)

**Location:** `/Assets/Scripts/DroneSoftware/Packet.cs`

Simple data container for all communications:

```csharp
public class Packet
{
    public string packetId;      // Unique identifier (auto-generated GUID)
    public float timestamp;       // Time.time when packet was created
    public string sender;         // ID of sender
    public string recipient;      // ID of recipient or "broadcast"/"all"
    public string messageType;    // Type of message (e.g., "formation_update")
    public string data;           // Message payload (string format)
}
```

**Creating a Packet:**
```csharp
Packet packet = new Packet(
    sender: "drone1",
    recipient: "drone2",        // or "broadcast" or "all"
    messageType: "formation_update",
    data: "x:2.5,z:3.0"
);
```

### 2. PacketReceiver (Onboard Software)

**Location:** `/Assets/Scripts/DroneSoftware/PacketReceiver.cs`

Attached to each drone. Handles all incoming communication.

**Key Features:**
- Auto-registers with PacketHandler on Awake
- Filters packets by recipient ID
- Queues packets for processing in Update
- Processes packets based on messageType
- Can send packets via `SendPacket()`

**Public Methods:**
```csharp
// Send a packet through the system
public void SendPacket(string recipient, string messageType, string data = "")

// Receive a packet (called by PacketHandler)
public void ReceivePacket(Packet packet)
```

**Built-in Message Types:**
- `formation_update` - Formation configuration changes
- `checkpoint_reached` - Leader reached checkpoint
- `slowdown_request` - Request wingman to slow down
- `position_report` - Broadcast position data
- `status_request` - Request status information
- `status_response` - Response to status request

**Adding New Message Types:**

Extend the `ProcessPacket()` method:

```csharp
switch (packet.messageType)
{
    case "your_new_type":
        HandleYourNewType(packet);
        break;
    
    // ... existing cases
}

private void HandleYourNewType(Packet packet)
{
    // Your logic here
    Debug.Log($"Handling new type: {packet.data}");
}
```

### 3. PacketHandler (Hardware/Simulation)

**Location:** `/Assets/Scripts/DroneHardware/PacketHandler.cs`

Singleton that simulates the radio broadcast medium.

**Key Features:**
- Singleton pattern (accessible via `PacketHandler.Instance`)
- Maintains list of all receivers
- Broadcasts packets to ALL receivers simultaneously
- Optional signal delay simulation
- Tracks statistics (packets sent/delivered)

**Public Methods:**
```csharp
// Broadcast a packet to all receivers
public void BroadcastPacket(Packet packet)

// Get number of registered receivers
public int GetReceiverCount()

// Reset statistics
public void ClearStatistics()
```

**Inspector Settings:**
- `logAllPackets` - Debug log every broadcast
- `signalDelay` - Simulated transmission delay (seconds)
- Statistics display (total sent/delivered)

## Usage Examples

### Example 1: Leader Notifies Checkpoint Reached

```csharp
// In leader's PacketReceiver or other script:
PacketReceiver receiver = GetComponent<PacketReceiver>();
receiver.SendPacket("broadcast", "checkpoint_reached", "");
```

All drones receive this and call `formationKeeper.OnCheckpointReached()`.

### Example 2: External System Requests Drone Status

```csharp
// From AutoSwarm or other external script:
Packet statusRequest = new Packet(
    sender: "AutoSwarm",
    recipient: "drone2",
    messageType: "status_request",
    data: ""
);
PacketHandler.Instance.BroadcastPacket(statusRequest);
```

The drone receives, processes, and sends back a `status_response`.

### Example 3: Drone-to-Drone Formation Update

```csharp
// Leader sends formation offset to wingman:
string formationData = $"offset_x:{offset.x},offset_z:{offset.z}";
receiver.SendPacket("drone2", "formation_update", formationData);
```

### Example 4: Broadcast Position Report

```csharp
// Any drone can broadcast its position:
Vector3 pos = droneComputer.GetPosition();
string posData = $"{pos.x:F2},{pos.y:F2},{pos.z:F2}";
receiver.SendPacket("all", "position_report", posData);
```

All receivers get this packet and can log/process the position.

## Setup Instructions

### 1. Scene Setup

Add PacketHandler to a GameObject in the scene (e.g., on the Swarm Controller):

```
Swarm Controller
  - AutoSwarm
  - TriangleFormationGenerator
  - PacketHandler          ← Add this component
```

### 2. Drone Setup

PacketReceiver auto-adds itself when referenced, but you can manually add it:

```
BlueDrone/BlueInvisDrone
  - DroneComputer
  - FormationKeeper
  - PacketReceiver         ← Add this component
```

Set the `receiverId` to a unique identifier (e.g., "drone1", "drone2", "drone3").

### 3. External System Setup

Any external script can send packets:

```csharp
public class AutoSwarm : MonoBehaviour
{
    void SendToAllDrones(string messageType, string data)
    {
        Packet packet = new Packet("AutoSwarm", "broadcast", messageType, data);
        PacketHandler.Instance?.BroadcastPacket(packet);
    }
}
```

## Data Encoding Guidelines

Since packet data is a string, use simple formats:

### Key-Value Pairs
```csharp
data = "x:2.5,y:10.0,z:3.0"
data = "role:leader,active:true"
```

### Comma-Separated Values
```csharp
data = "2.5,10.0,3.0"
```

### Pipe-Delimited Sections
```csharp
data = "position:2.5,10,3|velocity:1.2"
```

### JSON (for complex data)
```csharp
data = "{\"position\":{\"x\":2.5,\"y\":10},\"active\":true}"
// Parse with JsonUtility or simple string parsing
```

## Exception: GameObject References

Setup communications (not runtime) can include GameObject references:

```csharp
// During initialization in AutoSwarm:
formationKeeper.SetLeaderReference(leaderDroneComputer);
formationKeeper.SetWingmenReferences(wingmen);
```

This is **not** done through packets - it's direct assignment during scene setup.

## Best Practices

1. **Use descriptive messageType strings** - Makes debugging easier
2. **Keep data payloads simple** - Strings and numbers only for runtime
3. **Use "broadcast" for announcements** - All drones should hear
4. **Use specific recipient IDs for commands** - Direct drone-to-drone
5. **Log packets during development** - Enable `logReceivedPackets` in PacketReceiver
6. **Simulate realistic delays** - Set `signalDelay` in PacketHandler for testing
7. **Handle missing PacketHandler gracefully** - Always check for null

## Debugging

### Enable Logging

**PacketHandler:**
- Check `logAllPackets` to see all broadcasts

**PacketReceiver:**
- Check `logReceivedPackets` to see received packets
- Check `logIgnoredPackets` to see filtered packets

### Check Statistics

In PacketHandler inspector:
- `Total Packets Sent` - How many broadcasts
- `Total Packets Delivered` - How many deliveries (sent × receivers)

### Common Issues

**"PacketHandler not found"**
- Ensure PacketHandler exists in the scene
- It must be active and enabled

**"Packets not being received"**
- Check receiver IDs match
- Use "broadcast" or "all" for testing
- Enable logging to verify filtering

**"Duplicate PacketHandlers"**
- Only one PacketHandler should exist per scene
- It will auto-destroy duplicates

## Future Enhancements

Possible additions to the system:

- **Signal strength/range** - Packets only received within range
- **Packet loss simulation** - Random packet drops
- **Encryption/security** - Validate sender/recipient
- **Priority queues** - Process critical packets first
- **Bandwidth limits** - Throttle packet sending
- **Message acknowledgments** - Confirm receipt
- **Multi-part messages** - Split large data across packets
