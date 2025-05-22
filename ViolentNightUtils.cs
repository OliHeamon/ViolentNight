using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ViolentNight;

public static class ViolentNightUtils
{
    public static int StringToNpcId(string name)
    {
        if (NPCID.Search.TryGetId(name, out int id))
        {
            return id;
        }

        foreach (Mod mod in ModLoader.Mods)
        {
            if (ModContent.TryFind(mod.Name, name, out ModNPC npc))
            {
                return npc.Type;
            }
        }

        throw new Exception($"Invalid NPC ID in data file: {name}");
    }

    public static bool GetTilesBelow(Rectangle hitbox, out Point[] tiles)
    {
        List<Point> tileList = [];

        int y = (int)hitbox.Bottom().Y + 1;

        int minX = (int)hitbox.BottomLeft().X;
        int maxX = (int)hitbox.BottomRight().X;

        // This isn't perfect, but will work for any NPC with a hitbox less than 16 tiles wide.
        for (int i = minX; i <= maxX; i += 14)
        {
            int x = (int)MathHelper.Clamp(i, minX, maxX);

            Point tileCheckPoint = new(x / 16, y / 16);

            if (!NPCCanStandOnTile(tileCheckPoint.X, tileCheckPoint.Y))
                continue;

            tileList.Add(tileCheckPoint);
        }

        tiles = tileList.ToArray();

        return tiles.Length > 0;
    }

    private static bool NPCCanStandOnTile(int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return false;

        Tile tile = Main.tile[x, y];

        if (!tile.HasTile)
            return false;
        else if (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
        {
            return true;
        }

        return false;
    }

    public static float InverseLerp(float value, float a, float b) => (value - a) / (b - a);

    public static bool HasLineOfSight(Vector2 start, Vector2 end)
    {
        float distanceTiles = Vector2.Distance(start, end) / 16;

        float step = 1 / (distanceTiles * MathF.Sqrt(2) * 4);

        for (float t = 0; t <= 1; t += step)
        {
            Vector2 testPosition = Vector2.Lerp(start, end, t);

            int tileX = (int)Math.Floor(testPosition.X / 16);
            int tileY = (int)Math.Floor(testPosition.Y / 16);

            Vector2 tileCentre = new Vector2(tileX * 16, tileY * 16) + new Vector2(8, 8);

            if (!WorldGen.InWorld(tileX, tileY))
                return false;

            Tile testTile = Main.tile[tileX, tileY];

            bool opaque = NPCCanStandOnTile(tileX, tileY) && !TileID.Sets.Platforms[testTile.TileType];

            if (!opaque)
                continue;

            float y;

            switch (testTile.BlockType)
            {
                case BlockType.HalfBlock:
                    // If this is true, then the test position is below the centre of the tile.
                    if (testPosition.Y > tileCentre.Y)
                        return false;
                    break;
                case BlockType.SlopeDownLeft:
                case BlockType.SlopeUpRight:
                    y = MathHelper.Lerp(
                        tileCentre.Y - 8,
                        tileCentre.Y + 8,
                        InverseLerp(testPosition.X, tileCentre.X - 8, tileCentre.X + 8)
                    );

                    if (testTile.BlockType == BlockType.SlopeDownLeft && !(testPosition.Y < y))
                        return false;
                    else if (testTile.BlockType == BlockType.SlopeUpRight && !(testPosition.Y > y))
                        return false;

                        break;

                case BlockType.SlopeDownRight:
                case BlockType.SlopeUpLeft:

                    y = MathHelper.Lerp(
                        tileCentre.Y + 8,
                        tileCentre.Y - 8,
                        InverseLerp(testPosition.X, tileCentre.X - 8, tileCentre.X + 8)
                    );

                    if (testTile.BlockType == BlockType.SlopeDownRight && !(testPosition.Y < y))
                        return false;
                    else if (testTile.BlockType == BlockType.SlopeUpLeft && !(testPosition.Y > y))
                        return false;
                    break;
                // Solid block.
                default:
                    return false;
            }
        }

        return true;
    }
}
