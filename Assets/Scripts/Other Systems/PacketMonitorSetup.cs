using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

public class PacketMonitorSetup : MonoBehaviour
{
    [MenuItem("GameObject/UI/Packet Monitor (Wireshark Style)", false, 10)]
    public static void CreatePacketMonitor()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject monitorPanel = CreateMonitorPanel(canvas.transform);
        GameObject header = CreateHeader(monitorPanel.transform);
        GameObject toolbar = CreateToolbar(monitorPanel.transform);
        GameObject scrollView = CreateScrollView(monitorPanel.transform);
        GameObject footer = CreateFooter(monitorPanel.transform);
        GameObject rowPrefab = CreatePacketRowPrefab();

        PacketMonitorUI monitorUI = monitorPanel.AddComponent<PacketMonitorUI>();
        
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        Transform content = scrollView.transform.Find("Viewport/Content");
        Toggle filterToggle = toolbar.transform.Find("FilterDDoSToggle").GetComponent<Toggle>();
        Toggle autoscrollToggle = toolbar.transform.Find("AutoscrollToggle").GetComponent<Toggle>();
        Button clearButton = toolbar.transform.Find("ClearButton").GetComponent<Button>();
        TextMeshProUGUI packetCount = footer.transform.Find("PacketCount").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI filteredCount = footer.transform.Find("FilteredCount").GetComponent<TextMeshProUGUI>();

        SerializedObject so = new SerializedObject(monitorUI);
        so.FindProperty("packetRowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("packetListContainer").objectReferenceValue = content;
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("filterDDoSToggle").objectReferenceValue = filterToggle;
        so.FindProperty("autoscrollToggle").objectReferenceValue = autoscrollToggle;
        so.FindProperty("clearButton").objectReferenceValue = clearButton;
        so.FindProperty("packetCountText").objectReferenceValue = packetCount;
        so.FindProperty("filteredCountText").objectReferenceValue = filteredCount;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = monitorPanel;
        Debug.Log("[PacketMonitorSetup] Packet Monitor created successfully!");
    }

    private static GameObject CreateMonitorPanel(Transform parent)
    {
        GameObject panel = new GameObject("PacketMonitor");
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.05f);
        rect.anchorMax = new Vector2(0.95f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        return panel;
    }

    private static GameObject CreateHeader(Transform parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);
        
        RectTransform rect = header.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, 40);
        
        Image image = header.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        GameObject titleText = new GameObject("Title");
        titleText.transform.SetParent(header.transform, false);
        
        RectTransform titleRect = titleText.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, 0);
        
        TextMeshProUGUI text = titleText.AddComponent<TextMeshProUGUI>();
        text.text = "PACKET MONITOR";
        text.fontSize = 18;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.3f, 0.8f, 1f, 1f);
        text.alignment = TextAlignmentOptions.Left;
        
        return header;
    }

    private static GameObject CreateToolbar(Transform parent)
    {
        GameObject toolbar = new GameObject("Toolbar");
        toolbar.transform.SetParent(parent, false);
        
        RectTransform rect = toolbar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -40);
        rect.sizeDelta = new Vector2(0, 35);
        
        Image image = toolbar.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        HorizontalLayoutGroup layout = toolbar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.spacing = 10;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        
        CreateToggle(toolbar.transform, "FilterDDoSToggle", "Hide DDoS Packets", false);
        CreateToggle(toolbar.transform, "AutoscrollToggle", "Auto-scroll", true);
        CreateButton(toolbar.transform, "ClearButton", "Clear");
        
        return toolbar;
    }

    private static void CreateToggle(Transform parent, string name, string label, bool isOn)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent, false);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = isOn;
        
        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(200, 25);
        
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObj.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot = new Vector2(0, 0.5f);
        bgRect.sizeDelta = new Vector2(20, 20);
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(3, 3);
        checkRect.offsetMax = new Vector2(-3, -3);
        
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.3f, 0.8f, 1f, 1f);
        
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(25, 0);
        labelRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Left;
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
    }

    private static void CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 25);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.4f, 0.2f, 0.2f, 1f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.4f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.25f, 0.25f, 1f);
        colors.pressedColor = new Color(0.6f, 0.3f, 0.3f, 1f);
        button.colors = colors;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject CreateScrollView(Transform parent)
    {
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(parent, false);
        
        RectTransform rect = scrollView.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(0, 30);
        rect.offsetMax = new Vector2(0, -75);
        
        Image image = scrollView.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;
        
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 0;
        
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        
        return scrollView;
    }

    private static GameObject CreateFooter(Transform parent)
    {
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(parent, false);
        
        RectTransform rect = footer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(0, 30);
        
        Image image = footer.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.spacing = 20;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        
        CreateLabel(footer.transform, "PacketCount", "Total Packets: 0");
        CreateLabel(footer.transform, "FilteredCount", "Displayed: 0 | Filtered: 0");
        
        return footer;
    }

    private static void CreateLabel(Transform parent, string name, string text)
    {
        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent, false);
        
        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 20);
        
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 12;
        label.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        label.alignment = TextAlignmentOptions.Left;
    }

    private static GameObject CreatePacketRowPrefab()
    {
        GameObject row = new GameObject("PacketRow");
        
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 20);
        
        Image image = row.AddComponent<Image>();
        image.color = Color.clear;
        
        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.minHeight = 20;
        layoutElement.preferredHeight = 20;
        
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 2, 2);
        
        CreateRowField(row.transform, "Number", 50);
        CreateRowField(row.transform, "Time", 70);
        CreateRowField(row.transform, "Source", 120);
        CreateRowField(row.transform, "Destination", 120);
        CreateRowField(row.transform, "Type", 100);
        CreateRowField(row.transform, "Data", 300);
        
        string prefabPath = "Assets/Prefabs/PacketRowPrefab.prefab";
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, prefabPath);
        DestroyImmediate(row);
        
        return prefab;
    }

    private static void CreateRowField(Transform parent, string name, float width)
    {
        GameObject field = new GameObject(name);
        field.transform.SetParent(parent, false);
        
        RectTransform rect = field.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 18);
        
        LayoutElement layoutElement = field.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.flexibleWidth = (name == "Data") ? 1 : 0;
        
        TextMeshProUGUI text = field.AddComponent<TextMeshProUGUI>();
        text.text = name;
        text.fontSize = 11;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontStyle = FontStyles.Normal;
    }
}
#endif
