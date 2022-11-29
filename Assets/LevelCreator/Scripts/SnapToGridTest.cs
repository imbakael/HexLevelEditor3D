using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SnapToGridTest : MonoBehaviour {

    [SerializeField] private Level level = default;

    private void Update() {
        var gridCoord = level.WorldToGridCoordinates(transform.position);
        transform.position = level.GridToWorldCoordinates(gridCoord.x, gridCoord.z);
    }

    private void OnDrawGizmos() {
        Color oldColor = Gizmos.color;
        Gizmos.color = level.IsInsideGridBounds(transform.position) ? Color.green : Color.red;
        Gizmos.DrawCube(transform.position, Vector3.one);
        Gizmos.color = oldColor;
    }
}
