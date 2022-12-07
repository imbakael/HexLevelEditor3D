using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteItem : MonoBehaviour {

#if UNITY_EDITOR
    public enum Category {
        Plain,
        Forest,
        Mountain,
        Ocean
    }

    public Category category = Category.Plain;
    public string itemName = "";
    public Object inspectedScript;
#endif
}
