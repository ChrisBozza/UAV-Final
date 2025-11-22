using UnityEngine;
using System.Collections.Generic;

public class PacketHandler : MonoBehaviour
{
    public static PacketHandler Instance { get; private set; }

    public delegate void PacketBroadcastHandler(Packet packet);
    public event PacketBroadcastHandler OnPacketBroadcast;

    [Header("Broadcast Settings")]
    public bool logAllPackets = false;
    public bool logEventSubscribers = false;
    public float signalDelay = 0f;

    [Header("Distance-Based Delay")]
    public bool useDistanceBasedDelay = true;
    public float propagationSpeed = 343f;
    public bool visualizeTransmissions = false;

    [Header("ACK System")]
    public bool enableAckSystem = true;
    public bool logAcknowledgments = false;
    public float ackTimeout = 10.0f;
    public int maxRetries = 3;
    public bool useDynamicTimeout = true;

    [Header("Statistics")]
    public int totalPacketsSent = 0;
    public int totalPacketsDelivered = 0;
    public int totalAcksReceived = 0;
    public int totalPacketsDropped = 0;
    public int totalRetransmissions = 0;

    private List<PacketReceiver> receivers = new List<PacketReceiver>();
    private List<DelayedPacketDelivery> delayedPackets = new List<DelayedPacketDelivery>();
    private Dictionary<string, Transform> senderTransforms = new Dictionary<string, Transform>();
    private Dictionary<string, PacketAcknowledgment> pendingAcks = new Dictionary<string, PacketAcknowledgment>();
    private long nextSequenceNumber = 0;

    private class DelayedPacketDelivery
    {
        public Packet packet;
        public PacketReceiver receiver;
        public float deliveryTime;
        public float distance;

        public DelayedPacketDelivery(Packet packet, PacketReceiver receiver, float deliveryTime, float distance)
        {
            this.packet = packet;
            this.receiver = receiver;
            this.deliveryTime = deliveryTime;
            this.distance = distance;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PacketHandlers detected! Destroying duplicate.");
            Destroy(this);
            return;
        }

        Instance = this;
        senderTransforms["PacketHandler"] = transform;
    }

    void Update()
    {
        ProcessDelayedPackets();
        ProcessPendingAcknowledgments();
    }

    public void RegisterReceiver(PacketReceiver receiver)
    {
        if (!receivers.Contains(receiver))
        {
            receivers.Add(receiver);
            
            string senderId = receiver.receiverId;
            if (!senderTransforms.ContainsKey(senderId))
            {
                senderTransforms[senderId] = receiver.transform;
            }
            
            Debug.Log($"[PacketHandler] Registered receiver: {receiver.receiverId} at position {receiver.transform.position}");
        }
    }

    public void UnregisterReceiver(PacketReceiver receiver)
    {
        if (receivers.Contains(receiver))
        {
            receivers.Remove(receiver);
            
            if (senderTransforms.ContainsKey(receiver.receiverId))
            {
                senderTransforms.Remove(receiver.receiverId);
            }
            
            Debug.Log($"[PacketHandler] Unregistered receiver: {receiver.receiverId}");
        }
    }

