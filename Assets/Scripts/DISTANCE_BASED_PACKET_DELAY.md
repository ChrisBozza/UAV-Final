# Distance-Based Packet Delay System

## Overview

The PacketHandler simulates **speed of sound propagation** for radio packets based on the **physical distance** between sender and receiver. Packets travel at **343 m/s** (speed of sound in air), creating realistic delays without any range limits.

## How It Works

### 1. **Position Tracking**

The PacketHandler automatically tracks the Transform positions of:
- All registered PacketReceivers (drones)
- The PacketHandler itself (radio tower/ground station)
- AutoSwarm (registered manually)

### 2. **Distance Calculation**

When a packet is broadcast:
```csharp
Vector3 senderPosition = GetSenderPosition(packet.sender);
Vector3 receiverPosition = receiver.transform.position;
float distance = Vector3.Distance(senderPosition, receiverPosition);
```

### 3. **Delay Calculation - Speed of Sound**

```csharp
float delay = distance / propagationSpeed;  // propagationSpeed = 343 m/s
```

**Examples:**
- **343m distance → 1.0 second delay**
- **171.5m distance → 0.5 second delay**
- **34.3m distance → 0.1 second (100ms) delay**
- **10m distance → ~29ms delay**
- **100m distance → ~291ms delay**

### 4. **No Range Limits**

All packets are always delivered regardless of distance - only the timing changes based on how far they need to travel.

---

## Inspector Settings

### **Broadcast Settings**
- `logAllPackets` - Debug log all packet broadcasts and deliveries
- `signalDelay` - Base delay added to all packets (seconds)

### **Distance-Based Delay**
- `useDistanceBasedDelay` - Enable/disable distance-based delays
  - **true**: Uses speed of sound (343 m/s)
  - **false**: Uses only `signalDelay` (uniform delay)
- `propagationSpeed` - Speed of signal propagation (meters/second)
  - Default: `343` (speed of sound in air at 20°C)
  - Speed of light: `299792458` (for RF/radio simulation)
  - Custom: Any value you want for gameplay tuning
- `visualizeTransmissions` - Draw debug lines showing packet transmissions

### **Statistics**
- `totalPacketsSent` - Total packets broadcast
- `totalPacketsDelivered` - Total packets successfully delivered

---

## Configuration Examples

### **Speed of Sound (Default)**
```
useDistanceBasedDelay = true
propagationSpeed = 343        // m/s (speed of sound)
signalDelay = 0.0
```
Result: 343m takes 1 second

### **Speed of Light (Realistic Radio)**
```
useDistanceBasedDelay = true
propagationSpeed = 299792458  // m/s (speed of light)
signalDelay = 0.0
```
Result: Nearly instant, ~3.3ns per meter

### **Exaggerated (For Gameplay)**
```
useDistanceBasedDelay = true
propagationSpeed = 50         // m/s (very slow)
signalDelay = 0.0
```
Result: 50m takes 1 second (very noticeable delays)

### **Disabled (Instant)**
```
useDistanceBasedDelay = false
signalDelay = 0.0
```
Result: All packets arrive instantly

---

## Scene Setup

### **Radio Tower Position**

The PacketHandler's Transform position acts as the **ground station/radio tower**. Position it strategically:

```
Swarm Controller (GameObject)
├─ Transform: Position (0, 10, 0)  ← Radio tower height
├─ AutoSwarm (Component)
└─ PacketHandler (Component)       ← Uses this Transform position
```

When AutoSwarm sends packets, they originate from this position.

### **Drone Positions**

Each drone's PacketReceiver automatically uses its GameObject's Transform:

```
BlueDrone/BlueInvisDrone
├─ Transform: Position (runtime - flying position)
└─ PacketReceiver (Component)  ← Uses this Transform position
```

---

## Packet Flow Example

### Scenario: AutoSwarm sends target to drones at various distances

```
AutoSwarm broadcasts "set_target" packet
Sender: "AutoSwarm" at (0, 10, 0)
   
PacketHandler calculates distances and delays:

├─ Receiver "drone1" at (50, 5, 30)
│   Distance: 58.3m
│   Delay: 58.3 / 343 = 0.170s (170ms)
│   Delivery Time: Time.time + 0.170s
│   
├─ Receiver "drone2" at (150, 8, 100)
│   Distance: 180.3m
│   Delay: 180.3 / 343 = 0.526s (526ms)
│   Delivery Time: Time.time + 0.526s
│   
└─ Receiver "drone3" at (300, 5, 200)
    Distance: 360.6m
    Delay: 360.6 / 343 = 1.051s (1051ms)
    Delivery Time: Time.time + 1.051s

Result: 
- drone1 receives command in 170ms
- drone2 receives command in 526ms (356ms after drone1)
- drone3 receives command in 1051ms (881ms after drone1)
```

---

## Real-World Behavior Examples

#### **Example 1: Formation Leader Broadcasting**

```
Leader at (100, 5, 50) broadcasts position every 50ms (20Hz)
PacketHandler (radio tower) at (0, 10, 0)

Receiver Propagation Times:
├─ drone1 (Left Wing) at (103, 5, 48)
│   Distance: 3.6m → Delay: 10.5ms
│   Receives ~10ms after broadcast
│   
├─ drone2 (Right Wing) at (97, 5, 48)
│   Distance: 3.6m → Delay: 10.5ms
│   Receives ~10ms after broadcast
│   
└─ drone3 (Far Observer) at (500, 5, 300)
    Distance: 538m → Delay: 1568ms (1.57 seconds!)
    Receives 1.57 seconds after broadcast
    Uses VERY stale leader position data

Result: 
- Nearby wingmen get fresh data
- Distant observers see delayed/stale data
- All drones eventually receive all packets
```

