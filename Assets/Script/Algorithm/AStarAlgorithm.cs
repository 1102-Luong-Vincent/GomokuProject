using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public static class AStarAlgorithm
{
    class Node
    {
        public int x, y;
        public float hCost, gCost;
        public Node parent;
        public float fCost => hCost + gCost;

        public Node(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public static List<(int, int)> AStarFindWayPoint(GomoKuType[,] board, (int, int) endPoint)
    {
        return FindWayPoints(board, endPoint);
    }



    private static List<(int, int)> FindWayPoints(GomoKuType[,] board, (int, int) endPoint)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);

        Node[,] nodes = new Node[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                nodes[x, y] = new Node(x, y);
        }

        List<(int, int)> fullPath = new List<(int, int)>();
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node endNode = nodes[endPoint.Item1, endPoint.Item2];

        for (int i = 0; i < width; i++)
        {
            if (board[i, 0] == GomoKuType.None)
                openSet.Add(nodes[i, 0]);
            if (board[i, height - 1] == GomoKuType.None)
                openSet.Add(nodes[i, height - 1]);
        }
        for (int j = 0; j < height; j++)
        {
            if (board[0, j] == GomoKuType.None)
                openSet.Add(nodes[0, j]);
            if (board[width - 1, j] == GomoKuType.None)
                openSet.Add(nodes[width - 1, j]);
        }

        foreach (var node in openSet)
        {
            node.gCost = 0;
            node.hCost = Mathf.Abs(node.x - endNode.x) + Mathf.Abs(node.y - endNode.y);
            node.parent = null;
        }

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost)
                    current = openSet[i];
            }

            if (current == endNode)
            {
                Node c = current;
                while (c != null)
                {
                    fullPath.Add((c.x, c.y));
                    c = c.parent;
                }
                fullPath.Reverse();
                return ExtractWayPoints(fullPath);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(nodes, current, width, height))
            {
                if (closedSet.Contains(neighbor))
                    continue;
                if (board[neighbor.x, neighbor.y] != GomoKuType.None && neighbor != endNode)
                    continue;

                float G = current.gCost + 1;
                if (!openSet.Contains(neighbor) || G < neighbor.gCost)
                {
                    neighbor.gCost = G;
                    neighbor.hCost = Mathf.Abs(neighbor.x - endNode.x) + Mathf.Abs(neighbor.y - endNode.y);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return new List<(int, int)>();
    }

    private static List<(int, int)> ExtractWayPoints(List<(int, int)> fullPath)
    {
        if (fullPath.Count <= 2)
            return fullPath;

        List<(int, int)> wayPoints = new List<(int, int)>();
        wayPoints.Add(fullPath[0]);

        for (int i = 1; i < fullPath.Count - 1; i++)
        {
            var prev = fullPath[i - 1];
            var curr = fullPath[i];
            var next = fullPath[i + 1];

            int dx1 = curr.Item1 - prev.Item1;
            int dy1 = curr.Item2 - prev.Item2;
            int dx2 = next.Item1 - curr.Item1;
            int dy2 = next.Item2 - curr.Item2;

            if (dx1 != dx2 || dy1 != dy2)
            {
                wayPoints.Add(curr);
            }
        }

        wayPoints.Add(fullPath.Last());
        return wayPoints;
    }



    public static List<(int, int)> AStarFindPath(GomoKuType[,] board, (int, int) endPoint)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);

        Node[,] nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nodes[x, y] = new Node(x, y);
            }
        }
        List<(int, int)> Path = new List<(int, int)>();

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node endNode = nodes[endPoint.Item1, endPoint.Item2];

        for (int i = 0; i < width; i++)
        {
            if (board[i, 0] == GomoKuType.None)
            {
                openSet.Add(nodes[i, 0]);
            }
            if (board[i, height - 1] == GomoKuType.None)
            {
                openSet.Add(nodes[i, height - 1]);
            }
        }
        for (int j = 0; j < height; j++)
        {
            if (board[0, j] == GomoKuType.None)
            {
                openSet.Add(nodes[0, j]);
            }
            if (board[width - 1, j] == GomoKuType.None)
            {
                openSet.Add(nodes[width - 1, j]);
            }
        }
        foreach (var node in openSet)
        {
            node.gCost = 0;
            node.hCost = Mathf.Abs(node.x - endNode.x) + Mathf.Abs(node.y - endNode.y);
            node.parent = null;
        }

        while (openSet.Count > 0)
        {
            //Node current = openSet.OrderBy(n => n.fCost).First();
            Node current = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {

                if (openSet[i].fCost < current.fCost)
                {
                    current = openSet[i];
                }
            }
            if (current == endNode)
            {
                Node c = current;

                while (c != null)
                {
                    Path.Add((c.x, c.y));
                    c = c.parent;
                }
                Path.Reverse();
                return Path;
            }
            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(nodes, current, width, height))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                if (board[neighbor.x, neighbor.y] != GomoKuType.None && neighbor != endNode)
                {
                    continue;
                }

                float G = current.gCost + 1;

                if (!openSet.Contains(neighbor) || G < neighbor.gCost)
                {
                    neighbor.gCost = G;
                    neighbor.hCost = Mathf.Abs(neighbor.x - endNode.x) + Mathf.Abs(neighbor.y - endNode.y);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        return Path;
    }

    static List<Node> GetNeighbors(Node[,] nodes, Node node, int width, int height)
    {
        List<Node> neighbors = new List<Node>();

        int[,] directions = new int[,]
        {
            { 1, 0 }, { -1, 0 }, //right and left
            { 0, 1 }, { 0, -1 } //up, down
        };

        for (int i = 0; i < 4; i++)
        {
            int xNode = node.x + directions[i, 0];
            int yNode = node.y + directions[i, 1];

            if (xNode >= 0 && xNode < width && yNode >= 0 && yNode < height)
            {
                neighbors.Add(nodes[xNode, yNode]);
            }
        }
        return neighbors;
    }
}
