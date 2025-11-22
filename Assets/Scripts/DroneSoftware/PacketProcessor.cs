using UnityEngine;
using System.Collections;

public enum ProcessingMethod
{
    Normal,
    StandardDDoS,
    NovelDDoS
}

public class PacketProcessor : MonoBehaviour
{
    [Header("Processing Configuration")]
    public ProcessingMethod processingMethod = ProcessingMethod.Normal;
    
    [Header("Processing Rates (packets/second)")]
    [Tooltip("Normal operations: Fast forwarding without deep inspection")]
    public float normalPacketsPerSecond = 200f;
    
    [Tooltip("Standard DDoS: Connection tracking + stateful inspection overhead.\nBased on maintaining connection tables and checking for unacknowledged packets.\nReal hardware can do millions/sec, but scaled down for simulation.")]
    public float standardDDoSPacketsPerSecond = 50f;
    
    [Tooltip("Novel DDoS: Your custom mitigation approach (placeholder for now)")]
    public float novelDDoSPacketsPerSecond = 150f;

    [Header("Debug")]
    public bool logProcessingTimes = false;

    private bool isProcessing = false;
    private System.Action<Packet> onProcessingComplete;

    public bool IsProcessing => isProcessing;

    public void ProcessPacket(Packet packet, System.Action<Packet> completionCallback)
    {
        onProcessingComplete = completionCallback;
        StartCoroutine(ProcessPacketCoroutine(packet));
    }

    private IEnumerator ProcessPacketCoroutine(Packet packet)
    {
        isProcessing = true;
        float processingTime = GetProcessingTime();

        if (logProcessingTimes)
        {
            float packetsPerSec = 1f / processingTime;
            Debug.Log($"[PacketProcessor] Processing packet {packet.packetId} using {processingMethod} method (time: {processingTime:F4}s, rate: {packetsPerSec:F1} pkt/s)");
        }

        yield return new WaitForSeconds(processingTime);

        isProcessing = false;
        onProcessingComplete?.Invoke(packet);
    }

    private float GetProcessingTime()
    {
        switch (processingMethod)
        {
            case ProcessingMethod.Normal:
                return ProcessNormal();
            
            case ProcessingMethod.StandardDDoS:
                return ProcessStandardDDoS();
            
            case ProcessingMethod.NovelDDoS:
                return ProcessNovelDDoS();
            
            default:
                return ProcessNormal();
        }
    }

    private float ProcessNormal()
    {
        return 1f / normalPacketsPerSecond;
    }

    private float ProcessStandardDDoS()
    {
        return 1f / standardDDoSPacketsPerSecond;
    }

    private float ProcessNovelDDoS()
    {
        return 1f / novelDDoSPacketsPerSecond;
    }
}
