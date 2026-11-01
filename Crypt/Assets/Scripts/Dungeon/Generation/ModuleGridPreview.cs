using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//Draws the module grid and cell coordinates while editing a module.
public class ModuleGridPreview : MonoBehaviour
{
    [Header("Grid Size")]

    //This must match roomSpacing in DungeonGenerator.
    [SerializeField] private Vector3 cellSize = new Vector3(12f, 4f, 12f);

    //Only moves the preview. It does not affect module placement.
    [SerializeField] private Vector3 gridOffset = new Vector3(0f, 2f, 0f);

    //These control which coordinates are shown.
    [SerializeField] private Vector3Int minimumCell = new Vector3Int(-1, 0, -1);
    [SerializeField] private Vector3Int maximumCell = new Vector3Int(1, 5, 1);

    [Header("Display")]

    [SerializeField] private bool showEmptyCells = true;
    [SerializeField] private bool showCoordinates = true;

    [SerializeField] private Color emptyCellColor = new Color(1f, 1f, 1f, 0.25f);

    [SerializeField] private Color occupiedCellColor = new Color(0f, 1f, 0f, 0.8f);

    [SerializeField] private Color originCellColor = new Color(1f, 0.8f, 0f, 1f);



    private RoomModule roomModule;

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        roomModule = GetComponent<RoomModule>();

        HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

        if (roomModule != null)
        {
            foreach (Vector3Int cell in roomModule.OccupiedCells)
            {
                occupiedCells.Add(cell);
            }
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;

        //This makes the grid follow the module's position and rotation.
        Gizmos.matrix = transform.localToWorldMatrix;

        for (int y = minimumCell.y; y <= maximumCell.y; y++)
        {
            for (int z = minimumCell.z; z <= maximumCell.z; z++)
            {
                for (int x = minimumCell.x; x <= maximumCell.x; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);
                    bool isOccupied = occupiedCells.Contains(cell);
                    bool isOrigin = cell == Vector3Int.zero;

                    if (!showEmptyCells && !isOccupied && !isOrigin)
                    {
                        continue;
                    }

                    Vector3 localPosition = Vector3.Scale((Vector3)cell, cellSize) + gridOffset;

                    if (isOrigin)
                    {
                        Gizmos.color = originCellColor;
                    }
                    else if (isOccupied)
                    {
                        Gizmos.color = occupiedCellColor;
                    }
                    else
                    {
                        Gizmos.color = emptyCellColor;
                    }

                    //Make the boxes slightly smaller so individual cells are visible.
                    Gizmos.DrawWireCube(localPosition, cellSize * 0.96f);

                    if (showCoordinates)
                    {
                        DrawCoordinate(cell, localPosition, isOccupied, isOrigin);
                    }
                }
            }
        }

        Gizmos.matrix = previousMatrix;
    }

    private void DrawCoordinate(Vector3Int cell, Vector3 localPosition, bool isOccupied, bool isOrigin)
    {
        Vector3 worldPosition = transform.TransformPoint(localPosition);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 12;

        if (isOrigin)
        {
            labelStyle.normal.textColor = originCellColor;
        }
        else if (isOccupied)
        {
            labelStyle.normal.textColor = occupiedCellColor;
        }
        else
        {
            labelStyle.normal.textColor = Color.white;
        }

        Handles.Label(worldPosition, $"({cell.x}, {cell.y}, {cell.z})", labelStyle);
    }

#endif
}