    public void BroadcastPacket(Packet packet)
    {
        if (packet == null)
        {
            Debug.LogError("[PacketHandler] Cannot broadcast null packet!");
            return;
        }

        if (packet.sequenceNumber == -1)
        {
            packet.sequenceNumber = nextSequenceNumber++;
        }

        totalPacketsSent++;

        if (logEventSubscribers && OnPacketBroadcast != null)
        {
            int subscriberCount = OnPacketBroadcast.GetInvocationList().Length;
            Debug.Log($"[PacketHandler] Broadcasting to {subscriberCount} subscriber(s)");
        }

        OnPacketBroadcast?.Invoke(packet);

        if (logAllPackets)
        {
            Debug.Log($"[PacketHandler] Broadcasting: {packet}");
        }

        if (packet.IsAck())
        {
            float delay = CalculateTransmissionDelay(0f);
            DelayedPacketDelivery delivery = new DelayedPacketDelivery(
                packet,
                null,
                Time.time + delay,
                0f
            );
            delayedPackets.Add(delivery);

            if (logAcknowledgments)
            {
                Debug.Log($"[PacketHandler] Scheduled ACK delivery from {packet.sender} for packet {packet.ackForPacketId}");
            }
            return;
        }

        Vector3 senderPosition = GetSenderPosition(packet.sender);
        List<string> receiversExpectingAck = new List<string>();
        float maxDistance = 0f;

        foreach (PacketReceiver receiver in receivers)
        {
            if (receiver == null) continue;

            if (packet.IsForRecipient(receiver.receiverId))
            {
                Vector3 receiverPosition = receiver.transform.position;
                float distance = Vector3.Distance(senderPosition, receiverPosition);
                
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }

                float delay = CalculateTransmissionDelay(distance);

                if (visualizeTransmissions)
                {
                    Debug.DrawLine(senderPosition, receiverPosition, Color.cyan, delay);
                }

                DelayedPacketDelivery delivery = new DelayedPacketDelivery(
                    packet,
                    receiver,
                    Time.time + delay,
                    distance
                );

                delayedPackets.Add(delivery);

                if (enableAckSystem && packet.requiresAck && !packet.IsAck())
                {
                    receiversExpectingAck.Add(receiver.receiverId);
                }

                if (logAllPackets)
                {
                    Debug.Log($"[PacketHandler] Scheduled delivery: {packet.sender} -> {receiver.receiverId} (distance: {distance:F1}m, delay: {delay * 1000f:F2}ms)");
                }
            }
        }

