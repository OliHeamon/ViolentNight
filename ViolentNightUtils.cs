using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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

    public static bool GetTilesBelow(Rectangle hitbox, out List<Point> tiles)
    {
        tiles = [];

        int y = (int)hitbox.Bottom().Y + 1;

        int minX = (int)hitbox.BottomLeft().X;
        int maxX = (int)hitbox.BottomRight().X;

        for (int i = minX; i <= maxX; i += 14)
        {
            int x = (int)MathHelper.Clamp(i, minX, maxX);

            Point tileCheckPoint = new(x / 16, y / 16);

            if (!NPCCanStandOnTile(tileCheckPoint.X, tileCheckPoint.Y))
                continue;

            tiles.Add(tileCheckPoint);
        }

        return tiles.Count > 0;
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
}
