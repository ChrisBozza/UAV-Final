using UnityEngine;

[System.Serializable]
public class Packet
{
    public string packetId;
    public float timestamp;
    public string sender;
    public string recipient;
    public string messageType;
    public string data;

    public Packet(string sender, string recipient, string messageType, string data = "")
    {
        this.packetId = System.Guid.NewGuid().ToString();
        this.timestamp = Time.time;
        this.sender = sender;
        this.recipient = recipient;
        this.messageType = messageType;
        this.data = data;
    }

    public bool IsForRecipient(string recipientId)
    {
        return recipient == recipientId || recipient == "broadcast" || recipient == "all";
    }

    public override string ToString()
    {
        return $"Packet[{packetId}] from:{sender} to:{recipient} type:{messageType} @{timestamp:F2}s";
    }
}
