using UnityEngine;
using System.Collections.Generic;

public class PacketReceiver : MonoBehaviour
{
    [Header("Receiver Identity")]
    public string receiverId;

    [Header("Packet Processing")]
    public bool logReceivedPackets = false;
    public bool logIgnoredPackets = false;

    private DroneComputer droneComputer;
    private FormationKeeper formationKeeper;
    private Queue<Packet> packetQueue = new Queue<Packet>();

    void Awake()
    {
        droneComputer = GetComponent<DroneComputer>();
        formationKeeper = GetComponent<FormationKeeper>();

        if (string.IsNullOrEmpty(receiverId))
        {
            receiverId = gameObject.name;
        }

        PacketHandler.Instance?.RegisterReceiver(this);
    }

    void OnDestroy()
    {
        PacketHandler.Instance?.UnregisterReceiver(this);
    }

    void Update()
    {
        ProcessPacketQueue();
    }

    public void ReceivePacket(Packet packet)
    {
        if (packet.IsForRecipient(receiverId))
        {
            if (logReceivedPackets)
            {
                Debug.Log($"[{receiverId}] Received: {packet}");
            }

            packetQueue.Enqueue(packet);
        }
        else if (logIgnoredPackets)
        {
            Debug.Log($"[{receiverId}] Ignored packet for: {packet.recipient}");
        }
    }

    private void ProcessPacketQueue()
    {
        while (packetQueue.Count > 0)
        {
            Packet packet = packetQueue.Dequeue();
            ProcessPacket(packet);
        }
    }

    private void ProcessPacket(Packet packet)
    {
        switch (packet.messageType)
        {
            case "formation_update":
                HandleFormationUpdate(packet);
                break;

            case "checkpoint_reached":
                HandleCheckpointReached(packet);
                break;

            case "slowdown_request":
                HandleSlowdownRequest(packet);
                break;

            case "position_report":
                HandlePositionReport(packet);
                break;

            case "status_request":
                HandleStatusRequest(packet);
                break;

            default:
                Debug.LogWarning($"[{receiverId}] Unknown packet type: {packet.messageType}");
                break;
        }
    }

    private void HandleFormationUpdate(Packet packet)
    {
        if (formationKeeper != null)
        {
            Debug.Log($"[{receiverId}] Formation update received from {packet.sender}");
        }
    }

    private void HandleCheckpointReached(Packet packet)
    {
        if (formationKeeper != null)
        {
            formationKeeper.OnCheckpointReached();
        }
    }

    private void HandleSlowdownRequest(Packet packet)
    {
        if (formationKeeper != null)
        {
            Debug.Log($"[{receiverId}] Slowdown request from {packet.sender}");
        }
    }

    private void HandlePositionReport(Packet packet)
    {
        Debug.Log($"[{receiverId}] Position report from {packet.sender}: {packet.data}");
    }

    private void HandleStatusRequest(Packet packet)
    {
        if (droneComputer != null)
        {
            Vector3 position = droneComputer.GetPosition();
            Vector3 velocity = droneComputer.GetVelocity();
            string statusData = $"{position.x:F2},{position.y:F2},{position.z:F2}|{velocity.magnitude:F2}";

            SendPacket(packet.sender, "status_response", statusData);
        }
    }

    public void SendPacket(string recipient, string messageType, string data = "")
    {
        Packet packet = new Packet(receiverId, recipient, messageType, data);

        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.BroadcastPacket(packet);
        }
        else
        {
            Debug.LogError($"[{receiverId}] PacketHandler not found! Cannot send packet.");
        }
    }
}
