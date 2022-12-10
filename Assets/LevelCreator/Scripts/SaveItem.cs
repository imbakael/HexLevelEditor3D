using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveItem {
    public int levelId;
    public int col;
    public int row;
    public int[] walkArea;
    public string[] paths;
    public TileOffset[] offsets;

    public static SaveItem GetDefaultSaveItem(int col = 2, int row = 2) {
        return new SaveItem {
            levelId = -1,
            col = col,
            row = row,
            walkArea = new int[col * row],
            paths = new string[col * row],
            offsets = new TileOffset[col * row]
        };
    }
}
