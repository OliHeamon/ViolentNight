using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace ViolentNight.Systems.Data.DataFileTypes;

/// <summary>
/// A struct generated from targeting data files.
/// </summary>
/// <param name="npcType">The NPC type that uses this targeting data.</param>
/// <param name="maxSightRangeTiles">The maximum distance this NPC searches for targets at.</param>
/// <param name="weights">An array of targeting weights: each weight contains the NPC type (-1 if referring to the player) as well as its weight (which biases towards that target in distance calculations).</param>
public struct TargetingData(int npcType, int maxSightRangeTiles, TargetWeightingInfo[] weights)
{
    public int NPCType = npcType;
    public int MaxSightRangeTiles = maxSightRangeTiles;
    public TargetWeightingInfo[] Weights = weights;
}

public struct TargetWeightingInfo(int type, float weight)
{
    public int Type = type;
    public float Weight = weight;
}

public sealed class TargetingDataManager : IDataManager<TargetingData>
{
    public string Extension => ".targeting.hjson";

    public void Populate(ReadOnlySpan<JObject> inputs, Span<TargetingData> outputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            JObject json = inputs[i];

            TargetingData definition = new();

            foreach (var rootPair in json)
            {
                if (rootPair is not { Key: string npcId, Value: JObject setJson })
                {
                    continue;
                }

                definition.NPCType = ViolentNightUtils.StringToNpcId(npcId);

                float playerWeight = 0;

                foreach (var property in setJson.Properties())
                {
                    switch (property.Name)
                    {
                        case "MaxSightRangeTiles":
                            definition.MaxSightRangeTiles = property.Value.Value<int>();
                            break;
                        case "PlayerWeight":
                            playerWeight = property.Value.Value<float>();
                            break;
                        case "PreferredTargetWeights":
                            JArray[] values = property.Value.Values<JArray>().ToArray();

                            // An extra slot is allocated for the player targeting data.
                            TargetWeightingInfo[] targetingWeights = new TargetWeightingInfo[values.Length + 1];

                            for (int j = 0; j < values.Length; j++)
                            {
                                JArray entry = values[j];

                                string preferredNpc = entry.Value<string>(0);
                                float weight = entry.Value<float>(1);

                                targetingWeights[j] = new TargetWeightingInfo(ViolentNightUtils.StringToNpcId(preferredNpc), weight);
                            }

                            targetingWeights[^1] = new TargetWeightingInfo(-1, playerWeight);

                            definition.Weights = targetingWeights;

                            break;
                    }
                }
            }

            outputs[i] = definition;
        }
    }
}
