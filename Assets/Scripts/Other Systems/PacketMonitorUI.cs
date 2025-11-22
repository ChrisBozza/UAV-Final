using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class PacketMonitorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI consoleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Toggle filterDDoSToggle;
    [SerializeField] private Toggle autoscrollToggle;
    [SerializeField] private Button clearButton;
    [SerializeField] private TextMeshProUGUI packetCountText;
    [SerializeField] private TextMeshProUGUI filteredCountText;

    [Header("Monitor Settings")]
    [SerializeField] private int maxLines = 50;
    [SerializeField] private bool filterDDoSPackets = false;
    [SerializeField] private bool autoscroll = true;
    [SerializeField] private string junkMessageType = "junk_data";
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool useRichText = false;

    [Header("Color Coding")]
    [SerializeField] private string normalPacketColor = "#E0E0E0";
    [SerializeField] private string ackPacketColor = "#80FF80";
    [SerializeField] private string ddosPacketColor = "#FF4D4D";

    private StringBuilder consoleBuilder = new StringBuilder(10000);
    private List<string> lines = new List<string>(100);
    private Queue<Packet> pendingPackets = new Queue<Packet>();
    private int totalPacketCount = 0;
    private int displayedPacketCount = 0;
    private float startTime;
    private float lastUpdateTime;
    private bool needsUpdate = false;

    void Start()
    {
        startTime = Time.time;
        lastUpdateTime = Time.time;
        
        if (string.IsNullOrEmpty(normalPacketColor)) normalPacketColor = "#E0E0E0";
        if (string.IsNullOrEmpty(ackPacketColor)) ackPacketColor = "#80FF80";
        if (string.IsNullOrEmpty(ddosPacketColor)) ddosPacketColor = "#FF4D4D";

        if (filterDDoSToggle != null)
        {
            filterDDoSToggle.isOn = filterDDoSPackets;
            filterDDoSToggle.onValueChanged.AddListener(OnFilterDDoSToggled);
        }

        if (autoscrollToggle != null)
        {
            autoscrollToggle.isOn = autoscroll;
            autoscrollToggle.onValueChanged.AddListener(OnAutoscrollToggled);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearPackets);
        }

        SubscribeToPacketHandler();
        UpdateStatistics();
    }
    
    void Update()
    {
        if (needsUpdate && Time.time - lastUpdateTime >= updateInterval)
        {
            ProcessPendingPackets();
            lastUpdateTime = Time.time;
            needsUpdate = false;
        }
    }
    
    void OnEnable()
    {
        if (Time.time > 0)
        {
            SubscribeToPacketHandler();
        }
    }
    
    private void SubscribeToPacketHandler()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnPacketBroadcast -= OnPacketBroadcast;
            PacketHandler.Instance.OnPacketBroadcast += OnPacketBroadcast;
        }
        else
        {
            Invoke(nameof(RetrySubscribe), 1f);
        }
    }

    private void RetrySubscribe()
    {
        SubscribeToPacketHandler();
    }

    void OnDestroy()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnPacketBroadcast -= OnPacketBroadcast;
        }
    }
    
    void OnDisable()
    {
        if (PacketHandler.Instance != null)
        {
            PacketHandler.Instance.OnPacketBroadcast -= OnPacketBroadcast;
        }
    }

    private void OnPacketBroadcast(Packet packet)
    {
        totalPacketCount++;

        if (filterDDoSPackets && packet.messageType == junkMessageType)
        {
            return;
        }

        pendingPackets.Enqueue(packet);
        needsUpdate = true;
    }
    
    private void ProcessPendingPackets()
    {
        if (consoleText == null || pendingPackets.Count == 0)
        {
            return;
        }
        
        int processedCount = 0;
        int batchSize = Mathf.Min(20, pendingPackets.Count);
        
        while (pendingPackets.Count > 0 && processedCount < batchSize)
        {
            Packet packet = pendingPackets.Dequeue();
            AddPacketLine(packet);
            processedCount++;
        }
        
        if (pendingPackets.Count > 100)
        {
            pendingPackets.Clear();
        }
        
        RebuildConsoleText();
        UpdateStatistics();
        
        if (autoscroll && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void AddPacketLine(Packet packet)
    {
        if (lines.Count >= maxLines)
        {
            lines.RemoveRange(0, maxLines / 4);
        }

        displayedPacketCount++;
        float timestamp = Time.time - startTime;

        string colorCode = "";
        string colorEnd = "";
        
        if (useRichText)
        {
            string colorHex = normalPacketColor;
            if (packet.messageType.Contains("ACK") || packet.messageType.Contains("ack"))
            {
                colorHex = ackPacketColor;
            }
            else if (packet.messageType == junkMessageType)
            {
                colorHex = ddosPacketColor;
            }
            colorCode = $"<color={colorHex}>";
            colorEnd = "</color>";
        }

        string dataPreview = packet.data ?? "";
        if (dataPreview.Length > 20)
        {
            dataPreview = dataPreview.Substring(0, 17) + "...";
        }

        string line = $"{colorCode}[{displayedPacketCount:D4}] {timestamp:F1}s | {packet.sender,-12} → {packet.recipient,-12} | {packet.messageType,-15} | {dataPreview}{colorEnd}";
        lines.Add(line);
    }
    
    private void RebuildConsoleText()
    {
        consoleBuilder.Clear();
        
        int startIndex = Mathf.Max(0, lines.Count - maxLines);
        for (int i = startIndex; i < lines.Count; i++)
        {
            consoleBuilder.AppendLine(lines[i]);
        }
        
        consoleText.text = consoleBuilder.ToString();
    }

    private void OnFilterDDoSToggled(bool value)
    {
        filterDDoSPackets = value;
    }

    private void OnAutoscrollToggled(bool value)
    {
        autoscroll = value;
    }

    private void ClearPackets()
    {
        lines.Clear();
        pendingPackets.Clear();
        consoleBuilder.Clear();
        
        if (consoleText != null)
        {
            consoleText.text = "";
        }
        
        displayedPacketCount = 0;
        totalPacketCount = 0;
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        if (packetCountText != null)
        {
            packetCountText.text = $"Total Packets: {totalPacketCount}";
        }

        if (filteredCountText != null)
        {
            int filtered = totalPacketCount - displayedPacketCount;
            filteredCountText.text = $"Displayed: {displayedPacketCount} | Filtered: {filtered}";
        }
    }
}
