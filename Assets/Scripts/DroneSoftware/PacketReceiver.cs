using UnityEngine;
using System.Collections.Generic;

public class PacketReceiver : MonoBehaviour
{
    [Header("Receiver Identity")]
    public string receiverId;

    [Header("Packet Processing")]
    public bool logReceivedPackets = false;
    public bool logIgnoredPackets = false;
    public bool logAcknowledgments = false;

    [Header("DDoS Simulation")]
    public bool enableDDoSSimulation = false;
    [Range(0f, 1f)]
    public float packetDropRate = 0f;

    private DroneComputer droneComputer;
    private FormationKeeper formationKeeper;
    private Queue<Packet> packetQueue = new Queue<Packet>();
    private HashSet<long> processedSequenceNumbers = new HashSet<long>();

    private bool registered = false;

    void Awake()
    {
        droneComputer = GetComponent<DroneComputer>();
        formationKeeper = GetComponent<FormationKeeper>();

        if (string.IsNullOrEmpty(receiverId))
        {
            receiverId = gameObject.name;
        }

        if (PacketHandler.Instance == null)
        {
            Debug.LogWarning($"[PacketReceiver {receiverId}] PacketHandler.Instance is null in Awake! Will try again in Start.");
        }
        else
        {
            PacketHandler.Instance.RegisterReceiver(this);
            registered = true;
        }
    }

    void Start()
    {
        if (!registered && PacketHandler.Instance != null)
        {
            Debug.Log($"[PacketReceiver {receiverId}] Registering in Start() as backup.");
            PacketHandler.Instance.RegisterReceiver(this);
            registered = true;
        }
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
            if (packet.IsAck())
            {
                HandleAckPacket(packet);
                return;
            }

            if (enableDDoSSimulation && packetDropRate > 0f)
            {
                if (Random.value < packetDropRate)
                {
                    if (logReceivedPackets)
                    {
                        Debug.LogWarning($"[{receiverId}] Dropped packet (DDoS): {packet}");
                    }
                    return;
                }
            }

            if (logReceivedPackets)
            {
                Debug.Log($"[{receiverId}] Received: {packet}");
            }

            if (packet.requiresAck && PacketHandler.Instance != null && PacketHandler.Instance.enableAckSystem)
            {
                SendAcknowledgment(packet);
            }

            if (processedSequenceNumbers.Contains(packet.sequenceNumber))
            {
                if (logReceivedPackets)
                {
                    Debug.Log($"[{receiverId}] Duplicate packet detected (seq: {packet.sequenceNumber}), ignoring.");
                }
                return;
            }

            processedSequenceNumbers.Add(packet.sequenceNumber);
            packetQueue.Enqueue(packet);
        }
        else if (logIgnoredPackets)
        {
            Debug.Log($"[{receiverId}] Ignored packet for: {packet.recipient}");
        }
    }

    private void HandleAckPacket(Packet ackPacket)
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.ReceiveAcknowledgment(ackPacket.ackForPacketId, ackPacket.sender);

            if (logAcknowledgments)
            {
                Debug.Log($"[{receiverId}] Forwarded ACK from {ackPacket.sender} for packet {ackPacket.ackForPacketId}");
            }
        }
    }

    private void SendAcknowledgment(Packet originalPacket)
    {
        Packet ack = Packet.CreateAck(receiverId, originalPacket.sender, originalPacket.packetId, originalPacket.sequenceNumber);

        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.BroadcastPacket(ack);

            if (logAcknowledgments)
            {
                Debug.Log($"[{receiverId}] Sent ACK to {originalPacket.sender} for packet {originalPacket.packetId} (seq: {originalPacket.sequenceNumber})");
            }
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
            case "leader_state":
                HandleLeaderState(packet);
                break;

            case "formation_update":
                HandleFormationUpdate(packet);
                break;

            case "formation_offset":
                HandleFormationOffset(packet);
                break;

            case "formation_active":
                HandleFormationActive(packet);
                break;

            case "set_target":
                HandleSetTarget(packet);
                break;

            case "set_autopilot":
                HandleSetAutopilot(packet);
                break;

            case "power_on":
                HandlePowerOn(packet);
                break;

            case "power_off":
                HandlePowerOff(packet);
                break;

            case "set_speed_multiplier":
                HandleSetSpeedMultiplier(packet);
                break;

            case "rotate_to_direction":
                HandleRotateToDirection(packet);
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

    private void HandleLeaderState(Packet packet)
    {
        if (formationKeeper != null)
        {
            formationKeeper.OnLeaderStateReceived(packet.sender, packet.data);
        }
    }

    private void HandleFormationUpdate(Packet packet)
    {
        if (formationKeeper != null)
        {
            Debug.Log($"[{receiverId}] Formation update received from {packet.sender}");
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

    private void HandleFormationOffset(Packet packet)
    {
        if (formationKeeper != null)
        {
            Vector3 offset = ParseVector3(packet.data);
            formationKeeper.SetFormationOffset(offset);
        }
    }

    private void HandleFormationActive(Packet packet)
    {
        if (formationKeeper != null)
        {
            bool active = packet.data == "true";
            formationKeeper.SetFormationActive(active);
        }
    }

    private void HandleSetTarget(Packet packet)
    {
        if (droneComputer != null)
        {
            Vector3 targetPosition = ParseVector3(packet.data);
            droneComputer.SetTargetPosition(targetPosition);
        }
    }

    private void HandleSetAutopilot(Packet packet)
    {
        if (droneComputer != null)
        {
            bool autopilot = packet.data == "true";
            droneComputer.autoPilot = autopilot;
        }
    }

    private void HandlePowerOn(Packet packet)
    {
        if (droneComputer != null)
        {
            droneComputer.PowerOnEngine();
        }
    }

    private void HandlePowerOff(Packet packet)
    {
        if (droneComputer != null)
        {
            droneComputer.PowerOffEngine();
        }
    }

    private void HandleSetSpeedMultiplier(Packet packet)
    {
        if (droneComputer != null)
        {
            float multiplier = float.Parse(packet.data);
            droneComputer.SetSpeedMultiplier(multiplier);
        }
    }

    private void HandleRotateToDirection(Packet packet)
    {
        if (droneComputer != null)
        {
            Vector3 direction = ParseVector3(packet.data);
            droneComputer.RotateTowardsDirection(direction);
        }
    }

    private Vector3 ParseVector3(string vectorString)
    {
        string[] components = vectorString.Split(',');
        if (components.Length >= 3)
        {
            float x = float.Parse(components[0]);
            float y = float.Parse(components[1]);
            float z = float.Parse(components[2]);
            return new Vector3(x, y, z);
        }
        return Vector3.zero;
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
