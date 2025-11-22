using UnityEngine;
using System.Collections;

public enum ProcessingMethod
{
    Simple,
    Novel
}

public class PacketProcessor : MonoBehaviour
{
    [Header("Processing Configuration")]
    public ProcessingMethod processingMethod = ProcessingMethod.Simple;
    
    [Header("Processing Rates")]
    public float simplePacketsPerSecond = 100f;
    public float novelPacketsPerSecond = 200f;

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
            Debug.Log($"[PacketProcessor] Processing packet {packet.packetId} using {processingMethod} method (wait: {processingTime:F4}s)");
        }

        yield return new WaitForSeconds(processingTime);

        isProcessing = false;
        onProcessingComplete?.Invoke(packet);
    }

    private float GetProcessingTime()
    {
        switch (processingMethod)
        {
            case ProcessingMethod.Simple:
                return ProcessSimple();
            
            case ProcessingMethod.Novel:
                return ProcessNovel();
            
            default:
                return ProcessSimple();
        }
    }

    private float ProcessSimple()
    {
        return 1f / simplePacketsPerSecond;
    }

    private float ProcessNovel()
    {
        return 1f / novelPacketsPerSecond;
    }
}
