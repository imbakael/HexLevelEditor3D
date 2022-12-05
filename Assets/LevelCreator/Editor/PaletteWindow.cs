using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class PaletteWindow : EditorWindow {

    public static PaletteWindow instance;

    public delegate void itemSelectedDelegate(PaletteItem item, Texture2D preview);
    public static event itemSelectedDelegate ItemSelectedEvent;

    private List<PaletteItem.Category> categories;
    private List<string> categoryLabels;
    private PaletteItem.Category categorySelected;

    private string path = "Assets/Resources/LevelPieces";
    private List<PaletteItem> items;
    private Dictionary<PaletteItem.Category, List<PaletteItem>> categorizedItems;
    private Dictionary<PaletteItem, Texture2D> previews;
    private Vector2 scrollPosition;
    private const float BUTTON_WIDTH = 80;
    private const float BUTTON_HEIGHT = 90;

    private GUIStyle tabStyle;

    public static void ShowPalette() {
        instance = GetWindow<PaletteWindow>();
        instance.titleContent = new GUIContent("Palette");
    }

    private void OnEnable() {
        Debug.Log("palette OnEnable called");
        if (categories == null) {
            InitCategories();
        }
        if (categorizedItems == null) {
            InitContent();
        }
        InitStyles();
    }

    private void InitStyles() {
        GUISkin skin = (GUISkin)Resources.Load("PaletteWindowSkin");
        tabStyle = skin.label;
    }

    private void InitCategories() {
        categories = EditorUtils.GetListFromEnum<PaletteItem.Category>();
        categoryLabels = new List<string>();
        foreach (PaletteItem.Category item in categories) {
            categoryLabels.Add(item.ToString());
        }
    }

    private void InitContent() {
        items = EditorUtils.GetAssetsWithScript<PaletteItem>(path);
        categorizedItems = new Dictionary<PaletteItem.Category, List<PaletteItem>>();
        previews = new Dictionary<PaletteItem, Texture2D>();
        foreach (PaletteItem.Category item in categories) {
            categorizedItems.Add(item, new List<PaletteItem>());
        }
        foreach (PaletteItem item in items) {
            categorizedItems[item.category].Add(item);
        }
    }

    private void OnDisable() {
        Debug.Log("palette OnDisable called");
    }

    private void OnDestroy() {
        Debug.Log("palette OnDestroyable called");
    }

    private void Update() {
        if (previews.Count != items.Count) {
            GeneratePreviews();
        }
    }

    private void OnGUI() {
        DrawTabs();
        DrawScroll();
    }

    private void DrawTabs() {
        int index = (int)categorySelected;
        index = GUILayout.Toolbar(index, categoryLabels.ToArray(), tabStyle);
        categorySelected = categories[index];
    }

    private void DrawScroll() {
        if (categorizedItems[categorySelected].Count == 0) {
            EditorGUILayout.HelpBox("This category is empty!", MessageType.Info);
            return;
        }
        int xCapacity = Mathf.FloorToInt(position.width / BUTTON_WIDTH);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        int selectionGridIndex = -1;
        selectionGridIndex = GUILayout.SelectionGrid(
            selectionGridIndex,
            GetGUICOntentsFromItems(),
            xCapacity,
            GetGUIStyle());
        GetSelectedItem(selectionGridIndex);
        GUILayout.EndScrollView();
    }

    private GUIContent[] GetGUICOntentsFromItems() {
        List<GUIContent> guiContents = new List<GUIContent>();
        if (previews.Count == items.Count) {
            int totalItems = categorizedItems[categorySelected].Count;
            for (int i = 0; i < totalItems; i++) {
                GUIContent guiContent = new GUIContent();
                PaletteItem paletteItem = categorizedItems[categorySelected][i];
                guiContent.text = paletteItem.itemName;
                guiContent.image = previews[paletteItem];
                guiContents.Add(guiContent);
            }
        }
        return guiContents.ToArray();
    }

    private GUIStyle GetGUIStyle() {
        var style = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.LowerCenter,
            imagePosition = ImagePosition.ImageAbove,
            fixedWidth = BUTTON_WIDTH,
            fixedHeight = BUTTON_HEIGHT
        };
        return style;
    }

    private void GetSelectedItem(int index) {
        if (index != -1) {
            PaletteItem selectedItem = categorizedItems[categorySelected][index];
            Debug.Log("selected item is : " + selectedItem.itemName);
            ItemSelectedEvent?.Invoke(selectedItem, previews[selectedItem]);
        }
    }

    private void GeneratePreviews() {
        foreach (PaletteItem item in items) {
            if (!previews.ContainsKey(item)) {
                Texture2D preview = AssetPreview.GetAssetPreview(item.gameObject);
                if (preview != null) {
                    previews.Add(item, preview);
                }
            }
        }
    }

}
