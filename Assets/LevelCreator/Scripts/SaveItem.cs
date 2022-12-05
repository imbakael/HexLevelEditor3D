using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveItem {
    public int levelId;
    public int col;
    public int row;
    public int[] walkArea;
    public string[] paths;

    public static SaveItem GetDefaultSaveItem() {
        return new SaveItem {
            levelId = -1,
            col = 2,
            row = 2,
            walkArea = new int[2 * 2],
            paths = new string[2 * 2]
        };
    }

    public static SaveItem GetDefaultSaveItem(int col, int row) {
        return new SaveItem {
            levelId = -1,
            col = col,
            row = row,
            walkArea = new int[col * row],
            paths = new string[col * row]
        };
    }
}
