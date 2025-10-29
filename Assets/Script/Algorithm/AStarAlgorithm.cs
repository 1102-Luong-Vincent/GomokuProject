using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiAStarPaths
{
    public (int, int) TargetPoint;
    public List<Vector3> MainPath;
    public List<List<Vector3>> SurroundPaths;
}

public class AStarAlgorithm : MonoBehaviour
{
    public static AStarAlgorithm Instance;

    [SerializeField] private Collider BoardCollider;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private int gridCountX = 15;
    [SerializeField] private int gridCountY = 15;

    private Node[,] grid;
    private float cellSize;
    private Vector3 origin;

    private List<Vector3> lastPath = null; // for visualization

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait until DeskControl is initialized and grid cells exist
        yield return new WaitUntil(() => DeskControl.Instance != null && DeskControl.Instance.GetGridCell(0, 0) != null);

        gridCountX = DeskConstants.ConstrainCount;
        gridCountY = DeskConstants.ConstrainCount;

        BuildGrid();
    }

    private class Node
    {
        public Vector3 worldPos;
        public bool walkable;
        public Node parent;
        public float gCost, hCost;
        public int x, y;
        public float fCost => gCost + hCost;

        public Node(Vector3 pos, bool walkable, int x, int y)
        {
            worldPos = pos;
            this.walkable = walkable;
            this.x = x;
            this.y = y;
            gCost = float.MaxValue;
            parent = null;
        }
    }

    public void BuildGrid()
    {
        if (BoardCollider == null)
        {
            Debug.LogError("[A*] BoardCollider not assigned!");
            return;
        }

        if (DeskControl.Instance == null)
        {
            Debug.LogError("[A*] DeskControl.Instance is null!");
            return;
        }

        grid = new Node[gridCountX, gridCountY];

        Vector3 firstCellPos = DeskControl.Instance.GetGridCell(0, 0).transform.position;
        float boardY = firstCellPos.y;

        // Compute cell size (distance between first two cells)
        float computedCellSize = 0.0f;
        var cell01 = DeskControl.Instance.GetGridCell(1, 0);
        if (cell01 != null)
        {
            computedCellSize = Vector3.Distance(firstCellPos, cell01.transform.position);
        }
        else
        {
            Bounds b = BoardCollider.bounds;
            computedCellSize = Mathf.Min(b.size.x / gridCountX, b.size.z / gridCountY);
        }

        cellSize = computedCellSize;
        origin = new Vector3(firstCellPos.x, boardY, firstCellPos.z);

        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                GameObject cellObj = DeskControl.Instance.GetGridCell(x, y);

                Vector3 worldPos;
                if (cellObj != null)
                    worldPos = cellObj.transform.position;
                else
                    worldPos = origin + new Vector3(x * cellSize, 0f, y * cellSize);

                worldPos.y = boardY;

                bool walkable = !Physics.CheckBox(worldPos, Vector3.one * (cellSize * 0.4f), Quaternion.identity, obstacleMask);
                grid[x, y] = new Node(worldPos, walkable, x, y);
            }
        }

        Debug.Log($"[A*] Grid built successfully: {gridCountX}x{gridCountY}, cellSize={cellSize:F3}");
    }

    public MultiAStarPaths GetPathsToTarget((int, int) targetPoint, int surroundCount = 4)
    {
        MultiAStarPaths result = new MultiAStarPaths
        {
            TargetPoint = targetPoint,
            SurroundPaths = new List<List<Vector3>>()
        };

        if (grid == null)
            BuildGrid();

        Vector3 targetWorldPos = DeskControl.Instance.GetGridCell(targetPoint.Item1, targetPoint.Item2).transform.position;
        Node targetNode = GetNearestNode(targetWorldPos);
        targetNode.walkable = true;

        List<Node> outerNodes = GetOuterEdgeNodes();
        if (outerNodes.Count == 0) return result;

        Node bestStart = null;
        float bestDist = float.MaxValue;

        foreach (var start in outerNodes)
        {
            var path = FindPath(start, targetNode);
            if (path.Count == 0) continue;
            float dist = PathLength(path);
            if (dist < bestDist)
            {
                bestDist = dist;
                result.MainPath = path;
                bestStart = start;
            }
        }

        // Save path for visualization
        lastPath = result.MainPath;

        var randomOuter = outerNodes.OrderBy(x => Random.value).Where(n => n != bestStart).Take(surroundCount);
        foreach (var s in randomOuter)
        {
            var path = FindPath(s, targetNode);
            if (path.Count > 0)
                result.SurroundPaths.Add(path);
        }

        Debug.Log($"[A*] Target {targetPoint} -> MainPath {result.MainPath?.Count} nodes, SurroundPaths: {result.SurroundPaths.Count}");
        return result;
    }

    private List<Node> GetNeighbors(Node n)
    {
        List<Node> neighbors = new List<Node>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = n.x + dx[i];
            int ny = n.y + dy[i];
            if (nx >= 0 && nx < gridCountX && ny >= 0 && ny < gridCountY)
                neighbors.Add(grid[nx, ny]);
        }
        return neighbors;
    }

    private List<Vector3> FindPath(Node start, Node end)
    {
        foreach (var n in grid)
        {
            n.gCost = float.MaxValue;
            n.parent = null;
        }

        start.gCost = 0;
        start.hCost = Vector3.Distance(start.worldPos, end.worldPos);

        List<Node> open = new List<Node> { start };
        HashSet<Node> closed = new HashSet<Node>();

        while (open.Count > 0)
        {
            Node current = open.OrderBy(n => n.fCost).First();
            if (current == end) return RetracePath(start, end);

            open.Remove(current);
            closed.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!neighbor.walkable || closed.Contains(neighbor)) continue;

                float newCost = current.gCost + Vector3.Distance(current.worldPos, neighbor.worldPos);
                if (newCost < neighbor.gCost)
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = Vector3.Distance(neighbor.worldPos, end.worldPos);
                    neighbor.parent = current;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
        }

        return new List<Vector3>();
    }

    private List<Vector3> RetracePath(Node start, Node end)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = end;
        while (current != null)
        {
            path.Add(current.worldPos);
            if (current == start) break;
            current = current.parent;
        }
        Debug.Log($"[A*] Retracing path with {path.Count} nodes. Start={start.worldPos}, End={end.worldPos}");
        //path.Reverse();
        return path;
    }

    private List<Node> GetOuterEdgeNodes()
    {
        List<Node> edges = new List<Node>();
        int maxX = gridCountX - 1;
        int maxY = gridCountY - 1;

        for (int x = 0; x < gridCountX; x++)
        {
            edges.Add(grid[x, 0]);
            edges.Add(grid[x, maxY]);
        }
        for (int y = 1; y < gridCountY - 1; y++)
        {
            edges.Add(grid[0, y]);
            edges.Add(grid[maxX, y]);
        }

        return edges.Where(n => n.walkable).ToList();
    }

    private float PathLength(List<Vector3> path)
    {
        float len = 0f;
        for (int i = 1; i < path.Count; i++)
            len += Vector3.Distance(path[i - 1], path[i]);
        return len;
    }

    private Node GetNearestNode(Vector3 pos)
    {
        if (grid == null)
        {
            BuildGrid();
            if (grid == null) return null;
        }

        Node best = null;
        float bestDist = float.MaxValue;

        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                Node n = grid[x, y];
                if (n == null) continue;

                float d = Vector3.SqrMagnitude(n.worldPos - pos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
        }

        if (best == null)
            Debug.LogError("[A*] GetNearestNode: no nodes found!");

        return best;
    }

    private void OnDrawGizmos()
    {
        if (grid != null)
        {
            Gizmos.color = Color.gray;
            foreach (var n in grid)
            {
                if (n == null) continue;
                Gizmos.DrawWireCube(n.worldPos, Vector3.one * (cellSize * 0.4f));
                if (!n.walkable)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(n.worldPos, Vector3.one * (cellSize * 0.25f));
                    Gizmos.color = Color.gray;
                }
            }
        }

        if (lastPath != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < lastPath.Count; i++)
                Gizmos.DrawSphere(lastPath[i], cellSize * 0.15f);
        }
    }
}
