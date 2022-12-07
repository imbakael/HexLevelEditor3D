using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Level : MonoBehaviour {
    public const string DIRECTORY = "/SaveData/";
    public string fileName = "";

    public const float GRID_CELL_SIZE = 1.478016688f;

    [SerializeField] private LevelPiece[] pieces = default;
    public LevelPiece[] Pieces {
        get { return pieces; }
        set { pieces = value; }
    }

    public int TotalColumns { get; set; } = 15; // 列数，x方向
    public int TotalRows { get; set; } = 11; // 行数, y方向
    public int[] WalkArea { get; set; }

    public TileOffset[] Offsets { get; set; }

    public bool ShowGrid { get; set; } = true;
    public bool ShowWalkArea { get; set; } = false;

    private readonly Color normalColor = Color.white;
    private readonly Color selectedColor = Color.yellow;
    private readonly Color canWalkColor = new Color(0, 255, 255);

    #region DrawGridGizmos
    private void OnDrawGizmos() {
        if (ShowGrid) {
            var oldColor = Gizmos.color;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = normalColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            GridGizmo(TotalColumns, TotalRows);
            Gizmos.color = oldColor;
            Gizmos.matrix = oldMatrix;
        }

        if (ShowWalkArea && WalkArea != null && WalkArea.Length > 0) {
            var oldColor = Gizmos.color;
            for (int z = 0; z < TotalRows; z++) {
                for (int x = 0; x < TotalColumns; x++) {
                    int currentIndex = x + z * TotalColumns;
                    int value = WalkArea[currentIndex];
                    Vector3 pos = GridToWorldCoordinates(x, z);
                    Gizmos.color = value == 0 ? Color.red : canWalkColor;
                    Gizmos.DrawWireCube(pos, HexMetrics.outerRadius * Vector3.one);
                }
            }
            Gizmos.color = oldColor;
        }
    }

    private void OnDrawGizmosSelected() {
        var oldColor = Gizmos.color;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.color = selectedColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        GridBorderGizmo(TotalColumns, TotalRows);
        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

    private void GridGizmo(int col, int row) {
        for (int z = 0; z < row; z++) {
            for (int x = 0; x < col; x++) {
                HexCellGridGizmo(x, z);
            }
        }
    }

    // 绘制单个六边形的轮廓
    private void HexCellGridGizmo(int x, int z) {
        Vector3 centerPosition = GridToWorldCoordinates(x, z);
        for (int i = 0; i < 6; i++) {
            Gizmos.DrawLine(centerPosition + HexMetrics.corners[i], centerPosition + HexMetrics.corners[i + 1]);
        }
    }

    private void GridBorderGizmo(int col, int row) {
        // 下方
        for (int x = 0; x < col; x++) {
            Vector3 center = GridToWorldCoordinates(x, 0);
            Gizmos.DrawLine(center + HexMetrics.corners[2], center + HexMetrics.corners[3]);
            Gizmos.DrawLine(center + HexMetrics.corners[3], center + HexMetrics.corners[4]);
        }
        // 上方
        for (int x = 0; x < col; x++) {
            Vector3 center = GridToWorldCoordinates(x, row - 1);
            Gizmos.DrawLine(center + HexMetrics.corners[5], center + HexMetrics.corners[0]);
            Gizmos.DrawLine(center + HexMetrics.corners[0], center + HexMetrics.corners[1]);
        }
        // 左方
        for (int z = 0; z < row; z++) {
            Vector3 center = GridToWorldCoordinates(0, z);
            if (z % 2 == 0) {
                Gizmos.DrawLine(center + HexMetrics.corners[3], center + HexMetrics.corners[4]);
                Gizmos.DrawLine(center + HexMetrics.corners[5], center + HexMetrics.corners[0]);
            }
            Gizmos.DrawLine(center + HexMetrics.corners[4], center + HexMetrics.corners[5]);
        }
        // 右方
        for (int z = 0; z < row; z++) {
            Vector3 center = GridToWorldCoordinates(col - 1, z);
            if (z % 2 == 1) {
                Gizmos.DrawLine(center + HexMetrics.corners[0], center + HexMetrics.corners[1]);
                Gizmos.DrawLine(center + HexMetrics.corners[2], center + HexMetrics.corners[3]);
            }
            Gizmos.DrawLine(center + HexMetrics.corners[1], center + HexMetrics.corners[2]);
        }
    }

    #endregion

    public Vector3Int WorldToGridCoordinates(Vector3 worldPosition) {
        HexCoordinates coordinates = HexCoordinates.FromPosition(worldPosition);
        return new Vector3Int(coordinates.GridPosX, 0, coordinates.Z);
    }

    public Vector3 GridToWorldCoordinates(int x, int z) {
        return new Vector3(
            (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f),
            0f,
            z * (HexMetrics.outerRadius * 1.5f)
        );
    }

    public bool IsInsideGridBounds(Vector3 worldPosition) {
        Vector3Int gridPos = WorldToGridCoordinates(worldPosition);
        return IsInsideGridBounds(gridPos.x, gridPos.z);
    }

    public bool IsInsideGridBounds(int col, int row) => col >= 0 && col < TotalColumns && row >= 0 && row < TotalRows;

    public void Load(string fileName) {
        this.fileName = fileName;
        SaveItem saveItem = GetSaveItem(fileName);
        TotalColumns = saveItem.col;
        TotalRows = saveItem.row;
        WalkArea = saveItem.walkArea;
        Offsets = saveItem.offsets;
        pieces = GetPieces(saveItem.paths);
    }

    private SaveItem GetSaveItem(string fileName) {
        string path = Application.persistentDataPath + DIRECTORY + fileName;
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SaveItem>(json);
        }
        return SaveItem.GetDefaultSaveItem();
    }

    private LevelPiece[] GetPieces(string[] paths) {
        LevelPiece[] result = new LevelPiece[paths.Length];
        for (int z = 0; z < TotalRows; z++) {
            for (int x = 0; x < TotalColumns; x++) {
                int index = x + z * TotalColumns;
                if (!string.IsNullOrEmpty(paths[index])) {
                    GameObject prefab = Resources.Load(paths[index]) as GameObject;
                    GameObject obj = Instantiate(prefab);
                    obj.transform.parent = transform;
                    obj.name = string.Format("{0},{1}|{2}", x, z, prefab.name);
                    TileOffset offset = Offsets[index];
                    obj.transform.position = offset == null ? GridToWorldCoordinates(x, z) : (GridToWorldCoordinates(x, z) + new Vector3(offset.x, 0, offset.z));
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    result[index] = obj.GetComponent<LevelPiece>();
                }
            }
        }
        return result;
    }
}
