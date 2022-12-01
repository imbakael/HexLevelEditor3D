using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

[CustomEditor(typeof(Level))]
public class LevelInspector : Editor {
    public enum Mode {
        View,
        Paint,
        Edit,
        Erase
    }

    private Mode currentMode;
    private Mode selectedMode;

    private List<string> modeLabels;
    private List<string> categoryLabels;

    private Level level;
    private int newTotalColumns;
    private int newTotalRows;

    private PaletteItem itemSelected;
    private Texture2D itemPreview;
    private LevelPiece pieceSelected;
    private SpriteRenderer spriteRendererInspected;

    private float alpha = 1f;

    private int originalPosX;
    private int originalPosY;

    private void OnEnable() {
        Debug.Log("level onEnable");
        level = target as Level;
        level.transform.hideFlags = HideFlags.NotEditable;
        InitLevel();
        ResetResizeValues();
        InitMode();
        PaletteWindow.ItemSelectedEvent += UpdateCurrentPiece;
    }

    private void OnDisable() {
        Debug.Log("level onDisable");
        PaletteWindow.ItemSelectedEvent -= UpdateCurrentPiece;
    }

    private void UpdateCurrentPiece(PaletteItem item, Texture2D preview) {
        itemSelected = item;
        itemPreview = preview;
        pieceSelected = item.GetComponent<LevelPiece>();
        Repaint();
    }

    private void OnDestroy() {
        Debug.Log("onDestroy was called");
    }

    #region Init
    private void InitLevel() {
        if (level.Pieces == null || level.Pieces.Length == 0) {
            Debug.Log("init pieces array");
            level.Pieces = new LevelPiece[level.TotalColumns * level.TotalRows];
        }
    }

    private void ResetResizeValues() {
        newTotalColumns = level.TotalColumns;
        newTotalRows = level.TotalRows;
    }

    private void InitMode() {
        List<Mode> modes = EditorUtils.GetListFromEnum<Mode>();
        modeLabels = GetEnumName(modes);
    }

    public static List<string> GetEnumName<T>(List<T> enums) {
        var result = new List<string>();
        foreach (T item in enums) {
            result.Add(item.ToString());
        }
        return result;
    }

    #endregion

    #region OnInspectorGUI
    public override void OnInspectorGUI() {
        DrawLevelDataGUI();
        DrawLevelSizeGUI();
        DrawPieceSelectedGUI();
        if (GUI.changed) {
            EditorUtility.SetDirty(level);
        }
    }

