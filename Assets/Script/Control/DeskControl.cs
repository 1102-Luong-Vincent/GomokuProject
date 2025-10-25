using UnityEngine;
using UnityEngine.UI;

public class DeskConstants
{
    public const float CellSize = 0.02f;
    public const int ConstrainCount = 7;

}

public class DeskControl : MonoBehaviour
{

    [SerializeField] Transform DeckGridDetection;
    [SerializeField] Transform startPoint;
    [SerializeField] Transform endPoint;
    [SerializeField] GameObject DeckGridDetectionPrefab;

    private GameObject[,] gridCells;


    void Start()
    {
        InitDesk();
    }

    void InitDesk()
    {
        int gridCount = DeskConstants.ConstrainCount;
        float cellSize = DeskConstants.CellSize;
        float spacing = CalSpacing();

        gridCells = new GameObject[gridCount, gridCount];

        Vector3 startPos = startPoint.localPosition;
        for (int y = 0; y < gridCount; y++)
        {
            for (int x = 0; x < gridCount; x++)
            {
                Vector3 pos = startPos + new Vector3(x * spacing, -y * spacing, 0);
                GameObject cell = Instantiate(DeckGridDetectionPrefab, DeckGridDetection);
                cell.transform.localPosition = pos;
                cell.transform.localRotation = Quaternion.identity;
                cell.name = $"Cell_{x}_{y}";
                gridCells[x, y] = cell;

                if (cell.GetComponent<BoxCollider>() == null)
                {
                    var col = cell.AddComponent<BoxCollider>();

                    col.center = Vector3.zero;
                }
            }
        }
    }

    public bool IsAtGridCells(GameObject obj, out int xIndex, out int yIndex)
    {
        xIndex = -1;
        yIndex = -1;

        if (obj == null || gridCells == null) return false;

        int width = gridCells.GetLength(0);
        int height = gridCells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridCells[x, y] == obj)
                {
                    xIndex = x;
                    yIndex = y;
                    return true;
                }
            }
        }

        return false;
    }



    public GameObject GetGridCell(int x, int y)
    {
        if (gridCells == null) return null;
        if (x < 0 || x >= gridCells.GetLength(0)) return null;
        if (y < 0 || y >= gridCells.GetLength(1)) return null;

        return gridCells[x, y];
    }


    float CalSpacing()
    {
        float totalDistance = endPoint.position.x - startPoint.position.x;
        float spacing = totalDistance / (DeskConstants.ConstrainCount - 1);
        return spacing;
    }

}