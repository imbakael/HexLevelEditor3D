using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteItem : MonoBehaviour {

#if UNITY_EDITOR
    public enum Category {
        Grass,
        Stone
    }

    public Category category = Category.Grass;
    public string itemName = "";
    public Object inspectedScript;
#endif
}
