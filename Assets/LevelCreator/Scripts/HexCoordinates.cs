using System;
using UnityEngine;

// 逻辑坐标，与网格坐标(也就是视图坐标)一一对应
[System.Serializable]
public struct HexCoordinates {
    [SerializeField]
    private int x, z;
    public int X => x;
    public int Z => z;
    public int Y => -x - z;

    public int GridPosX => x + z / 2;

    public HexCoordinates(int x, int z) {
        this.x = x;
        this.z = z;
    }

    // 视图坐标转逻辑坐标
    public static HexCoordinates FromOffsetCoordinates(int gridX, int girdZ) {
        return new HexCoordinates(gridX - girdZ / 2, girdZ);
    }

    public int GetIndex(int width) {
        return x + z * width + z / 2;
    }

    public static int GetIndex(int gridX, int gridZ, int width) {
        int x = gridX - gridZ / 2;
        int z = gridZ;
        return x + z * width + z / 2;
    }

    /// <summary>
    /// 求任意position对应的逻辑坐标
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static HexCoordinates FromPosition(Vector3 position) {
        // 坐标规律1：当z == 0 时，也就是一维时 y = -x
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;
        // 坐标规律2：每提升两行高度（HexMetrics.outerRadius * 3f），x - 1且y - 1
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);
        // 找到误差最大的，误差最大的那个应该由剩余两个数来计算，如iX = -iY - iZ
        if (iX + iY + iZ != 0) {
            float deltaX = Mathf.Abs(x - iX);
            float deltaY = Mathf.Abs(y - iY);
            float deltaZ = Mathf.Abs(-x - y - iZ);
            if (deltaX > deltaY && deltaX > deltaZ) {
                iX = -iY - iZ;
            } else if (deltaZ > deltaY) {
                iZ = -iX - iY;
            }
        }
        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other) {
        return
            ((x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z)) / 2;
    }
}

