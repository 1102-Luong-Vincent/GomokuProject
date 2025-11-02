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
    [SerializeField] private int gridCountX = 9;
    [SerializeField] private int gridCountY = 9;

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
        // Wait until DeskControl is ready
        yield return new WaitUntil(() => DeskControl.Instance != null && DeskControl.Instance.GetGridCell(0, 0) != null);

        // use DeskConstants if available (your project)
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

    /// <summary>
    /// Build grid by sampling DeskControl's actual grid cells (positions). This avoids arithmetic/orientation mismatches.
    /// </summary>
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

        // Get origin from (0,0) cell (top-left or whichever your desk defines)
        var cell00 = DeskControl.Instance.GetGridCell(0, 0);
        if (cell00 == null)
        {
            Debug.LogError("[A*] DeskControl GetGridCell(0,0) returned null!");
            return;
        }
        origin = cell00.transform.position;

        // Try to compute cellSize using neighbor cell (1,0). Fallback to collider size if neighbor missing
        var cell10 = DeskControl.Instance.GetGridCell(1, 0);
        if (cell10 != null)
            cellSize = Vector3.Distance(cell00.transform.position, cell10.transform.position);
        else
        {
            Bounds b = BoardCollider.bounds;
            cellSize = Mathf.Min(b.size.x / gridCountX, b.size.z / gridCountY);
        }

        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                GameObject cellObj = DeskControl.Instance.GetGridCell(x, y);
                Vector3 worldPos;
                if (cellObj != null)
                {
                    worldPos = cellObj.transform.position;
                }
                else
                {
                    // fallback spatial placement (shouldn't be needed if DeskControl provides cells)
                    worldPos = origin + Vector3.right * (x * cellSize) - Vector3.forward * (y * cellSize);
                }

                // enforce board Y from (0,0) cell to avoid Y drift
                worldPos.y = origin.y;

                bool walkable = !Physics.CheckBox(worldPos, Vector3.one * (cellSize * 0.4f), Quaternion.identity, obstacleMask);
                grid[x, y] = new Node(worldPos, walkable, x, y);
            }
        }

        Debug.Log($"[A*] Grid built: {gridCountX}x{gridCountY}, cellSize={cellSize:F3}");
    }

    public MultiAStarPaths GetPathsToTarget((int, int) targetPoint, int surroundCount = 4)
    {
        MultiAStarPaths result = new MultiAStarPaths
        {
            TargetPoint = targetPoint,
            SurroundPaths = new List<List<Vector3>>()
        };

        if (grid == null) BuildGrid();

        // target world
        Vector3 targetWorldPos = DeskControl.Instance.GetGridCell(targetPoint.Item1, targetPoint.Item2).transform.position;
        Node targetNode = GetNearestNode(targetWorldPos);
        if (targetNode == null)
        {
            Debug.LogWarning("[A*] GetNearestNode returned null for target!");
            return result;
        }
        targetNode.walkable = true;

        List<Node> outerNodes = GetOuterEdgeNodes();
        if (outerNodes.Count == 0)
        {
            Debug.LogWarning("[A*] No outer nodes found!");
            return result;
        }

        Node bestStart = null;
        float bestDist = float.MaxValue;

        foreach (var start in outerNodes)
        {
            var path = FindPathWaypoints(start, targetNode);
            if (path == null || path.Count == 0) continue;
            float dist = PathLength(path);
            if (dist < bestDist)
            {
                bestDist = dist;
                result.MainPath = path;
                bestStart = start;
            }
        }

        if (result.MainPath == null || result.MainPath.Count == 0)
        {
            Debug.LogWarning("[A*] Could not find a main path to target");
            return result;
        }

        lastPath = result.MainPath;

        var availableNodes = outerNodes.Where(n => n != bestStart).ToList();
        var randomNodes = availableNodes.OrderBy(x => Random.value).Take(surroundCount).ToList();
        foreach (var startNode in randomNodes)
        {
            var path = FindPathWaypoints(startNode, targetNode);
            if (path != null && path.Count > 0)
                result.SurroundPaths.Add(path);
        }

        Debug.Log($"[A*] Target {targetPoint} -> MainPath {result.MainPath.Count} waypoints, SurroundPaths: {result.SurroundPaths.Count}");
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

    /// <summary>
    /// Run A* and then compress the full grid path into necessary waypoints (only when direction changes).
    /// </summary>
    private List<Vector3> FindPathWaypoints(Node start, Node end)
    {
        if (start == null || end == null) return new List<Vector3>();

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

            if (current == end)
            {
                var fullPath = RetracePath(start, end);
                return SimplifyToWaypoints(fullPath);
            }

            open.Remove(current);
            closed.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (neighbor == null || !neighbor.walkable || closed.Contains(neighbor)) continue;

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
        path.Reverse();
        return path;
    }

    /// <summary>
    /// From full sequence of node positions, return only the waypoints (start, each turning node, end).
    /// </summary>
    private List<Vector3> SimplifyToWaypoints(List<Vector3> fullPath)
    {
        if (fullPath == null || fullPath.Count <= 2) return fullPath ?? new List<Vector3>();

        List<Vector3> waypoints = new List<Vector3> { fullPath[0] };
        Vector3 prevDir = (fullPath[1] - fullPath[0]).normalized;

        for (int i = 2; i < fullPath.Count; i++)
        {
            Vector3 newDir = (fullPath[i] - fullPath[i - 1]).normalized;
            if (Vector3.Angle(prevDir, newDir) > 0.1f) // small threshold -> detects turns
            {
                waypoints.Add(fullPath[i - 1]);
            }
            prevDir = newDir;
        }

        waypoints.Add(fullPath[fullPath.Count - 1]);
        return waypoints;
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

        return edges.Where(n => n != null && n.walkable).ToList();
    }

    private float PathLength(List<Vector3> path)
    {
        float len = 0f;
        if (path == null) return len;
        for (int i = 1; i < path.Count; i++)
            len += Vector3.Distance(path[i - 1], path[i]);
        return len;
    }

    /// <summary>
    /// Robust nearest-node lookup: search grid for the node with smallest squared distance.
    /// This avoids rounding / orientation mismatches.
    /// </summary>
    private Node GetNearestNode(Vector3 pos)
    {
        if (grid == null) return null;

        Node best = null;
        float bestDist = float.MaxValue;
        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                var n = grid[x, y];
                if (n == null) continue;
                float d = (n.worldPos - pos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
        }
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

        if (lastPath != null && lastPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < lastPath.Count; i++)
                Gizmos.DrawSphere(lastPath[i], Mathf.Max(0.001f, cellSize * 0.12f));

            Gizmos.color = Color.yellow;
            for (int i = 0; i < lastPath.Count - 1; i++)
                Gizmos.DrawLine(lastPath[i], lastPath[i + 1]);
        }
    }
}
