using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelPiece : MonoBehaviour {

    public string info;

    [ContextMenu("Show guid")]
    public void ShowGuid() {
        Debug.Log("name = " + gameObject.name + ", guid = " + AssetDatabase.AssetPathToGUID("Assets/Prefabs/LevelPieces/" + gameObject.name + ".prefab"));
    }
}
