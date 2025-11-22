using UnityEngine;
using System.Collections.Generic;

public class PacketAcknowledgment
{
    public Packet packet;
    public List<string> pendingReceivers;
    public float sentTime;
    public int retryCount;
    public float nextRetryTime;
    public float ackTimeout;
    public int maxRetries;

    public PacketAcknowledgment(Packet packet, List<string> receivers, float ackTimeout, int maxRetries)
    {
        this.packet = packet;
        this.pendingReceivers = new List<string>(receivers);
        this.sentTime = Time.time;
        this.retryCount = 0;
        this.ackTimeout = ackTimeout;
        this.maxRetries = maxRetries;
        this.nextRetryTime = Time.time + ackTimeout;
    }

    public bool IsAcknowledged()
    {
        return pendingReceivers.Count == 0;
    }

    public bool ShouldRetry()
    {
        return Time.time >= nextRetryTime && retryCount < maxRetries;
    }

    public void MarkAcknowledged(string receiverId)
    {
        pendingReceivers.Remove(receiverId);
    }

    public void IncrementRetry()
    {
        retryCount++;
        nextRetryTime = Time.time + ackTimeout;
    }

    public bool HasExpired()
    {
        return retryCount >= maxRetries;
    }

    public float GetAge()
    {
        return Time.time - sentTime;
    }
}
