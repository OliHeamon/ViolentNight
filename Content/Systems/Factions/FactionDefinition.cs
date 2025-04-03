namespace ViolentNight.Content.Systems.Factions;

/// <summary>
/// A struct generated from faction data files.
/// </summary>
/// <param name="identifier">A string identifier for the faction type.</param>
/// <param name="members">NPC IDs of all members. In the data file, these are encoded as strings - ID name (vanilla) or type name (modded).</param>
/// <param name="enemies">Faction identifiers of all enemy factions.</param>
public struct FactionDefinition(string identifier, int[] members, string[] enemies)
{
    public string Identifier = identifier;
    public int[] Members = members;
    public string[] EnemyFactions = enemies;
}
