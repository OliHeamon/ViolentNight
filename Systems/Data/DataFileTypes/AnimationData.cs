using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ViolentNight.Systems.Data.DataFileTypes;

/// <summary>
/// A struct generated from animation data files.
/// </summary>
/// <param name="npcType">The NPC corresponding to this animation data.</param>
/// <param name="animationStates">An array of animation state infos. Each animation state info contains its mode as well as an array of frames,
/// and each frame contains the frame ID as well as additional data such as delay or variant ID depending on the mode.</param>
public struct AnimationData(int npcType, int frames, AnimationStateInfo[] animationStates)
{
    public const string CycleMode = "Cycle";
    public const string VariantMode = "Variant";

    public int NPCType = npcType;
    public int Frames = frames;
    public AnimationStateInfo[] AnimationStates = animationStates;
}

public struct AnimationStateInfo(string identifier, string mode, AnimationFrame[] frames)
{
    public string Identifier = identifier;
    public string Mode = mode;
    public AnimationFrame[] Frames = frames;
}

public struct AnimationFrame(int extraInfo, int frame)
{
    public int ExtraInfo = extraInfo;
    public int Frame = frame;
}

public sealed class AnimationDataManager : IDataManager<AnimationData>
{
    public string Extension => ".animation.hjson";

    public void Populate(ReadOnlySpan<JObject> inputs, Span<AnimationData> outputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            JObject json = inputs[i];

            AnimationData definition = new();

            List<AnimationStateInfo> animationStateInfos = [];

            foreach (var rootPair in json)
            {
                if (rootPair is not { Key: string npcId, Value: JObject setJson })
                {
                    continue;
                }

                definition.NPCType = ViolentNightUtils.StringToNpcId(npcId);

                IEnumerable<JProperty> properties = setJson.Properties();

                foreach (var property in properties)
                {
                    switch (property.Name)
                    {
                        case "Frames":
                            definition.Frames = property.Value.Value<int>();
                            break;
                        default:
                            AnimationStateInfo info = new()
                            {
                                Identifier = property.Name,
                            };

                            foreach (JToken token in property.Values())
                            {
                                if (token is JProperty framesArray && framesArray.Name == "Frames")
                                {
                                    AnimationFrame[] frames;

                                    switch (info.Mode)
                                    {
                                        case AnimationData.CycleMode:

                                            JArray[] values = framesArray.Value.Values<JArray>().ToArray();

                                            frames = new AnimationFrame[values.Length];

                                            for (int j = 0; j < values.Length; j++)
                                            {
                                                JArray frame = values[j];

                                                int extraInfo = frame.Value<int>(0);
                                                int frameId = frame.Value<int>(1);

                                                frames[j] = new(extraInfo, frameId);
                                            }

                                            info.Frames = frames;

                                            animationStateInfos.Add(info);

                                            break;
                                        case AnimationData.VariantMode:

                                            int[] frameValues = framesArray.Value.Values<int>().ToArray();

                                            frames = new AnimationFrame[frameValues.Length];

                                            for (int j = 0; j < frameValues.Length; j++)
                                            {
                                                frames[j] = new(0, frameValues[j]);
                                            }

                                            info.Frames = frames;

                                            animationStateInfos.Add(info);

                                            break;
                                    }
                                }
                                else if (token is JProperty modeProperty && modeProperty.Name == "Mode" )
                                {
                                    info.Mode = modeProperty.Value.Value<string>();
                                }
                            }

                            break;
                    }
                }
            }

            definition.AnimationStates = animationStateInfos.ToArray();

            outputs[i] = definition;
        }
    }
}
