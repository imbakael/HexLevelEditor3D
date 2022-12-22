using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMap : MonoBehaviour {

    [SerializeField] private int columns = default;
    [SerializeField] private int rows = default;

    private int[] map;

    private void Start() {
        map = new int[columns * rows];
    }
}