    private void DrawLevelDataGUI() {
        EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box")) {
            level.ShowGrid = EditorGUILayout.Toggle("显示网格", level.ShowGrid);
            level.ShowWalkArea = EditorGUILayout.Toggle("显示可行走区域", level.ShowWalkArea);
        }
    }

    private void DrawLevelSizeGUI() {
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope("box")) {
            using (new EditorGUILayout.VerticalScope()) {
                newTotalColumns = EditorGUILayout.IntField("Columns", Mathf.Max(1, newTotalColumns));
                newTotalRows = EditorGUILayout.IntField("Rows", Mathf.Max(1, newTotalRows));
            }

            using (new EditorGUILayout.VerticalScope()) {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = newTotalColumns != level.TotalColumns || newTotalRows != level.TotalRows;
                if (GUILayout.Button("Resize", GUILayout.Height(2 * EditorGUIUtility.singleLineHeight))) {
                    if (EditorUtility.DisplayDialog("Level Creator", "是否重设关卡长宽？", "是的", "取消")) {
                        ResizeLevel();
                    }
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Reset")) {
                    ResetResizeValues();
                }
                GUI.enabled = oldEnabled;
            }
        }
    }

    private void DrawPieceSelectedGUI() {
        EditorGUILayout.LabelField("Piece Selected", EditorStyles.boldLabel);
        if (pieceSelected == null) {
            EditorGUILayout.HelpBox("No piece selected!", MessageType.Info);
        } else {
            using (new EditorGUILayout.VerticalScope("box")) {
                EditorGUILayout.LabelField(new GUIContent(itemPreview), GUILayout.Height(40));
                EditorGUILayout.LabelField(itemSelected.itemName);
            }
        }
    }

    private void ResizeLevel() {
        LevelPiece[] newPieces = new LevelPiece[newTotalColumns * newTotalRows];
        for (int col = 0; col < level.TotalColumns; col++) {
            for (int row = 0; row < level.TotalRows; row++) {
                if (col < newTotalRows && row < newTotalRows) {
                    newPieces[col + row * newTotalColumns] = level.Pieces[col + row * level.TotalColumns];
                } else {
                    LevelPiece lp = level.Pieces[col + row * level.TotalColumns];
                    if (lp != null) {
                        DestroyImmediate(lp.gameObject);
                    }
                }
            }
        }
        level.Pieces = newPieces;
        level.TotalColumns = newTotalColumns;
        level.TotalRows = newTotalRows;
        //Save(level);
        SceneView.RepaintAll();
    }

    #endregion

    #region OnSceneGUI
    private void OnSceneGUI() {
        DrawModeGUI();
        ModeHandler();
        EventHandler();
        DrawAlphaGUI();
        //DrawPaletteItemCategoryGUI();
        DrawSaveAndLoadGUI();
        DrawShowGridGUI();
    }

    private void DrawModeGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10f, 10f, 500f, 40f));
        selectedMode = (Mode)GUILayout.Toolbar((int)currentMode, modeLabels.ToArray(), GUILayout.ExpandHeight(true));
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void ModeHandler() {
        switch (selectedMode) {
            case Mode.Paint:
            case Mode.Edit:
            case Mode.Erase:
                Tools.current = Tool.None;
                break;
            case Mode.View:
            default:
                Tools.current = Tool.View;
                break;
        }
        if (selectedMode != currentMode) {
            currentMode = selectedMode;
            Repaint();
        }
        level.ShowWalkArea = selectedMode == Mode.Erase;
        SceneView.currentDrawingSceneView.orthographic = true;
    }

    private void EventHandler() {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Camera camera = SceneView.currentDrawingSceneView.camera;
        Vector3 mousePosition = Event.current.mousePosition;
        //Vector3 beforeMousePosition = mousePosition;
        mousePosition = new Vector2(mousePosition.x, camera.pixelHeight - mousePosition.y);
        //Debug.LogFormat("pos1, pos2 : {0}, {1}", beforeMousePosition, mousePosition);
        Vector3 worldPos = camera.ScreenToWorldPoint(mousePosition);
        //Debug.Log("worldpos = " + worldPos);
        Vector3Int gridPos = level.WorldToGridCoordinates(worldPos);
        int x = gridPos.x;
        int z = gridPos.z;
        Debug.LogFormat("GridPos {0}, {1}", x, z);
    }

    private void EditWalkArea(int col, int row) {
        int buttonValue = Event.current.button;
        if (buttonValue == 1) {
            Event.current.Use();
        }
        if ((buttonValue == 0 || buttonValue == 1) && level.IsInsideGridBounds(col, row)) {
            level.WalkArea[col + row * level.TotalColumns] = 1 - buttonValue;
            SceneView.RepaintAll();
        }
    }

    private void DrawAlphaGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(Screen.safeArea.width - 400, 10, 480, 100));
        using (new EditorGUILayout.HorizontalScope("box")) {
            GUILayout.Label("全局alpha值", GUILayout.MaxWidth(150));
            float lastAlpha = alpha;
            alpha = GUILayout.HorizontalSlider(alpha, 0f, 1f);
            if (lastAlpha != alpha) {
                //RefreshSpritesAlpha();
            }
        }
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void DrawSaveAndLoadGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(Screen.safeArea.width - 150f, 160f, 100f, 100f));
        using (new EditorGUILayout.VerticalScope("box")) {
            if (GUILayout.Button("保存关卡", GUILayout.MaxHeight(40))) {
                //Save(level);
            }
            GUILayout.Space(20);
            if (GUILayout.Button("关闭", GUILayout.MaxHeight(40))) {
                //Selection.activeGameObject = GameObject.Find("LevelMainMenu");
                //DestroyImmediate(level.gameObject);
            }
        }
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void DrawShowGridGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(Screen.safeArea.width - 150f, 300f, 100f, 80f));
        using (new EditorGUILayout.VerticalScope("box")) {
            level.ShowGrid = GUILayout.Toggle(level.ShowGrid, "显示网格");
        }
        GUILayout.EndArea();
        Handles.EndGUI();
    }
    #endregion

    private string[] GetSpriteNames(SpriteRenderer[] spriteRenders, Level level) {
        string[] names = new string[level.TotalColumns * level.TotalRows];
        for (int i = 0; i < spriteRenders.Length; i++) {
            if (spriteRenders[i] != null) {
                names[i] = spriteRenders[i].sprite.name;
            }
        }
        return names;
    }

}
