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

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start() => BuildGrid();

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
        if (BoardCollider == null) return;

        Bounds b = BoardCollider.bounds;
        Vector3 firstCellPos = DeskControl.Instance.GetGridCell(0, 0).transform.position;
        origin = new Vector3(firstCellPos.x, b.min.y, firstCellPos.z);
        cellSize = Mathf.Min(b.size.x / gridCountX, b.size.z / gridCountY);

        grid = new Node[gridCountX, gridCountY];

        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                Vector3 worldPos = origin + new Vector3(x * cellSize, 0, y * cellSize);
                bool walkable = !Physics.CheckBox(worldPos, Vector3.one * (cellSize * 0.4f), Quaternion.identity, obstacleMask);
                grid[x, y] = new Node(worldPos, walkable, x, y);
            }
        }
    }

    public MultiAStarPaths GetPathsToTarget((int, int) targetPoint, int surroundCount = 4)
    {
        MultiAStarPaths result = new MultiAStarPaths
        {
            TargetPoint = targetPoint,
            SurroundPaths = new List<List<Vector3>>()
        };

        if (grid == null) BuildGrid();

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

        var randomOuter = outerNodes.OrderBy(x => Random.value).Where(n => n != bestStart).Take(surroundCount);
        foreach (var s in randomOuter)
        {
            var path = FindPath(s, targetNode);
            if (path.Count > 0)
                result.SurroundPaths.Add(path);
        }

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
        foreach (var n in grid) { n.gCost = float.MaxValue; n.parent = null; }

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
        path.Reverse();
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
        pos = BoardCollider.bounds.ClosestPoint(pos);
        Vector3 offset = pos - origin;
        int x = Mathf.Clamp(Mathf.FloorToInt(offset.x / cellSize), 0, gridCountX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(offset.z / cellSize), 0, gridCountY - 1);
        return grid[x, y];
    }
}
