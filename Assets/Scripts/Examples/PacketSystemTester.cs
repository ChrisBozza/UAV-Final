using UnityEngine;

public class PacketSystemTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public string targetReceiverId = "drone1";
    public float testInterval = 5f;
    public bool autoSendTests = false;

    [Header("DDoS Simulation")]
    public bool simulateDDoS = false;
    [Range(0f, 1f)]
    public float dropRate = 0.3f;

    private float nextTestTime;

    void Update()
    {
        if (autoSendTests && Time.time >= nextTestTime)
        {
            SendTestPacket();
            nextTestTime = Time.time + testInterval;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SendTestPacket();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            ToggleDDoSOnAllReceivers();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowStatistics();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearStatistics();
        }
    }

    void SendTestPacket()
    {
        if (PacketHandler.Instance == null)
        {
            Debug.LogError("[PacketSystemTester] PacketHandler not found!");
            return;
        }

        Vector3 randomPosition = new Vector3(
            Random.Range(-50f, 50f),
            Random.Range(10f, 30f),
            Random.Range(-50f, 50f)
        );

        string positionData = $"{randomPosition.x:F2},{randomPosition.y:F2},{randomPosition.z:F2}";
        Packet packet = new Packet("PacketSystemTester", targetReceiverId, "set_target", positionData);

        PacketHandler.Instance.BroadcastPacket(packet);
        Debug.Log($"[PacketSystemTester] Sent test packet (seq: {packet.sequenceNumber}) to {targetReceiverId} with position {randomPosition}");
    }

    void ToggleDDoSOnAllReceivers()
    {
        simulateDDoS = !simulateDDoS;

        PacketReceiver[] receivers = FindObjectsByType<PacketReceiver>(FindObjectsSortMode.None);
        foreach (PacketReceiver receiver in receivers)
        {
            receiver.enableDDoSSimulation = simulateDDoS;
            receiver.packetDropRate = dropRate;
        }

        Debug.Log($"[PacketSystemTester] DDoS simulation {(simulateDDoS ? "ENABLED" : "DISABLED")} on {receivers.Length} receivers (drop rate: {dropRate * 100f}%)");
    }

    void ShowStatistics()
    {
        if (PacketHandler.Instance == null)
        {
            Debug.LogError("[PacketSystemTester] PacketHandler not found!");
            return;
        }

        PacketHandler handler = PacketHandler.Instance;
        float deliveryRate = handler.totalPacketsSent > 0 
            ? (float)handler.totalPacketsDelivered / handler.totalPacketsSent * 100f 
            : 0f;

        Debug.Log("=== PACKET HANDLER STATISTICS ===");
        Debug.Log($"Total Packets Sent: {handler.totalPacketsSent}");
        Debug.Log($"Total Packets Delivered: {handler.totalPacketsDelivered}");
        Debug.Log($"Total ACKs Received: {handler.totalAcksReceived}");
        Debug.Log($"Total Retransmissions: {handler.totalRetransmissions}");
        Debug.Log($"Total Packets Dropped: {handler.totalPacketsDropped}");
        Debug.Log($"Pending ACKs: {handler.GetPendingAckCount()}");
        Debug.Log($"Delivery Rate: {deliveryRate:F1}%");
        Debug.Log($"Registered Receivers: {handler.GetReceiverCount()}");
    }

    void ClearStatistics()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.ClearStatistics();
            Debug.Log("[PacketSystemTester] Statistics cleared!");
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Packet System Tester", GUI.skin.box);
        GUILayout.Space(5);

        if (GUILayout.Button("Send Test Packet (T)"))
        {
            SendTestPacket();
        }

        if (GUILayout.Button($"Toggle DDoS: {(simulateDDoS ? "ON" : "OFF")} (D)"))
        {
            ToggleDDoSOnAllReceivers();
        }

        if (GUILayout.Button("Show Statistics (S)"))
        {
            ShowStatistics();
        }

        if (GUILayout.Button("Clear Statistics (C)"))
        {
            ClearStatistics();
        }

        GUILayout.Space(5);
        GUILayout.Label($"Target: {targetReceiverId}");
        GUILayout.Label($"Drop Rate: {dropRate * 100f:F0}%");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
