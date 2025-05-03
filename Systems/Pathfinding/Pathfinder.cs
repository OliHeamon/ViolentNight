using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ViolentNight.Systems.Pathfinding;

public sealed class Pathfinder(MappedRegion defaultRegion, Func<List<Point>, Point> findIdealEndNode)
{
    public MappedRegion ActiveRegion { get; set; } = defaultRegion;

    private readonly Func<List<Point>, Point> findIdealEndNode = findIdealEndNode;

    public readonly List<Edge> edgesToTraverse = [];

    private int navEdge = 0;

    public bool StartPath(Point potentialNode, out bool alreadyAtGoal)
    {
        alreadyAtGoal = false;

        edgesToTraverse.Clear();
        navEdge = 0;

        if (ActiveRegion == null)
            return false;

        bool successfulNode = ActiveRegion
            .TryGetStartAndEndNodes(potentialNode, findIdealEndNode, out int startNodeId, out int endNodeId, out List<int> accessibleNodeIds);

        if (startNodeId == endNodeId)
        {
            alreadyAtGoal = true;
            return true;
        }

        if (!successfulNode)
            return false;

        // Use Djikstra to find the shortest path between the start and end node.
        edgesToTraverse.AddRange(Djikstra(startNodeId, endNodeId, accessibleNodeIds));

        // TODO: address case where start and end are same.
        return edgesToTraverse.Count > 0;
    }

    public bool TryGetNextNavPoint(out Point point, out EdgeType edgeType)
    {
        point = Point.Zero;
        edgeType = EdgeType.Walk;

        if (edgesToTraverse.Count == 0)
        {
            return false;
        }

        if (navEdge > edgesToTraverse.Count - 1)
        {
            navEdge = 0;
            return false;
        }

        Edge edge = edgesToTraverse[navEdge];

        point = ActiveRegion.NodeIdToPoint[edge.To];
        edgeType = edge.EdgeType;

        navEdge++;

        return true;
    }

    // Truth nuke: I want to use Djikstra's here to guarantee the shortest path, A* yielded very odd results with jumps.
    private List<Edge> Djikstra(int startId, int endId, List<int> accessibleNodeIds)
    {
        PriorityQueue<int, float> vertexQ = new();

        Dictionary<int, float> dist = [];
        Dictionary<int, int> prev = [];

        vertexQ.Enqueue(startId, 0);
        dist[startId] = 0;

        IReadOnlyDictionary<int, List<Edge>> adjacencyMap = ActiveRegion.AdjacencyMap;

        while (vertexQ.Count > 0)
        {
            int u = vertexQ.Dequeue();

            List<Edge> edges = adjacencyMap[u];

            foreach (Edge edge in edges)
            {
                int v = edge.To;

                float uDist = dist.TryGetValue(u, out float uDistance) ? uDistance : float.PositiveInfinity;
                float vDist = dist.TryGetValue(v, out float vDistance) ? vDistance : float.PositiveInfinity;

                if (!accessibleNodeIds.Contains(v))
                {
                    vDist = float.PositiveInfinity;
                    uDist = float.PositiveInfinity;
                }

                float alt = uDist + edge.Cost;

                if (alt < vDist)
                {
                    prev[v] = u;
                    dist[v] = alt;

                    vertexQ.Enqueue(v, alt);
                }
            }
        }

        List<int> sequence = [];

        int target = endId;

        if (prev.ContainsKey(target) || target == startId)
        {
            while (prev.ContainsKey(target))
            {
                sequence.Add(target);
                target = prev[target];
            }
        }

        sequence.Add(startId);

        sequence.Reverse();

        List<Edge> edgeSequence = [];

        for (int i = 0; i < sequence.Count - 1; i++)
        {
            int node = sequence[i];

            List<Edge> neighbours = adjacencyMap[node];

            Edge correctEdge = neighbours.First(e => e.From == sequence[i] && e.To == sequence[i + 1]);

            edgeSequence.Add(correctEdge);
        }

        return edgeSequence;
    }

    /*
    private float Heuristic(int start, int end)
    {
        Point nodePoint = ActiveRegion.NodeIdToPoint[start];
        Point goalPoint = ActiveRegion.NodeIdToPoint[end];

        float dx = nodePoint.X - goalPoint.X;
        float dy = nodePoint.Y - goalPoint.Y;

        float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));

        return distance;
    }

    private List<Edge> AStar(int startId, int endId)
    {
        IReadOnlyDictionary<int, List<Edge>> adjacencyMap = ActiveRegion.AdjacencyMap;

        // TODO: change to prio queue?
        HashSet<int> openSet = [startId];

        // For a node N, cameFrom[n] is the node preceding it on the cheapest path from the start to N currently known.
        Dictionary<int, int> cameFrom = [];

        Dictionary<int, float> gScore = [];
        Dictionary<int, float> fScore = [];

        foreach (int node in adjacencyMap.Keys)
        {
            gScore[node] = float.PositiveInfinity;
            fScore[node] = float.PositiveInfinity;
        }

        gScore[startId] = 0;
        fScore[startId] = Heuristic(startId, endId);

        while (openSet.Count > 0)
        {
            int current = -1;
            float lowestFScore = float.PositiveInfinity;

            foreach (int node in openSet)
            {
                if (fScore[node] < lowestFScore)
                {
                    current = node;
                }
            }

            if (current == endId)
                return NodeSequence(cameFrom, current);

            openSet.Remove(current);

            List<Edge> neighbours = adjacencyMap[current];

            foreach (Edge edge in neighbours)
            {
                int neighbouringNode = edge.To;

                float tentativeG = gScore[current] + edge.Cost;

                if (tentativeG < gScore[neighbouringNode])
                {
                    cameFrom[neighbouringNode] = current;
                    gScore[neighbouringNode] = tentativeG;
                    fScore[neighbouringNode] = tentativeG + Heuristic(neighbouringNode, endId);

                    if (!openSet.Contains(neighbouringNode))
                    {
                        openSet.Add(neighbouringNode);
                    }
                }
            }
        }

        return null;
    }

    private List<Edge> NodeSequence(Dictionary<int, int> cameFrom, int current)
    {
        IReadOnlyDictionary<int, List<Edge>> adjacencyMap = ActiveRegion.AdjacencyMap;

        List<int> sequence = [current];

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            sequence.Add(current);
        }

        sequence.Reverse();

        List<Edge> edgeSequence = [];

        // This needs improving to not be so inefficient at rescontructing the edges.
        for (int i = 0; i < sequence.Count - 1; i++)
        {
            int node = sequence[i];

            List<Edge> neighbours = adjacencyMap[node];

            Edge correctEdge = neighbours.First(e => e.From == sequence[i] && e.To == sequence[i + 1]);

            edgeSequence.Add(correctEdge);
        }

        return edgeSequence;
    }*/
}
