using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using ViolentNight.Systems.Data;
using ViolentNight.Systems.Data.DataFileTypes;

namespace ViolentNight.Systems;

public sealed class FactionSystem : ModSystem
{
    private static readonly Dictionary<int, HashSet<int>> enemiesOf = [];

    private static readonly Dictionary<int, string> factionName = [];

    public override void OnModLoad()
    {
        ReadOnlySpan<FactionData> definitions = DataManager.GetAllDataOfType<FactionData>();

        GenerateEnemyMap(definitions);
    }

    private static void GenerateEnemyMap(ReadOnlySpan<FactionData> definitions)
    {
        Dictionary<string, FactionData> factionsById = [];

        foreach (FactionData faction in definitions)
        {
            factionsById[faction.Identifier] = faction;
        }

        // For every NPC that is in a faction, construct a HashSet of the NPC IDs of their enemies.
        foreach (string factionId in factionsById.Keys)
        {
            // Enumerate every NPC that is in a faction.
            foreach (int factionNpc in factionsById[factionId].Members)
            {
                factionName[factionNpc] = factionId;

                // Enumerate all the enemy factions of the given NPC.
                foreach (string enemyFaction in factionsById[factionId].EnemyFactions)
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

    public static string GetFactionIdentifier(int npc) => factionName[npc];
}
