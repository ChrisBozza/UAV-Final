using UnityEngine;
using System.Collections.Generic;

public class PacketAcknowledgment
{
    public Packet packet;
    public List<string> pendingReceivers;
    public float sentTime;
    public int retryCount;
    public float nextRetryTime;

    private const float ACK_TIMEOUT = 2.0f;
    private const int MAX_RETRIES = 3;

    public PacketAcknowledgment(Packet packet, List<string> receivers)
    {
        this.packet = packet;
        this.pendingReceivers = new List<string>(receivers);
        this.sentTime = Time.time;
        this.retryCount = 0;
        this.nextRetryTime = Time.time + ACK_TIMEOUT;
    }

    public bool IsAcknowledged()
    {
        return pendingReceivers.Count == 0;
    }

    public bool ShouldRetry()
    {
        return Time.time >= nextRetryTime && retryCount < MAX_RETRIES;
    }

    public void MarkAcknowledged(string receiverId)
    {
        pendingReceivers.Remove(receiverId);
    }

    public void IncrementRetry()
    {
        retryCount++;
        nextRetryTime = Time.time + ACK_TIMEOUT;
    }

    public bool HasExpired()
    {
        return retryCount >= MAX_RETRIES;
    }

    public float GetAge()
    {
        return Time.time - sentTime;
    }
}
