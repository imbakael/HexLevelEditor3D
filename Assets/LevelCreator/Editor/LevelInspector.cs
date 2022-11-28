using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[CustomEditor(typeof(Level))]
public class LevelInspector : Editor {
    public enum Mode {
        View,
        Paint,
        Edit,
        EditWalkArea
    }

    private Mode currentMode;
    private Mode selectedMode;

    private List<string> modeLabels;
    private List<string> categoryLabels;

    private Level level;
    private int newTotalColumns;
    private int newTotalRows;

    private Texture2D itemPreview;
    private SpriteRenderer spriteRendererInspected;

    private float alpha = 1f;

    private int originalPosX;
    private int originalPosY;

    private void OnEnable() {
        if (EditorApplication.isPlaying) {
            return;
        }
        Debug.Log("level onEnable");
        level = target as Level;
        level.transform.hideFlags = HideFlags.NotEditable;
        ResetResizeValues();
    }


    #region Init
    private void ResetResizeValues() {
        newTotalColumns = level.TotalColumns;
        newTotalRows = level.TotalRows;
    }

    #endregion

    #region OnInspectorGUI
    public override void OnInspectorGUI() {
        DrawLevelSizeGUI();
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

    private void ResizeLevel() {
        level.TotalColumns = newTotalColumns;
        level.TotalRows = newTotalRows;
        //Save(level);
        SceneView.RepaintAll();
    }

    #endregion

    #region OnSceneGUI
    private void OnSceneGUI() {
        //DrawModeGUI();
        //ModeHandler();
        //DrawAlphaGUI();
        //DrawPaletteItemCategoryGUI();
        //DrawSaveAndLoadGUI();
        //DrawShowGridGUI();
    }

    private void DrawModeGUI() {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10f, 10f, 500f, 40f));
        selectedMode = (Mode)GUILayout.Toolbar((int)currentMode, modeLabels.ToArray(), GUILayout.ExpandHeight(true));
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void ModeHandler() {
        Tools.current = selectedMode switch {
            Mode.Paint or Mode.Edit or Mode.EditWalkArea => Tool.None,
            _ => Tool.View,
        };
        if (selectedMode != currentMode) {
            Repaint();
        }
        level.ShowWalkArea = selectedMode == Mode.EditWalkArea;
        SceneView.currentDrawingSceneView.in2DMode = true;
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
        GUILayout.BeginArea(new Rect(Screen.safeArea.width - 500, 10, 480, 100));
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
