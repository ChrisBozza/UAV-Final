# Packet Acknowledgment (SYN-ACK) System

## Overview

The packet system now implements a reliable transmission protocol with acknowledgments (ACKs) and automatic retransmission for dropped packets. This system is designed to work with the upcoming DDoS simulation feature.

## Key Features

### 1. Packet Sequencing
- Every packet is assigned a unique **sequence number** (incrementing from 0)
- Receivers track processed sequence numbers to detect and ignore duplicates
- Sequence numbers help identify packet order when some are dropped

### 2. Acknowledgment System
- Receivers automatically send ACK packets when they receive a message
- `PacketHandler` tracks which receivers have acknowledged each packet
- ACKs include the original packet ID and sequence number

### 3. Automatic Retransmission
- If a receiver doesn't ACK within **2 seconds**, the packet is retransmitted
- Maximum of **3 retries** before a packet is considered dropped
- Retransmissions only go to receivers who haven't ACKed yet

### 4. DDoS Simulation (Packet Dropping)
- `PacketReceiver` has configurable packet drop rate (0-100%)
- When enabled, packets are randomly dropped before processing
- Dropped packets trigger automatic retransmission from the sender

## Usage

### Basic Sending (with ACK)
```csharp
public void SendPacket(string recipient, string messageType, string data = "")
{
    Packet packet = new Packet(receiverId, recipient, messageType, data);
    PacketHandler.Instance.BroadcastPacket(packet);
}
```

### Sending Without ACK Requirement
```csharp
Packet packet = new Packet(senderId, recipient, messageType, data, requiresAck: false);
PacketHandler.Instance.BroadcastPacket(packet);
```

### Enable DDoS Simulation on a Receiver
In the Inspector on a `PacketReceiver` component:
- Check **Enable DDoS Simulation**
- Set **Packet Drop Rate** (e.g., 0.3 = 30% packet loss)

## Configuration

### PacketHandler Settings

**ACK System:**
- `enableAckSystem` - Enable/disable the entire ACK system
- `logAcknowledgments` - Log ACK sends and receives
- `ackTimeout` - Time to wait before retransmission (default: 2s)
- `maxRetries` - Maximum retransmission attempts (default: 3)

**Statistics:**
- `totalPacketsSent` - Total packets broadcast
- `totalPacketsDelivered` - Total packets delivered to receivers
- `totalAcksReceived` - Total ACKs received
- `totalRetransmissions` - Total packet retransmissions
- `totalPacketsDropped` - Packets that failed after max retries

### PacketReceiver Settings

**DDoS Simulation:**
- `enableDDoSSimulation` - Enable packet dropping
- `packetDropRate` - Probability of dropping a packet (0.0 to 1.0)

## Packet Flow

### Normal Flow (No Packet Loss)
```
1. Sender creates Packet (seq: 100)
2. PacketHandler broadcasts to all receivers
3. PacketHandler tracks pending ACKs
4. Receiver gets packet, processes it
5. Receiver sends ACK back
6. PacketHandler receives ACK, marks packet as acknowledged
```

### Flow With Packet Loss
```
1. Sender creates Packet (seq: 100)
2. PacketHandler broadcasts to Receiver A and B
3. Receiver A drops packet (DDoS simulation)
4. Receiver B processes packet and sends ACK
5. After 2 seconds, PacketHandler retransmits to Receiver A only
6. Receiver A gets retransmitted packet, sends ACK
7. PacketHandler receives both ACKs, packet complete
```

### Flow With Complete Failure
```
1. Sender creates Packet (seq: 100)
2. PacketHandler broadcasts to Receiver A
3. Receiver A drops packet (retry 1)
4. After 2s, retransmit (retry 2)
5. Receiver A drops again (retry 3)
6. After 2s, retransmit (retry 3, final)
7. Receiver A drops again
8. Packet marked as DROPPED, statistics updated
```

## Debugging

### Using PacketHandlerDebugger

Add the `PacketHandlerDebugger` component to any GameObject to see real-time information:

- **Statistics** - Packet counts and delivery rates
- **Pending ACKs** - Which packets are waiting for acknowledgment
- **Retry Information** - How many times packets have been retransmitted

### Logging Options

**PacketHandler:**
- `logAllPackets` - Log every packet broadcast
- `logAcknowledgments` - Log ACK sends/receives

**PacketReceiver:**
- `logReceivedPackets` - Log packets received
- `logAcknowledgments` - Log ACK sends

## Architecture

### Classes

**Packet.cs**
- Contains packet data and metadata
- `sequenceNumber` - Order tracking
- `requiresAck` - Whether this packet needs acknowledgment
- `ackForPacketId` - If this is an ACK, which packet it's for

**PacketAcknowledgment.cs**
- Tracks acknowledgment state for a single packet
- Manages retry timing and counts
- Stores list of receivers who haven't ACKed yet

**PacketHandler.cs**
- Broadcasts packets with sequence numbers
- Tracks pending acknowledgments
- Handles automatic retransmission
- Manages statistics

**PacketReceiver.cs**
- Receives packets and sends ACKs
- Detects duplicate packets via sequence numbers
- Simulates DDoS packet dropping
- Processes packet queue

**PacketHandlerDebugger.cs**
- Visual debugging tool for the ACK system
- Shows real-time statistics and pending ACKs

## Example: Testing Reliability

```csharp
// On a test script
void Start()
{
    // Configure receiver to drop 50% of packets
    PacketReceiver receiver = GetComponent<PacketReceiver>();
    receiver.enableDDoSSimulation = true;
    receiver.packetDropRate = 0.5f;
    
    // Enable logging
    PacketHandler.Instance.logAcknowledgments = true;
    receiver.logReceivedPackets = true;
}

void SendTestPacket()
{
    // This packet will be retransmitted if dropped
    Packet packet = new Packet("TestSender", "drone1", "set_target", "100,50,200");
    PacketHandler.Instance.BroadcastPacket(packet);
}
```

## Performance Considerations

- Sequence numbers use `long` type (supports 9 quintillion packets)
- Processed sequence numbers stored in `HashSet` for O(1) duplicate detection
- ACK packets do not require acknowledgment (prevents infinite loops)
- Pending ACKs are cleaned up after max retries to prevent memory leaks

## Future Enhancements

- Configurable timeout per packet type
- Selective acknowledgment (SACK) for batch operations
- Flow control mechanisms
- Bandwidth throttling during DDoS
- Network congestion detection
