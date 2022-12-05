using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;
using Newtonsoft.Json;

[CustomEditor(typeof(Level))]
public class LevelInspector : Editor {
    public enum Mode {
        View,
        Paint,
        Edit,
        Erase,
        WalkArea // 可行走区域
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
    private PaletteItem itemInspected;

    private int originalPosX;
    private int originalPosZ;

    private float alpha = 1f;

    private GUIStyle titleStyle;

    private void OnEnable() {
        Debug.Log("level onEnable");
        level = target as Level;
        level.transform.hideFlags = HideFlags.NotEditable;
        InitLevel();
        InitWalkArea();
        ResetResizeValues();
        InitMode();
        InitStyles();
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

    private void InitLevel() {
        if (level.Pieces == null || level.Pieces.Length == 0) {
            level.Pieces = new LevelPiece[level.TotalColumns * level.TotalRows];
        }
    }

    private void InitWalkArea() {
        if (level.WalkArea == null || level.WalkArea.Length == 0) {
            level.WalkArea = new int[level.TotalColumns * level.TotalRows];
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

    private void InitStyles() {
        GUISkin skin = (GUISkin)Resources.Load("LevelCreatorSkin");
        titleStyle = skin.label;
    }

    public static List<string> GetEnumName<T>(List<T> enums) {
        var result = new List<string>();
        foreach (T item in enums) {
            result.Add(item.ToString());
        }
        return result;
    }

    #region OnInspectorGUI
    public override void OnInspectorGUI() {
        DrawLevelDataGUI();
        DrawLevelSizeGUI();
        DrawPieceSelectedGUI();
        DrawInspectedItemGUI();
        if (GUI.changed) {
            EditorUtility.SetDirty(level);
        }
    }

    private void DrawLevelDataGUI() {
        EditorGUILayout.LabelField("数据", titleStyle);
        using (new EditorGUILayout.VerticalScope("box")) {
            
        }
    }

    private void DrawLevelSizeGUI() {
        EditorGUILayout.LabelField("地图大小", titleStyle);
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
        EditorGUILayout.LabelField("选中瓦片", titleStyle);
        if (pieceSelected == null) {
            EditorGUILayout.HelpBox("No piece selected!", MessageType.Info);
        } else {
            using (new EditorGUILayout.VerticalScope("box")) {
                EditorGUILayout.LabelField(new GUIContent(itemPreview), GUILayout.Height(40));
                EditorGUILayout.LabelField(itemSelected.itemName);
            }
        }
    }

    private void DrawInspectedItemGUI() {
        if (currentMode != Mode.Edit) {
            return;
        }
        EditorGUILayout.LabelField("编辑瓦片", titleStyle);
        if (itemInspected != null) {
            using (new EditorGUILayout.VerticalScope("box")) {
                EditorGUILayout.LabelField("Name:" + itemInspected.name);
                CreateEditor(itemInspected.inspectedScript).OnInspectorGUI();
            }
        } else {
            EditorGUILayout.HelpBox("No piece to Edit!", MessageType.Info);
        }
    }

    private void ResizeLevel() {
        LevelPiece[] newPieces = new LevelPiece[newTotalColumns * newTotalRows];
        for (int z = 0; z < level.TotalRows; z++) {
            for (int x = 0; x < level.TotalColumns; x++) {
                if (x < newTotalColumns && z < newTotalRows) {
                    newPieces[x + z * newTotalColumns] = level.Pieces[x + z * level.TotalColumns];
                } else {
                    LevelPiece lp = level.Pieces[x + z * level.TotalColumns];
                    if (lp != null) {
                        DestroyImmediate(lp.gameObject);
                    }
                }
            }
        }
        level.Pieces = newPieces;
        int[] newWalkArea = new int[newTotalColumns * newTotalRows];
        for (int z = 0; z < level.TotalRows; z++) {
            for (int x = 0; x < level.TotalColumns; x++) {
                int currentIndex = x + z * level.TotalColumns;
                if (x < newTotalColumns && z < newTotalRows) {
                    int newIndex = x + z * newTotalColumns;
                    newWalkArea[newIndex] = level.WalkArea[currentIndex];
                }
            }
        }
        level.WalkArea = newWalkArea;
        level.TotalColumns = newTotalColumns;
        level.TotalRows = newTotalRows;
        Save(level);
        SceneView.RepaintAll();
    }

    #endregion

    #region OnSceneGUI
    private void OnSceneGUI() {
        DrawModeGUI();
        ModeHandler();
        EventHandler();
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
            case Mode.WalkArea:
                Tools.current = Tool.None;
                break;
            case Mode.View:
            default:
                Tools.current = Tool.View;
                break;
        }
        if (selectedMode != currentMode) {
            currentMode = selectedMode;
            itemInspected = null;
            Repaint();
        }
        level.ShowWalkArea = selectedMode == Mode.WalkArea;
        SceneView.currentDrawingSceneView.orthographic = true;
        SceneView.currentDrawingSceneView.rotation = Quaternion.Euler(90, 0, 0);
        SceneView.currentDrawingSceneView.isRotationLocked = true;
        //Debug.Log(SceneView.currentDrawingSceneView.rotation);
    }

    private void EventHandler() {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Camera camera = SceneView.currentDrawingSceneView.camera;
        Vector3 mousePosition = Event.current.mousePosition;
        mousePosition = new Vector2(mousePosition.x, camera.pixelHeight - mousePosition.y);
        Vector3 worldPos = camera.ScreenToWorldPoint(mousePosition);
        Vector3Int gridPos = level.WorldToGridCoordinates(worldPos);
        int x = gridPos.x;
        int z = gridPos.z;
        //Debug.LogFormat("GridPos {0}, {1}", x, z);
        switch (currentMode) {
            case Mode.View:
                break;
            case Mode.Paint:
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) {
                    Paint(x, z);
                }
                break;
            case Mode.Edit:
                if (Event.current.type == EventType.MouseDown) {
                    Edit(x, z);
                    originalPosX = x;
                    originalPosZ = z;
                }
                if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Ignore) {
                    if (itemInspected != null) {
                        Move();
                    }
                }
                if (itemInspected != null) {
                    itemInspected.transform.position = Handles.FreeMoveHandle(
                        itemInspected.transform.position,
                        itemInspected.transform.rotation,
                        Level.GRID_CELL_SIZE / 2,
                        Level.GRID_CELL_SIZE / 2 * Vector3.one,
                        Handles.RectangleHandleCap
                    );
                }
                break;
            case Mode.Erase:
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) {
                    Erase(x, z);
                }
                break;
            case Mode.WalkArea:
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) {
                    EditWalkArea(x, z);
                }
                break;
            default:
                break;
        }
    }

    private void Paint(int x, int z) {
        //Debug.LogFormat("Painting {0},{1}", x, z);
        if (pieceSelected == null || !level.IsInsideGridBounds(x, z)) {
            return;
        }
        if (level.Pieces[x + z * level.TotalColumns] != null) {
            DestroyImmediate(level.Pieces[x + z * level.TotalColumns].gameObject);
        }
        GameObject obj = PrefabUtility.InstantiatePrefab(pieceSelected.gameObject) as GameObject;
        obj.transform.parent = level.transform;
        obj.name = string.Format("{0},{1}|{2}", x, z, obj.name);
        obj.transform.position = level.GridToWorldCoordinates(x, z);
        obj.hideFlags = HideFlags.HideInHierarchy;
        level.Pieces[x + z * level.TotalColumns] = obj.GetComponent<LevelPiece>();
    }

    private void Erase(int x, int z) {
        //Debug.LogFormat("Erasing {0},{1}", x, z);
        if (!level.IsInsideGridBounds(x, z)) {
            return;
        }
        if (level.Pieces[x + z * level.TotalColumns] != null) {
            DestroyImmediate(level.Pieces[x + z * level.TotalColumns].gameObject);
        }
    }

    private void Edit(int x, int z) {
        //Debug.LogFormat("Editing {0},{1}", x, z);
        if (!level.IsInsideGridBounds(x, z) || level.Pieces[x + z * level.TotalColumns] == null) {
            itemInspected = null;
        } else {
            itemInspected = level.Pieces[x + z * level.TotalColumns].GetComponent<PaletteItem>();
        }
        Repaint();
    }

    private void Move() {
        Vector3Int gridPoint = level.WorldToGridCoordinates(itemInspected.transform.position);
        int x = gridPoint.x;
        int z = gridPoint.z;
        if (x == originalPosX && z == originalPosZ) {
            return;
        }
        if (!level.IsInsideGridBounds(x, z) || level.Pieces[x + z * level.TotalColumns] != null) {
            itemInspected.transform.position = level.GridToWorldCoordinates(originalPosX, originalPosZ);
        } else {
            level.Pieces[originalPosX + originalPosZ * level.TotalColumns] = null;
            level.Pieces[x + z * level.TotalColumns] = itemInspected.GetComponent<LevelPiece>();
            level.Pieces[x + z * level.TotalColumns].transform.position = level.GridToWorldCoordinates(x, z);
        }
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

    private void DrawSaveAndLoadGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(Screen.safeArea.width - 150f, 160f, 100f, 100f));
        using (new EditorGUILayout.VerticalScope("box")) {
            if (GUILayout.Button("保存关卡", GUILayout.MaxHeight(40))) {
                Save(level);
            }
            GUILayout.Space(20);
            if (GUILayout.Button("关闭", GUILayout.MaxHeight(40))) {
                Selection.activeGameObject = GameObject.Find("LevelMainMenu");
                DestroyImmediate(level.gameObject);
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

    private void Save(Level level) {
        Debug.Log("Save!, fileName = " + level.fileName);
        string dir = Application.persistentDataPath + Level.DIRECTORY;
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
        var saveItem = new SaveItem {
            levelId = 1,
            col = level.TotalColumns,
            row = level.TotalRows,
            walkArea = level.WalkArea,
            paths = GetPaths(level)
        };
        string json = JsonConvert.SerializeObject(saveItem);
        File.WriteAllText(dir + level.fileName, json);
    }

    private string[] GetPaths(Level level) {
        string[] paths = new string[level.Pieces.Length];
        for (int z = 0; z < level.TotalRows; z++) {
            for (int x = 0; x < level.TotalColumns; x++) {
                int index = x + z * level.TotalColumns;
                if (level.Pieces[index] != null) {
                    paths[index] = "LevelPieces/" + level.Pieces[index].name.Split('|')[1];
                }
            }
        }
        return paths;
    }
}
