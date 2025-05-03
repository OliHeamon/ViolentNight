using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using ViolentNight.Systems.Geometry;

namespace ViolentNight.Systems.Pathfinding;

/// <summary>
/// When an NPC scans a region of tiles to build a navigation graph, it is stored in a MappedRegion. These regions automatically update when the tilemap is changed.
/// Mapped regions are static, and are relative to a central tile.
/// </summary>
public sealed class MappedRegion(int rangeTiles, Rectangle parentNpcHitbox, int maxHorizontalJumpDistance, int maxVerticalJumpHeight)
{
    private const float WalkCost = 1;
    private const float FallCost = 2;
    private const float JumpCostMultiplier = 1.25f;

    public IReadOnlyDictionary<int, List<Edge>> AdjacencyMap => adjacencyMap;

    public IReadOnlyDictionary<int, Point> NodeIdToPoint => nodeIdToPoint;

    public IReadOnlyDictionary<Point, int> PointToNodeId => pointToNodeId;

    private readonly int rangeTiles = rangeTiles;

    private readonly Rectangle parentNpcHitbox = parentNpcHitbox;

    private readonly Dictionary<Point, int> pointToNodeId = [];
    private readonly Dictionary<int, Point> nodeIdToPoint = [];

    private readonly Dictionary<int, List<Edge>> adjacencyMap = [];

    private readonly int maxHorizontalJumpDistance = maxHorizontalJumpDistance;
    private readonly int maxVerticalJumpHeight = maxVerticalJumpHeight;

    public void Remap(Vector2 center)
    {
        pointToNodeId.Clear();
        nodeIdToPoint.Clear();
        adjacencyMap.Clear();

        for (int y = -rangeTiles; y < rangeTiles; y++)
        {
            for (int x = -rangeTiles; x < rangeTiles; x++)
            {
                // The world position of the tile being checked.
                Vector2 tilePosition = new Vector2(x * 16, y * 16) + center;

                // Convert to integer tile coordinates.
                int tileX = (int)tilePosition.X / 16;
                int tileY = (int)tilePosition.Y / 16;

                if (!WorldGen.InWorld(tileX, tileY))
                    continue;

                // tileX and tileY indicate the bottom-left of the checked area.
                int widthInTiles = (int)Math.Ceiling(parentNpcHitbox.Width / 16f);
                int heightInTiles = (int)Math.Ceiling(parentNpcHitbox.Height / 16f);

                Tile standingTile = Main.tile[tileX, tileY];

                // Eliminate tiles that are either non existent or cannot be stood on.
                if (!standingTile.HasTile || !Main.tileSolid[standingTile.TileType])
                {
                    continue;
                }

                // Check tiles above the standing tile to make sure the NPC's hitbox can fit.
                for (int yOffsetTiles = 1; yOffsetTiles < heightInTiles + 1; yOffsetTiles++)
                {
                    int offsetTileY = tileY - yOffsetTiles;

                    Tile offsetTile = Main.tile[tileX, offsetTileY];

                    // Hitbox area is obstructed.
                    if (offsetTile.HasTile && Main.tileSolid[offsetTile.TileType])
                    {
                        goto CheckNextTile;
                    }
                }

                // Tile is valid node, process it.
                AddValidNode(tileX, tileY);

            CheckNextTile:
                continue;
            }
        }

        GenerateGraph();
    }

    private void AddValidNode(int x, int y)
    {
        int id = UniqueNodeId(x, y);

        pointToNodeId[new(x, y)] = id;
        nodeIdToPoint[id] = new(x, y);
    }

    // Cantor pair.
    private int UniqueNodeId(int x, int y)
    {
        int w = x + y;
        return (w * (w + 1)) / 2 + y;
    }

