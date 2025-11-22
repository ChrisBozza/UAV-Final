using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class UpdatePacketMonitorUI : EditorWindow
{
    [MenuItem("Tools/Packet Monitor/Update Font Sizes")]
    public static void UpdateFontSizes()
    {
        PacketMonitorUI monitor = FindFirstObjectByType<PacketMonitorUI>();
        
        if (monitor == null)
        {
            Debug.LogError("[UpdatePacketMonitor] No PacketMonitorUI found in scene!");
            return;
        }
        
        GameObject monitorPanel = monitor.gameObject;
        
        Transform header = monitorPanel.transform.Find("Header");
        if (header != null)
        {
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 60);
            
            TextMeshProUGUI title = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.fontSize = 28;
                title.fontStyle = FontStyles.Bold;
            }
        }
        
        Transform toolbar = monitorPanel.transform.Find("Toolbar");
        if (toolbar != null)
        {
            RectTransform toolbarRect = toolbar.GetComponent<RectTransform>();
            toolbarRect.anchoredPosition = new Vector2(0, -60);
            toolbarRect.sizeDelta = new Vector2(0, 50);
            
            HorizontalLayoutGroup layout = toolbar.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 15;
                layout.padding = new RectOffset(15, 15, 10, 10);
            }
            
            UpdateToggleText(toolbar.Find("FilterDDoSToggle"), 20);
            UpdateToggleText(toolbar.Find("AutoscrollToggle"), 20);
            
            Transform clearButton = toolbar.Find("ClearButton");
            if (clearButton != null)
            {
                LayoutElement le = clearButton.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = 120;
                    le.preferredHeight = 40;
                }
                
                TextMeshProUGUI buttonText = clearButton.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.fontSize = 20;
                }
            }
        }
        
        Transform scrollView = monitorPanel.transform.Find("ScrollView");
        if (scrollView != null)
        {
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.offsetMin = new Vector2(0, 50);
            scrollRect.offsetMax = new Vector2(0, -110);
        }
        
        Transform footer = monitorPanel.transform.Find("Footer");
        if (footer != null)
        {
            RectTransform footerRect = footer.GetComponent<RectTransform>();
            footerRect.sizeDelta = new Vector2(0, 50);
            
            HorizontalLayoutGroup layout = footer.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 30;
                layout.padding = new RectOffset(15, 15, 10, 10);
            }
            
            TextMeshProUGUI packetCount = footer.Find("PacketCount")?.GetComponent<TextMeshProUGUI>();
            if (packetCount != null)
            {
                packetCount.fontSize = 18;
            }
            
            TextMeshProUGUI filteredCount = footer.Find("FilteredCount")?.GetComponent<TextMeshProUGUI>();
            if (filteredCount != null)
            {
                filteredCount.fontSize = 18;
            }
        }
        
        UpdateRowPrefab(monitor);
        
        EditorUtility.SetDirty(monitorPanel);
        Debug.Log("[UpdatePacketMonitor] Font sizes and spacing updated successfully!");
    }
    
    private static void UpdateToggleText(Transform toggle, int fontSize)
    {
        if (toggle == null) return;
        
        LayoutElement le = toggle.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minWidth = 250;
            le.preferredHeight = 30;
        }
        
        Transform background = toggle.Find("Background");
        if (background != null)
        {
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(30, 30);
        }
        
        TextMeshProUGUI label = toggle.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.fontSize = fontSize;
            
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(35, 0);
        }
    }
    
    private static void UpdateRowPrefab(PacketMonitorUI monitor)
    {
        SerializedObject so = new SerializedObject(monitor);
        GameObject rowPrefab = so.FindProperty("packetRowPrefab").objectReferenceValue as GameObject;
        
        if (rowPrefab == null)
        {
            Debug.LogWarning("[UpdatePacketMonitor] No row prefab found!");
            return;
        }
        
        string prefabPath = AssetDatabase.GetAssetPath(rowPrefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        RectTransform rect = prefabInstance.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 35);
        
        LayoutElement layoutElement = prefabInstance.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.minHeight = 35;
            layoutElement.preferredHeight = 35;
        }
        
        HorizontalLayoutGroup layout = prefabInstance.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
        }
        
        UpdateRowField(prefabInstance.transform, "Number", 80, 20, false);
        UpdateRowField(prefabInstance.transform, "Time", 100, 20, false);
        UpdateRowField(prefabInstance.transform, "Source", 180, 20, false);
        UpdateRowField(prefabInstance.transform, "Destination", 180, 20, false);
        UpdateRowField(prefabInstance.transform, "Type", 150, 20, false);
        UpdateRowField(prefabInstance.transform, "Data", 400, 20, true);
        
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        Debug.Log("[UpdatePacketMonitor] Row prefab updated!");
    }
    
    private static void UpdateRowField(Transform parent, string name, float width, int fontSize, bool flexible)
    {
        Transform field = parent.Find(name);
        if (field == null) return;
        
        LayoutElement le = field.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredWidth = width;
            le.flexibleWidth = flexible ? 1 : 0;
        }
        
        TextMeshProUGUI text = field.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = fontSize;
        }
    }
}
