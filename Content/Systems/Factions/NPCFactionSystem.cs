using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace ViolentNight.Content.Systems.Factions;

public sealed class NPCFactionSystem : ModSystem
{
    private static readonly Dictionary<int, HashSet<int>> enemiesOf = [];

    private static readonly Dictionary<int, string> factionName = [];

    public override void Load()
    {
        FactionDefinition[] definitions = LoadFactionDefinitions();

        GenerateEnemyMap(definitions);
    }

    private static FactionDefinition[] LoadFactionDefinitions()
    {
    }

    private static void GenerateEnemyMap(FactionDefinition[] definitions)
    {
        Dictionary<string, FactionDefinition> factionsById = [];

        foreach (FactionDefinition faction in definitions)
        {
            factionsById[faction.Identifier] = faction;
        }

        // For every NPC that is in a faction, construct a HashSet of the NPC IDs of their enemies.
        foreach (string id in factionsById.Keys)
        {
            // Enumerate every NPC that is in a faction.
            foreach (int factionNpc in factionsById[id].Members)
            {
                // Enumerate all the enemy factions of the given NPC.
                foreach (string enemyFaction in factionsById[id].EnemyFactions)
                {
                    HashSet<int> enemies = [];

                    foreach (int enemy in factionsById[enemyFaction].Members)
                    {
                        enemies.Add(enemy);
                    }

                    enemiesOf[factionNpc] = enemies;
                }
            }
        }
    }

    public static bool IsEnemyOf(int npc, int potentialEnemy) => enemiesOf[npc].Contains(potentialEnemy);

    public static string? GetFactionIdentifier(int npc) => factionName.TryGetValue(npc, out string identifier) ? identifier : null;
}
