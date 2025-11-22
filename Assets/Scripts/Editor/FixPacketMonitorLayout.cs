using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixPacketMonitorLayout : EditorWindow
{
    [MenuItem("Tools/Packet Monitor/Fix Layout Visibility")]
    public static void FixLayout()
    {
        PacketMonitorUI monitor = FindFirstObjectByType<PacketMonitorUI>();
        
        if (monitor == null)
        {
            Debug.LogError("[FixLayout] No PacketMonitorUI found in scene!");
            return;
        }
        
        GameObject monitorPanel = monitor.gameObject;
        Transform content = monitorPanel.transform.Find("ScrollView/Viewport/Content");
        
        if (content == null)
        {
            Debug.LogError("[FixLayout] Content not found!");
            return;
        }
        
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 2;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        EditorUtility.SetDirty(content.gameObject);
        Debug.Log("[FixLayout] Content layout fixed! The rows should now be visible.");
    }
}