    private void GenerateGraph()
    {
        foreach (Point validTile in pointToNodeId.Keys)
        {
            // Maximum downward raycast distance the NPC will check for drops tiles at.
            int maxDropTiles = 128;

            // The tiles that an NPC can walk to from a valid tile are those on the same y-level or those offset by exactly 1 tile.
            // Iterating with both of these offsets produces the complete set of valid neighbours:
            // []   []
            // [] O []
            // []   []
            int[] xOffsets = [-1, 1];
            int[] yOffsets = [maxDropTiles, -1, 0, 1];

            // Check for walkable nodes.
            // TODO: add drop neighbours
            foreach (int xOffset in xOffsets)
            {
                foreach (int yOffset in yOffsets)
                {
                    Point neighbouringPoint = new();
                    EdgeType edgeType = EdgeType.Walk;

                    void AddNode(float cost)
                    {
                        int nodeId = pointToNodeId[validTile];
                        int neighbourId = pointToNodeId[neighbouringPoint];

                        if (!adjacencyMap.ContainsKey(nodeId))
                            adjacencyMap[nodeId] = [];

                        adjacencyMap[nodeId].Add(new(neighbourId, nodeId, edgeType, cost));
                    }

                    // Check for walk edges.
                    if (yOffset != maxDropTiles)
                    {
                        neighbouringPoint = new(xOffset + validTile.X, yOffset + validTile.Y);

                        // Skip if the neighbouring point being checked is invalid.
                        if (!pointToNodeId.ContainsKey(neighbouringPoint))
                        {
                            continue;
                        }

                        AddNode(WalkCost);
                    }
                    // Check for drop edges.
                    else
                    {
                        for (int yDrop = 0; yDrop < maxDropTiles; yDrop++)
                        {
                            neighbouringPoint = new(xOffset + validTile.X, yDrop + validTile.Y);

                            // End the downward raycast if it encounters a tile.
                            if (!WorldGen.InWorld(neighbouringPoint.X, neighbouringPoint.Y))
                                break;

                            Tile tile = Main.tile[neighbouringPoint.X, neighbouringPoint.Y];

                            if (tile.HasTile && Main.tileSolid[tile.TileType])
                            {
                                if (yDrop >= 2 && pointToNodeId.ContainsKey(neighbouringPoint))
                                {
                                    edgeType = EdgeType.Fall;

                                    AddNode(FallCost);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // TODO: make falling account for hitboxes.
            List<Point> points = GenerateDestinationsFrom(validTile, maxHorizontalJumpDistance, maxVerticalJumpHeight);

            foreach (Point jumpPoint in points)
            {
                int nodeId = pointToNodeId[validTile];
                int neighbourId = pointToNodeId[jumpPoint];

                if (!adjacencyMap.ContainsKey(nodeId))
                    adjacencyMap[nodeId] = [];

                // The cost of doing a jump scales with the euclidean distance of the jump times a multiplier.
                float cost = Vector2.Distance(new(validTile.X, validTile.Y), new(jumpPoint.X, jumpPoint.Y)) * JumpCostMultiplier;

                adjacencyMap[nodeId].Add(new(neighbourId, nodeId, EdgeType.Jump, cost));
            }
        }
    }

    public void DrawEdge(Edge edge)
    {
        if (!nodeIdToPoint.ContainsKey(edge.From) || !nodeIdToPoint.ContainsKey(edge.To))
            return;

        Point origin = nodeIdToPoint[edge.From];
        Point adjacent = nodeIdToPoint[edge.To];

        Vector2 originWorld = new Vector2(origin.X * 16, origin.Y * 16) + new Vector2(8);

        if (edge.EdgeType == EdgeType.Jump)
        {
            Vector2 adjacentWorld = new Vector2(adjacent.X * 16, adjacent.Y * 16) + new Vector2(8);

            Color color = originWorld.Y > adjacentWorld.Y ? Color.Green : Color.Blue;

            List<Vector2> points = GenerateJumpProfile(originWorld, adjacentWorld);

            if (points.Count < 2)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                    continue;

                Vector2 prev = points[i - 1];
                Vector2 next = points[i];

                Utils.DrawLine(Main.spriteBatch, prev, next, color, color, 1);
            }
        }
        else
        {
            Vector2 adjacentWorld = new Vector2(adjacent.X * 16, adjacent.Y * 16) + new Vector2(8);

            Color color = edge.EdgeType switch
            {
                EdgeType.Fall => Color.Orange,
                EdgeType.Walk => Color.Yellow,
                _ => Color.White
            };

            Utils.DrawLine(Main.spriteBatch, originWorld, adjacentWorld, color, color, 1);
        }

        Vector2 screen = originWorld - Main.screenPosition;

        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)screen.X - 2, (int)screen.Y - 2, 4, 4), Color.Red);
    }

    public void DrawEdges()
    {
        foreach (int nodeId in adjacencyMap.Keys)
        {
            List<Edge> adjacent = adjacencyMap[nodeId];

            foreach (Edge edge in adjacent)
            {
                DrawEdge(edge);
            }
        }
    }

    private List<Point> GenerateDestinationsFrom(Point origin, int maxHorizontalJumpDistance, int maxVerticalJumpHeight)
    {
        List<Point> result = [];

        int minimumXDistanceToRequireJump = (int)Math.Ceiling((float)parentNpcHitbox.Width / 16);

        for (int yOffset = -maxVerticalJumpHeight; yOffset <= maxVerticalJumpHeight; yOffset++)
        {
            for (int xOffset = -maxHorizontalJumpDistance; xOffset <= maxHorizontalJumpDistance; xOffset++)
            {
                if (Math.Abs(xOffset) < minimumXDistanceToRequireJump)
                    continue;

                Point candidate = new(origin.X + xOffset, origin.Y + yOffset);

                if (pointToNodeId.ContainsKey(candidate) && IsPathClear(origin, candidate))
                {
                    result.Add(candidate);
                }
            }
        }

        return result;
    }

    private bool IsPathClear(Point origin, Point candidate)
    {
        Vector2 vectorOrigin = new(origin.X * 16, origin.Y * 16);
        Vector2 vectorCandidate = new(candidate.X * 16, candidate.Y * 16);

        List<Vector2> points = GenerateJumpProfile(vectorOrigin, vectorCandidate);

        foreach (Vector2 point in points)
        {
            Point sample = new((int)point.X / 16, (int)point.Y / 16);

            // Don't check collision involving the start or end node, we know they're valid already.
            if ((sample.X == origin.X && sample.Y == origin.Y) || (sample.X == candidate.X && sample.Y == candidate.Y))
            {
                continue;
            }

            if (!NPCCanIntersectTile(sample.X, sample.Y))
                return false;

            // tileX and tileY indicate the bottom-left of the checked area.
            int widthInTiles = (int)Math.Ceiling(parentNpcHitbox.Width / 16f);
            int heightInTiles = (int)Math.Ceiling(parentNpcHitbox.Height / 16f);

            // Check tiles above the standing tile to make sure the NPC's hitbox can fit.
            for (int yOffsetTiles = 1; yOffsetTiles < heightInTiles + 1; yOffsetTiles++)
            {
                int offsetTileY = sample.Y - yOffsetTiles;

                if (!NPCCanIntersectTile(sample.X, offsetTileY))
                    return false;
            }
        }

        return true;
    }

    public bool NPCCanIntersectTile(int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return false;

        Tile tile = Main.tile[x, y];

        if (!tile.HasTile)
            return true;
        else if (Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
        {
            return false;
        }

        return true;
    }

    private List<Vector2> GenerateJumpProfile(Vector2 start, Vector2 end)
    {
        int samplePointCount = (int)(Vector2.Distance(start, end) / 8);

        // If negative, start is larger in Y, which means Y is lower.
        bool endIsOver2BlocksHigherThanStart = (start.Y - end.Y) / 16 >= 2 && Math.Abs(end.X - start.X) / 16 <= 2;

        Vector2 midpoint;

        // Jump to a higher position.
        if (endIsOver2BlocksHigherThanStart)
        {
            // Choose the highest Y of the two as the Y middle point.
            float y = Math.Min(start.Y, end.Y);

            // Choose the X coordinate of the point with the highest Y as the X middle point.
            // If the start is lower than the end, then it's start.X.
            midpoint = new(start.X, end.Y);
        }
        // Jump to a lower or equal position.
        else
        {
            float yImpulse = Math.Min(Math.Abs(start.X - end.X), maxVerticalJumpHeight * 16);

            midpoint = new((start.X + end.X) / 2, start.Y - yImpulse);
        }

        Vector2[] controlPoints = [start, midpoint, end];

        return BezierCurve.GetPoints(controlPoints, samplePointCount);
    }

    public bool TryGetStartAndEndNodes(Point potentialNode, Func<List<Point>, Point> findIdealEndNode, out int startNodeId, out int endNodeId, out List<int> accessibleNodeIds)
    {
        startNodeId = endNodeId = -1;
        accessibleNodeIds = null;

        if (!pointToNodeId.TryGetValue(potentialNode, out int value))
        {
            return false;
        }

        accessibleNodeIds = BreadthFirstSearch(value);

        // Enumerate all nodes on the tree that can be accessed from where the NPC currently is.
        List<Point> accessibleNodes = accessibleNodeIds.Where(nodeId => adjacencyMap.ContainsKey(nodeId)).Select(nodeId => nodeIdToPoint[nodeId]).ToList();

        Point end = findIdealEndNode.Invoke(accessibleNodes);

        if (end == Point.Zero)
        {
            return false;
        }

        startNodeId = value;
        endNodeId = pointToNodeId[end];

        return true;
    }

    private List<int> BreadthFirstSearch(int origin)
    {
        int source = origin;

        List<int> traversal = [];

        Queue<int> queue = [];

        HashSet<int> visited = [];

        visited.Add(source);
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            traversal.Add(current);

            foreach (Edge edge in adjacencyMap[current])
            {
                int neighbour = edge.To;

                if (!visited.Contains(neighbour))
                {
                    visited.Add(neighbour);

                    if (adjacencyMap.ContainsKey(neighbour))
                    {
                        queue.Enqueue(neighbour);
                    }
                }
            }
        }

        return traversal;
    }
}
