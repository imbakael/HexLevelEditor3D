using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Level : MonoBehaviour {
    public const string DIRECTORY = "/SaveData/";
    public const float GRID_CELL_SIZE = 1f;

    public Transform[] Layers { get; set; }
    public int TotalColumns { get; set; } = 15; // 列数，x方向
    public int TotalRows { get; set; } = 11; // 行数, y方向

    public bool ShowGrid { get; set; } = true;
    public int[] WalkArea { get; set; }
    public bool ShowWalkArea { get; set; } = false;

    public string fileName = "";
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

        //if (ShowWalkArea && WalkArea != null && WalkArea.Length > 0) {
        //    var oldColor = Gizmos.color;
        //    for (int i = 0; i < TotalColumns; i++) {
        //        for (int j = 0; j < TotalRows; j++) {
        //            int currentIndex = i + j * TotalColumns;
        //            int value = WalkArea[currentIndex];
        //            Vector3 pos = GridToWorldCoordinates(i, j);
        //            Gizmos.color = value == 0 ? Color.red : canWalkColor;
        //            Gizmos.DrawWireCube(pos, 0.5f * GRID_CELL_SIZE * Vector3.one);
        //        }
        //    }
        //    Gizmos.color = oldColor;
        //}
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

    private void GridTileBorderGizmo(int col, int row) {
        var leftDown = new Vector3(col * GRID_CELL_SIZE, row * GRID_CELL_SIZE, 0);
        var leftUp = new Vector3(col * GRID_CELL_SIZE, (row + 1) * GRID_CELL_SIZE, 0);
        var rightDown = new Vector3((col + 1) * GRID_CELL_SIZE, row * GRID_CELL_SIZE, 0);
        var rightUp = new Vector3((col + 1) * GRID_CELL_SIZE, (row + 1) * GRID_CELL_SIZE, 0);
        Gizmos.DrawLine(leftDown, leftUp);
        Gizmos.DrawLine(leftDown, rightDown);
        Gizmos.DrawLine(rightUp, leftUp);
        Gizmos.DrawLine(rightUp, rightDown);
    }

    #endregion

    public Vector3Int WorldToGridCoordinates(Vector3 point) {
        HexCoordinates coordinates = HexCoordinates.FromPosition(point);
        return new Vector3Int(coordinates.GridPosX, 0, coordinates.Z);
    }

    public Vector3 GridToWorldCoordinates(int x, int z) {
        return new Vector3(
            (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f),
            0f,
            z * (HexMetrics.outerRadius * 1.5f)
        );
    }

    public bool IsInsideGridBounds(Vector3 point) {
        Vector3Int gridPos = WorldToGridCoordinates(point);
        return IsInsideGridBounds(gridPos.x, gridPos.z);
    }

    public bool IsInsideGridBounds(int col, int row) => col >= 0 && col < TotalColumns && row >= 0 && row < TotalRows;

}
