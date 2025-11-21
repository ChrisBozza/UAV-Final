using UnityEngine;
using System.Collections.Generic;

public class PacketHandler : MonoBehaviour
{
    public static PacketHandler Instance { get; private set; }

    [Header("Broadcast Settings")]
    public bool logAllPackets = false;
    public float signalDelay = 0f;

    [Header("Statistics")]
    public int totalPacketsSent = 0;
    public int totalPacketsDelivered = 0;

    private List<PacketReceiver> receivers = new List<PacketReceiver>();
    private Queue<DelayedPacket> delayedPackets = new Queue<DelayedPacket>();

    private class DelayedPacket
    {
        public Packet packet;
        public float deliveryTime;

        public DelayedPacket(Packet packet, float deliveryTime)
        {
            this.packet = packet;
            this.deliveryTime = deliveryTime;
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
            Debug.Log($"[PacketHandler] Registered receiver: {receiver.receiverId}");
        }
    }

    public void UnregisterReceiver(PacketReceiver receiver)
    {
        if (receivers.Contains(receiver))
        {
            receivers.Remove(receiver);
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

        if (signalDelay > 0f)
        {
            DelayedPacket delayed = new DelayedPacket(packet, Time.time + signalDelay);
            delayedPackets.Enqueue(delayed);
        }
        else
        {
            DeliverPacketToReceivers(packet);
        }
    }

    private void ProcessDelayedPackets()
    {
        while (delayedPackets.Count > 0)
        {
            DelayedPacket delayed = delayedPackets.Peek();

            if (Time.time >= delayed.deliveryTime)
            {
                delayedPackets.Dequeue();
                DeliverPacketToReceivers(delayed.packet);
            }
            else
            {
                break;
            }
        }
    }

    private void DeliverPacketToReceivers(Packet packet)
    {
        foreach (PacketReceiver receiver in receivers)
        {
            if (receiver != null)
            {
                receiver.ReceivePacket(packet);
                totalPacketsDelivered++;
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
}