#### **Example 2: Swarm Coordination Across Large Area**

```
AutoSwarm at (0, 10, 0) sends "formation_active = true" packet

Drones spread across terrain:
├─ drone1 at (20, 5, 15)     → 25m  → 73ms delay
├─ drone2 at (100, 8, 80)    → 128m → 373ms delay  
└─ drone3 at (200, 5, 150)   → 250m → 729ms delay

Result: Formation activates in a wave:
- drone1 activates at t=0.073s
- drone2 activates at t=0.373s (300ms later)
- drone3 activates at t=0.729s (656ms later)
```

---

## Debug Features

### **Console Logging**

Enable `logAllPackets = true` to see:
```
[PacketHandler] Broadcasting: Packet from AutoSwarm
[PacketHandler] Scheduled delivery: AutoSwarm -> drone1 (distance: 58.3m, delay: 170.00ms)
[PacketHandler] Scheduled delivery: AutoSwarm -> drone2 (distance: 180.5m, delay: 526.24ms)
[PacketHandler] Scheduled delivery: AutoSwarm -> drone3 (distance: 360.6m, delay: 1051.02ms)
```

### **Visual Debugging**

Enable `visualizeTransmissions = true` to see:
- **Cyan lines** drawn from sender to receiver in Scene view
- Lines persist for the duration of the transmission delay
- Longer lines = longer delays = farther distances

### **Statistics Panel**

Monitor in Inspector:
```
Total Packets Sent: 127
Total Packets Delivered: 127  (always equal - no packet loss)
```

---

## Why 343 m/s?

**343 m/s** is the speed of sound in air at 20°C (68°F) at sea level. This creates interesting propagation delays:

| Distance | Delay | Real-World Example |
|----------|-------|-------------------|
| 3.4m | 10ms | Drone in close formation |
| 34.3m | 100ms | Nearby squad member |
| 171.5m | 500ms | Half-second delay |
| 343m | 1.0s | One full second |
| 686m | 2.0s | Significant coordination lag |

This is much slower than radio (speed of light) but creates **noticeable, gameplay-relevant delays** that test your formation algorithms' ability to handle latency.

---

## Impact on Formation Keeping

The system creates **realistic communication lag** that your formation algorithms must handle:

### **Leader Broadcasting Position**

```
t = 0.000s: Leader at (100, 5, 50) broadcasts position
t = 0.010s: Wingman 1 (3m away) receives → uses fresh data
t = 0.029s: Wingman 2 (10m away) receives → uses 29ms old data
t = 0.146s: Wingman 3 (50m away) receives → uses 146ms old data

If leader is moving at 10 m/s:
- Wingman 1 sees leader position with 0.03m error
- Wingman 2 sees leader position with 0.29m error  
- Wingman 3 sees leader position with 1.46m error!
```

Wingmen must either:
1. Accept position error and correct reactively
2. Predict leader movement from velocity data
3. Increase broadcast rate to minimize staleness

---

## Testing Scenarios

### **Test 1: Watch Staggered Command Reception**
1. Enable `logAllPackets = true`
2. Spread drones at 100m, 200m, 300m from ground station
3. Send a command from AutoSwarm
4. Watch console show deliveries ~291ms apart

### **Test 2: Formation with Latency**
1. Keep default `propagationSpeed = 343`
2. Leader broadcasts at 20Hz
3. Place wingmen at different distances
4. Farther wingmen will lag slightly behind

### **Test 3: Exaggerate for Testing**
1. Set `propagationSpeed = 50` (very slow)
2. Fly drones 100m+ apart
3. Watch commands take 2+ seconds to arrive
4. Test algorithm resilience to high latency

---

## Performance Notes

### **No Range Limits**

Unlike radio systems with limited range, all packets eventually arrive:
- No packet dropping logic
- No distance checks
- Every receiver always gets every packet (just delayed)

### **Per-Receiver Scheduling**

Each receiver gets its own delivery time:

```csharp
foreach (PacketReceiver receiver in receivers)
{
    float distance = Vector3.Distance(senderPosition, receiverPosition);
    float delay = distance / propagationSpeed;
    
    DelayedPacketDelivery delivery = new DelayedPacketDelivery(
        packet, receiver, Time.time + delay, distance
    );
    delayedPackets.Add(delivery);
}
```

Result: 3 drones = 3 scheduled deliveries per broadcast

---

## Advanced Customization

### **Change Propagation Speed**

Adjust `propagationSpeed` in Inspector for different effects:

```
propagationSpeed = 343        // Speed of sound (default)
propagationSpeed = 299792458  // Speed of light (radio waves)
propagationSpeed = 100        // Slow (3.43x slower than sound)
propagationSpeed = 1000       // Fast (2.9x faster than sound)
```

### **Add Base Processing Delay**

Use `signalDelay` to simulate radio hardware processing:

```
signalDelay = 0.01           // 10ms hardware processing
propagationSpeed = 343        // + speed of sound propagation

Total delay = 0.01 + (distance / 343)
```

Example: 100m distance = 10ms + 291ms = 301ms total

---

## Summary

The distance-based packet delay system now simulates **speed of sound propagation**:

✅ **Realistic** - 343 m/s creates noticeable but reasonable delays  
✅ **No Packet Loss** - All packets always arrive (no range limits)  
✅ **Position-Aware** - Uses actual 3D world distances  
✅ **Visualizable** - Debug lines show transmissions in Scene view  
✅ **Configurable** - Adjust propagation speed for different effects  
✅ **Performant** - Efficient delayed delivery scheduling  

Perfect for testing how your formation algorithms handle communication latency without the complexity of packet loss!

