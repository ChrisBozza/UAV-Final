using UnityEngine;

[System.Serializable]
public class Packet
{
    public string packetId;
    public long sequenceNumber;
    public float timestamp;
    public string sender;
    public string recipient;
    public string messageType;
    public string data;
    public bool requiresAck;
    public string ackForPacketId;

    public Packet(string sender, string recipient, string messageType, string data = "", bool requiresAck = true)
    {
        this.packetId = System.Guid.NewGuid().ToString();
        this.sequenceNumber = -1;
        this.timestamp = Time.time;
        this.sender = sender;
        this.recipient = recipient;
        this.messageType = messageType;
        this.data = data;
        this.requiresAck = requiresAck;
        this.ackForPacketId = null;
    }

    public static Packet CreateAck(string sender, string recipient, string originalPacketId, long sequenceNumber)
    {
        Packet ack = new Packet(sender, recipient, "ack", "", false);
        ack.ackForPacketId = originalPacketId;
        ack.sequenceNumber = sequenceNumber;
        return ack;
    }

    public bool IsAck()
    {
        return messageType == "ack" && !string.IsNullOrEmpty(ackForPacketId);
    }

    public bool IsForRecipient(string recipientId)
    {
        return recipient == recipientId || recipient == "broadcast" || recipient == "all";
    }

    public override string ToString()
    {
        if (IsAck())
        {
            return $"ACK[{packetId}] from:{sender} to:{recipient} ackFor:{ackForPacketId} seq:{sequenceNumber} @{timestamp:F2}s";
        }
        return $"Packet[{packetId}] from:{sender} to:{recipient} type:{messageType} seq:{sequenceNumber} @{timestamp:F2}s";
    }
}
