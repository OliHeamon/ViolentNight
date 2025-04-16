using System;
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
}
