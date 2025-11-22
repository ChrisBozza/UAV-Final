using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class ConvertPacketMonitorToConsole : EditorWindow
{
    [MenuItem("Tools/Packet Monitor/Convert to Console Style")]
    public static void Convert()
    {
        PacketMonitorUI monitor = FindFirstObjectByType<PacketMonitorUI>();
        
        if (monitor == null)
        {
            Debug.LogError("[ConvertToConsole] No PacketMonitorUI found in scene!");
            return;
        }
        
        GameObject monitorPanel = monitor.gameObject;
        Transform scrollView = monitorPanel.transform.Find("ScrollView");
        
        if (scrollView == null)
        {
            Debug.LogError("[ConvertToConsole] ScrollView not found!");
            return;
        }
        
        Transform viewport = scrollView.Find("Viewport");
        Transform content = viewport?.Find("Content");
        
        if (content != null)
        {
            foreach (Transform child in content)
            {
                DestroyImmediate(child.gameObject);
            }
            
            DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
            DestroyImmediate(content.GetComponent<ContentSizeFitter>());
            
            GameObject consoleTextObj = new GameObject("ConsoleText");
            consoleTextObj.transform.SetParent(content, false);
            
            RectTransform textRect = consoleTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0, 1000);
            
            TextMeshProUGUI consoleText = consoleTextObj.AddComponent<TextMeshProUGUI>();
            consoleText.fontSize = 18;
            consoleText.color = Color.white;
            consoleText.alignment = TextAlignmentOptions.TopLeft;
            consoleText.overflowMode = TextOverflowModes.Overflow;
            consoleText.enableWordWrapping = false;
            consoleText.richText = true;
            consoleText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            consoleText.text = "Waiting for packets...";
            
            ContentSizeFitter csf = consoleTextObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            SerializedObject so = new SerializedObject(monitor);
            so.FindProperty("consoleText").objectReferenceValue = consoleText;
            so.ApplyModifiedProperties();
            
            Debug.Log("[ConvertToConsole] Successfully converted to console style!");
        }
        
        EditorUtility.SetDirty(monitorPanel);
    }
}
