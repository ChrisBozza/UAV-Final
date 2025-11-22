using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DDoSAttacker : MonoBehaviour
{
    [Header("Attack Configuration")]
    [Tooltip("Enable/disable the DDoS attack")]
    public bool attackActive = false;
    
    [Tooltip("Packets sent per second during attack")]
    public float attackRate = 500f;
    
    [Tooltip("If true, varies attack rate randomly within range")]
    public bool randomizeRate = false;
    
    [Range(0f, 1f)]
    [Tooltip("Maximum random variation (0.5 = ±50%)")]
    public float rateVariation = 0.3f;

    [Header("Range Configuration")]
    [Tooltip("Attack range in Unity units")]
    public float attackRange = 50f;
    
    [Tooltip("Direct references to drone GameObjects to track")]
    public List<GameObject> targetDrones = new List<GameObject>();
    
    [Tooltip("Auto-populate drones by finding all GameObjects with PacketReceiver component")]
    public bool autoPopulateDrones = true;
    
    [Tooltip("Only attack drones within range")]
    public bool enforceRange = true;
    
    [Tooltip("Show attack range sphere in Game view")]
    public bool showRangeSphere = true;
    
    [Tooltip("Opacity of the range sphere (0-1)")]
    [Range(0f, 1f)]
    public float sphereOpacity = 0.15f;
    
    [Tooltip("Show attack range in Scene view")]
    public bool showRangeGizmo = true;

    [Header("Target Configuration")]
    [Tooltip("If true, sends to all targets in range. If false, picks random target")]
    public bool attackAllTargets = true;

    [Header("Junk Packet Settings")]
    [Tooltip("Message type for junk packets")]
    public string junkMessageType = "junk_data";
    
    [Tooltip("If true, generates random data payload")]
    public bool randomPayload = true;
    
    [Tooltip("Size of random payload in characters")]
    public int payloadSize = 64;

    [Header("Attack Patterns")]
    [Tooltip("Burst attack: sends packets in bursts instead of steady stream")]
    public bool burstMode = false;
    
    [Tooltip("Duration of each burst in seconds")]
    public float burstDuration = 2f;
    
    [Tooltip("Pause between bursts in seconds")]
    public float burstPauseDuration = 3f;

    [Header("Statistics")]
    public int totalPacketsSent = 0;
    public float currentAttackRate = 0f;
    public int dronesInRange = 0;

    [Header("Debug")]
    public bool logAttackPackets = false;
    public bool logRangeChanges = true;

    private PacketReceiver attackerReceiver;
    private float nextPacketTime = 0f;
    private bool inBurst = true;
    private float burstTimer = 0f;
    private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private List<GameObject> dronesInRangeList = new List<GameObject>();
    
    private GameObject rangeSphereObject;
    private MeshRenderer rangeSphereRenderer;
    private Material rangeSphereMaterial;
    private float previousAttackRange;

    void Start()
    {
        attackerReceiver = GetComponent<PacketReceiver>();
        
        if (attackerReceiver == null)
        {
            attackerReceiver = gameObject.AddComponent<PacketReceiver>();
            attackerReceiver.receiverId = "DDoSAttacker";
        }

        if (autoPopulateDrones)
        {
            PopulateTargetDrones();
        }

        if (targetDrones.Count == 0)
        {
            Debug.LogWarning("[DDoSAttacker] No target drones specified. Enable 'Auto Populate Drones' or manually add drone GameObjects.");
        }

        CreateRangeSphere();
        UpdateDronesInRange();
        previousAttackRange = attackRange;
    }

    void Update()
    {
        UpdateDronesInRange();
        UpdateRangeSphere();

        if (!attackActive || dronesInRangeList.Count == 0)
        {
            return;
        }

        if (burstMode)
        {
            UpdateBurstMode();
        }
        else
        {
            UpdateSteadyMode();
        }
    }

    private void UpdateDronesInRange()
    {
        int previousCount = dronesInRangeList.Count;
        dronesInRangeList.Clear();

        foreach (GameObject drone in targetDrones)
        {
            if (drone == null) continue;

            if (!enforceRange || IsInRange(drone))
            {
                dronesInRangeList.Add(drone);
            }
        }

        dronesInRange = dronesInRangeList.Count;

        if (logRangeChanges && dronesInRange != previousCount)
        {
            Debug.Log($"[DDoSAttacker] Drones in range changed: {previousCount} → {dronesInRange}");
        }
    }

    private bool IsInRange(GameObject drone)
    {
        float distance = Vector3.Distance(transform.position, drone.transform.position);
        return distance <= attackRange;
    }

    private void UpdateSteadyMode()
    {
        if (Time.time >= nextPacketTime)
        {
            SendJunkPacket();
            ScheduleNextPacket();
        }
    }

    private void UpdateBurstMode()
    {
        burstTimer += Time.deltaTime;

        if (inBurst)
        {
            if (burstTimer >= burstDuration)
            {
                inBurst = false;
                burstTimer = 0f;
                if (logAttackPackets)
                {
                    Debug.Log("[DDoSAttacker] Burst ended. Pausing...");
                }
            }
            else
            {
                if (Time.time >= nextPacketTime)
                {
                    SendJunkPacket();
                    ScheduleNextPacket();
                }
            }
        }
        else
        {
            if (burstTimer >= burstPauseDuration)
            {
                inBurst = true;
                burstTimer = 0f;
                if (logAttackPackets)
                {
                    Debug.Log("[DDoSAttacker] Starting new burst...");
                }
            }
        }
    }

    private void ScheduleNextPacket()
    {
        float rate = attackRate;
        
        if (randomizeRate)
        {
            float variation = attackRate * rateVariation;
            rate = Random.Range(attackRate - variation, attackRate + variation);
        }
        
        currentAttackRate = rate;
        float interval = 1f / rate;
        nextPacketTime = Time.time + interval;
    }

    private void SendJunkPacket()
    {
        if (PacketHandler.Instance == null)
        {
            Debug.LogError("[DDoSAttacker] PacketHandler not found!");
            return;
        }

        if (dronesInRangeList.Count == 0)
        {
            return;
        }

        GameObject targetDrone = GetRandomTargetDrone();
        if (targetDrone == null) return;

        PacketReceiver targetReceiver = targetDrone.GetComponent<PacketReceiver>();
        if (targetReceiver == null)
        {
            Debug.LogWarning($"[DDoSAttacker] Target drone {targetDrone.name} has no PacketReceiver!");
            return;
        }

        string payload = randomPayload ? GenerateRandomPayload() : "JUNK";
        
        Packet junkPacket = new Packet(
            attackerReceiver.receiverId,
            targetReceiver.receiverId,
            junkMessageType,
            payload
        );

        PacketHandler.Instance.BroadcastPacket(junkPacket);
        totalPacketsSent++;

        if (logAttackPackets && totalPacketsSent % 100 == 0)
        {
            Debug.Log($"[DDoSAttacker] Sent {totalPacketsSent} junk packets. Current rate: {currentAttackRate:F1} pkt/s, Targets in range: {dronesInRange}");
        }
    }

    private GameObject GetRandomTargetDrone()
    {
        if (dronesInRangeList.Count == 0)
            return null;

        return dronesInRangeList[Random.Range(0, dronesInRangeList.Count)];
    }

    private string GenerateRandomPayload()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(payloadSize);
        
        for (int i = 0; i < payloadSize; i++)
        {
            sb.Append(ALPHABET[Random.Range(0, ALPHABET.Length)]);
        }
        
        return sb.ToString();
    }

    public void StartAttack()
    {
        attackActive = true;
        totalPacketsSent = 0;
        nextPacketTime = Time.time;
        Debug.Log("[DDoSAttacker] Attack started!");
    }

    public void StopAttack()
    {
        attackActive = false;
        Debug.Log($"[DDoSAttacker] Attack stopped. Total packets sent: {totalPacketsSent}");
    }

    public void SetAttackRate(float rate)
    {
        attackRate = Mathf.Max(1f, rate);
    }

    public void AddTargetDrone(GameObject drone)
    {
        if (!targetDrones.Contains(drone))
        {
            targetDrones.Add(drone);
            Debug.Log($"[DDoSAttacker] Added target drone: {drone.name}");
        }
    }

    public void RemoveTargetDrone(GameObject drone)
    {
        if (targetDrones.Remove(drone))
        {
            Debug.Log($"[DDoSAttacker] Removed target drone: {drone.name}");
        }
    }

    public int GetDronesInRangeCount()
    {
        return dronesInRange;
    }

    public void PopulateTargetDrones()
    {
        targetDrones.Clear();
        
        PacketReceiver[] allReceivers = FindObjectsByType<PacketReceiver>(FindObjectsSortMode.None);
        
        foreach (PacketReceiver receiver in allReceivers)
        {
            if (receiver.gameObject != this.gameObject && receiver.receiverId != "DDoSAttacker")
            {
                targetDrones.Add(receiver.gameObject);
            }
        }
        
        Debug.Log($"[DDoSAttacker] Auto-populated {targetDrones.Count} target drones");
    }

    private void CreateRangeSphere()
    {
        rangeSphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rangeSphereObject.name = "DDoS Range Indicator";
        rangeSphereObject.transform.SetParent(transform);
        rangeSphereObject.transform.localPosition = Vector3.zero;
        rangeSphereObject.transform.localScale = Vector3.one * attackRange * 2f;

        Collider sphereCollider = rangeSphereObject.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            Destroy(sphereCollider);
        }

        rangeSphereRenderer = rangeSphereObject.GetComponent<MeshRenderer>();
        
        rangeSphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rangeSphereMaterial.SetFloat("_Surface", 1);
        rangeSphereMaterial.SetFloat("_Blend", 0);
        rangeSphereMaterial.SetFloat("_AlphaClip", 0);
        rangeSphereMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rangeSphereMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rangeSphereMaterial.SetFloat("_ZWrite", 0);
        rangeSphereMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        rangeSphereMaterial.renderQueue = 3000;
        
        rangeSphereMaterial.SetColor("_BaseColor", new Color(1f, 0f, 0f, sphereOpacity));
        rangeSphereMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rangeSphereMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        
        rangeSphereRenderer.material = rangeSphereMaterial;
        rangeSphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeSphereRenderer.receiveShadows = false;
        
        rangeSphereObject.SetActive(showRangeSphere);
    }

    private void UpdateRangeSphere()
    {
        if (rangeSphereObject == null)
        {
            return;
        }

        if (Mathf.Abs(previousAttackRange - attackRange) > 0.01f)
        {
            rangeSphereObject.transform.localScale = Vector3.one * attackRange * 2f;
            previousAttackRange = attackRange;
        }

        if (rangeSphereObject.activeSelf != showRangeSphere)
        {
            rangeSphereObject.SetActive(showRangeSphere);
        }

        if (rangeSphereMaterial != null)
        {
            Color currentColor = rangeSphereMaterial.GetColor("_BaseColor");
            if (Mathf.Abs(currentColor.a - sphereOpacity) > 0.01f)
            {
                rangeSphereMaterial.SetColor("_BaseColor", new Color(1f, 0f, 0f, sphereOpacity));
            }
        }
    }

    void OnDestroy()
    {
        if (rangeSphereMaterial != null)
        {
            Destroy(rangeSphereMaterial);
        }
        if (rangeSphereObject != null)
        {
            Destroy(rangeSphereObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo) return;

        Gizmos.color = attackActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying && dronesInRangeList != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject drone in dronesInRangeList)
            {
                if (drone != null)
                {
                    Gizmos.DrawLine(transform.position, drone.transform.position);
                }
            }
        }
    }
}
