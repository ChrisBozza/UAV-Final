using UnityEngine;
using System.Collections.Generic;

public class PacketHandlerDebugger : MonoBehaviour
{
    [Header("Debug Display")]
    public bool showDebugInfo = true;
    public bool showPendingAcks = true;
    public bool showStatistics = true;

    private PacketHandler handler;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;

    void Start()
    {
        handler = PacketHandler.Instance;
    }

    void OnGUI()
    {
        if (!showDebugInfo || handler == null)
            return;

        InitializeStyles();

        GUILayout.BeginArea(new Rect(10, 10, 400, 600));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Packet Handler Debug", headerStyle);

        if (showStatistics)
        {
            GUILayout.Space(10);
            GUILayout.Label("Statistics:", headerStyle);
            GUILayout.Label($"Total Packets Sent: {handler.totalPacketsSent}", labelStyle);
            GUILayout.Label($"Total Packets Delivered: {handler.totalPacketsDelivered}", labelStyle);
            GUILayout.Label($"Total ACKs Received: {handler.totalAcksReceived}", labelStyle);
            GUILayout.Label($"Total Retransmissions: {handler.totalRetransmissions}", labelStyle);
            GUILayout.Label($"Total Packets Dropped: {handler.totalPacketsDropped}", labelStyle);
            GUILayout.Label($"Registered Receivers: {handler.GetReceiverCount()}", labelStyle);
        }

        if (showPendingAcks)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Pending ACKs: {handler.GetPendingAckCount()}", headerStyle);

            Dictionary<string, PacketAcknowledgment> pendingAcks = handler.GetPendingAcks();
            foreach (var kvp in pendingAcks)
            {
                PacketAcknowledgment ack = kvp.Value;
                string packetInfo = $"Seq:{ack.packet.sequenceNumber} Type:{ack.packet.messageType}";
                GUILayout.Label($"{packetInfo}", labelStyle);
                GUILayout.Label($"  Waiting for: {string.Join(", ", ack.pendingReceivers)}", labelStyle);
                GUILayout.Label($"  Age: {ack.GetAge():F2}s | Retries: {ack.retryCount}", labelStyle);
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void InitializeStyles()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.yellow;
        }
    }
}
