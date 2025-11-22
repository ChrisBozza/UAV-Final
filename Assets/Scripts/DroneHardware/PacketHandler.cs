using UnityEngine;
using System.Collections.Generic;

public class PacketHandler : MonoBehaviour
{
    public static PacketHandler Instance { get; private set; }

    [Header("Broadcast Settings")]
    public bool logAllPackets = false;
    public float signalDelay = 0f;

    [Header("Distance-Based Delay")]
    public bool useDistanceBasedDelay = true;
    public float propagationSpeed = 343f;
    public bool visualizeTransmissions = false;

    [Header("Statistics")]
    public int totalPacketsSent = 0;
    public int totalPacketsDelivered = 0;

    private List<PacketReceiver> receivers = new List<PacketReceiver>();
    private List<DelayedPacketDelivery> delayedPackets = new List<DelayedPacketDelivery>();
    private Dictionary<string, Transform> senderTransforms = new Dictionary<string, Transform>();

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

        totalPacketsSent++;

        if (logAllPackets)
        {
            Debug.Log($"[PacketHandler] Broadcasting: {packet}");
        }

        Vector3 senderPosition = GetSenderPosition(packet.sender);

        foreach (PacketReceiver receiver in receivers)
        {
            if (receiver == null) continue;

            Vector3 receiverPosition = receiver.transform.position;
            float distance = Vector3.Distance(senderPosition, receiverPosition);

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

            if (logAllPackets)
            {
                Debug.Log($"[PacketHandler] Scheduled delivery: {packet.sender} -> {receiver.receiverId} (distance: {distance:F1}m, delay: {delay * 1000f:F2}ms)");
            }
        }
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
                if (delivery.receiver != null)
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
}
