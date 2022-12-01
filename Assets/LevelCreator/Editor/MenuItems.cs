using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class MenuItems {

    [MenuItem("Tools/Level Creator/Show Palette #_p")]
    private static void ShowPallette() {
        PaletteWindow.ShowPalette();
    }

}
