using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public static class EditorUtils {

    public static List<T> GetAssetsWithScript<T> (string path) where T : MonoBehaviour {
        var assetList = new List<T>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
        for (int i = 0; i < guids.Length; i++) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
            T t = asset.GetComponent<T>();
            if (t != null) {
                assetList.Add(t);
            }
        }
        return assetList;
    }

    public static List<T> GetListFromEnum<T>() {
        var result = new List<T>();
        Array enums = Enum.GetValues(typeof(T));
        foreach (T e in enums) {
            result.Add(e);
        }
        return result;
    }
}