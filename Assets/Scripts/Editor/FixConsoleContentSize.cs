using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixConsoleContentSize : EditorWindow
{
    [MenuItem("Tools/Packet Monitor/Fix Console Size")]
    public static void Fix()
    {
        PacketMonitorUI monitor = FindFirstObjectByType<PacketMonitorUI>();
        
        if (monitor == null)
        {
            Debug.LogError("[FixConsoleSize] No PacketMonitorUI found!");
            return;
        }
        
        Transform content = monitor.transform.Find("ScrollView/Viewport/Content");
        
        if (content != null)
        {
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, 5000);
            
            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            if (contentFitter != null)
            {
                DestroyImmediate(contentFitter);
            }
            
            EditorUtility.SetDirty(content.gameObject);
            Debug.Log("[FixConsoleSize] Content size fixed to 5000 pixels tall!");
        }
    }
}
