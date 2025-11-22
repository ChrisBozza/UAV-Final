using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupConsoleTextVisibility : EditorWindow
{
    [MenuItem("Tools/Packet Monitor/Make Text Visible NOW")]
    public static void MakeVisible()
    {
        PacketMonitorUI monitor = FindFirstObjectByType<PacketMonitorUI>();
        
        if (monitor == null)
        {
            Debug.LogError("No PacketMonitorUI found!");
            return;
        }
        
        RectTransform monitorRect = monitor.GetComponent<RectTransform>();
        monitorRect.localScale = Vector3.one;
        monitorRect.anchorMin = new Vector2(0.05f, 0.05f);
        monitorRect.anchorMax = new Vector2(0.95f, 0.95f);
        monitorRect.anchoredPosition = Vector2.zero;
        monitorRect.sizeDelta = Vector2.zero;
        EditorUtility.SetDirty(monitor.gameObject);
        
        Transform consoleTextTransform = monitor.transform.Find("ScrollView/Viewport/Content/ConsoleText");
        
        if (consoleTextTransform == null)
        {
            Debug.LogError("ConsoleText not found!");
            return;
        }
        
        TextMeshProUGUI consoleText = consoleTextTransform.GetComponent<TextMeshProUGUI>();
        
        if (consoleText != null)
        {
            consoleText.fontSize = 16;
            consoleText.fontStyle = FontStyles.Normal;
            consoleText.color = Color.white;
            consoleText.alignment = TextAlignmentOptions.TopLeft;
            consoleText.overflowMode = TextOverflowModes.Overflow;
            consoleText.textWrappingMode = TextWrappingModes.NoWrap;
            consoleText.margin = new Vector4(10, 10, 10, 10);
            consoleText.text = "TEST: Packet monitor console\nLine 2\nLine 3\nYou should see this text!";
            
            EditorUtility.SetDirty(consoleText);
        }
        
        RectTransform consoleRect = consoleTextTransform.GetComponent<RectTransform>();
        consoleRect.anchorMin = new Vector2(0, 0);
        consoleRect.anchorMax = new Vector2(1, 1);
        consoleRect.pivot = new Vector2(0, 1);
        consoleRect.anchoredPosition = Vector2.zero;
        consoleRect.sizeDelta = Vector2.zero;
        
        ContentSizeFitter fitter = consoleTextTransform.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }
        
        Transform content = monitor.transform.Find("ScrollView/Viewport/Content");
        if (content != null)
        {
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            
            EditorUtility.SetDirty(content.gameObject);
        }
        
        Debug.Log("Console text visibility fixed! Check the Scene/Game view.");
        EditorUtility.SetDirty(consoleTextTransform.gameObject);
    }
}