        if (enableAckSystem && packet.requiresAck && !packet.IsAck() && receiversExpectingAck.Count > 0)
        {
            float calculatedTimeout = CalculateAckTimeout(maxDistance);
            PacketAcknowledgment ackTracker = new PacketAcknowledgment(packet, receiversExpectingAck, calculatedTimeout, maxRetries);
            pendingAcks[packet.packetId] = ackTracker;

            if (logAcknowledgments)
            {
                Debug.Log($"[PacketHandler] Waiting for ACKs from {receiversExpectingAck.Count} receivers for packet {packet.packetId} (timeout: {calculatedTimeout:F2}s, max dist: {maxDistance:F1}m)");
            }
        }
    }

    private float CalculateAckTimeout(float maxDistance)
    {
        if (!useDynamicTimeout || !useDistanceBasedDelay)
        {
            return ackTimeout;
        }

        float roundTripDelay = (maxDistance / propagationSpeed) * 2f;
        float safetyMargin = 2f;
        float calculatedTimeout = roundTripDelay + safetyMargin + signalDelay * 2f;

        return Mathf.Max(ackTimeout, calculatedTimeout);
    }

    private float CalculateTransmissionDelay(float distance)
    {
        if (!useDistanceBasedDelay)
        {
            return signalDelay;
        }

        float distanceDelay = distance / propagationSpeed;
        float totalDelay = signalDelay + distanceDelay;

        return totalDelay;
    }

    private Vector3 GetSenderPosition(string senderId)
    {
        if (senderTransforms.ContainsKey(senderId))
        {
            return senderTransforms[senderId].position;
        }

        if (senderId == "AutoSwarm" || senderId == "PacketHandler")
        {
            return transform.position;
        }

        Debug.LogWarning($"[PacketHandler] Unknown sender: {senderId}, using PacketHandler position");
        return transform.position;
    }

    private void ProcessDelayedPackets()
    {
        for (int i = delayedPackets.Count - 1; i >= 0; i--)
        {
            DelayedPacketDelivery delivery = delayedPackets[i];

            if (Time.time >= delivery.deliveryTime)
            {
                if (delivery.packet.IsAck())
                {
                    ReceiveAcknowledgment(delivery.packet.ackForPacketId, delivery.packet.sender);
                    totalPacketsDelivered++;
                }
                else if (delivery.receiver != null)
                {
                    delivery.receiver.ReceivePacket(delivery.packet);
                    totalPacketsDelivered++;
                }

                delayedPackets.RemoveAt(i);
            }
        }
    }

    public int GetReceiverCount()
    {
        return receivers.Count;
    }

    public void ClearStatistics()
    {
        totalPacketsSent = 0;
        totalPacketsDelivered = 0;
        totalAcksReceived = 0;
        totalPacketsDropped = 0;
        totalRetransmissions = 0;
    }

    public void RegisterSender(string senderId, Transform senderTransform)
    {
        if (!senderTransforms.ContainsKey(senderId))
        {
            senderTransforms[senderId] = senderTransform;
            Debug.Log($"[PacketHandler] Registered sender: {senderId} at position {senderTransform.position}");
        }
    }

    public void UnregisterSender(string senderId)
    {
        if (senderTransforms.ContainsKey(senderId))
        {
            senderTransforms.Remove(senderId);
            Debug.Log($"[PacketHandler] Unregistered sender: {senderId}");
        }
    }

    public void ReceiveAcknowledgment(string packetId, string receiverId)
    {
        if (pendingAcks.ContainsKey(packetId))
        {
            PacketAcknowledgment ackTracker = pendingAcks[packetId];
            ackTracker.MarkAcknowledged(receiverId);
            totalAcksReceived++;

            if (logAcknowledgments)
            {
                Debug.Log($"[PacketHandler] ACK received from {receiverId} for packet {packetId}. Remaining: {ackTracker.pendingReceivers.Count}");
            }

            if (ackTracker.IsAcknowledged())
            {
                pendingAcks.Remove(packetId);

                if (logAcknowledgments)
                {
                    Debug.Log($"[PacketHandler] All ACKs received for packet {packetId}. Age: {ackTracker.GetAge():F3}s");
                }
            }
        }
    }

    private void ProcessPendingAcknowledgments()
    {
        List<string> toRemove = new List<string>();

        foreach (var kvp in pendingAcks)
        {
            PacketAcknowledgment ackTracker = kvp.Value;

            if (ackTracker.ShouldRetry())
            {
                ackTracker.IncrementRetry();
                totalRetransmissions++;

                if (logAcknowledgments)
                {
                    Debug.LogWarning($"[PacketHandler] Retransmitting packet {kvp.Key} (attempt {ackTracker.retryCount}/{maxRetries}). Missing ACKs from: {string.Join(", ", ackTracker.pendingReceivers)}");
                }

                Packet retransmitPacket = ackTracker.packet;
                Vector3 senderPosition = GetSenderPosition(retransmitPacket.sender);

                foreach (string receiverId in ackTracker.pendingReceivers)
                {
                    PacketReceiver receiver = receivers.Find(r => r != null && r.receiverId == receiverId);
                    if (receiver != null)
                    {
                        Vector3 receiverPosition = receiver.transform.position;
                        float distance = Vector3.Distance(senderPosition, receiverPosition);
                        float delay = CalculateTransmissionDelay(distance);

                        DelayedPacketDelivery delivery = new DelayedPacketDelivery(
                            retransmitPacket,
                            receiver,
                            Time.time + delay,
                            distance
                        );

                        delayedPackets.Add(delivery);
                    }
                }
            }

            if (ackTracker.HasExpired())
            {
                totalPacketsDropped++;
                toRemove.Add(kvp.Key);

                Debug.LogError($"[PacketHandler] Packet {kvp.Key} dropped after {maxRetries} retries. No ACK from: {string.Join(", ", ackTracker.pendingReceivers)}");
            }
        }

        foreach (string packetId in toRemove)
        {
            pendingAcks.Remove(packetId);
        }
    }

    public int GetPendingAckCount()
    {
        return pendingAcks.Count;
    }

    public Dictionary<string, PacketAcknowledgment> GetPendingAcks()
    {
        return new Dictionary<string, PacketAcknowledgment>(pendingAcks);
    }
}
