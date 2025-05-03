using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using ViolentNight.Systems;
using ViolentNight.Systems.AI;
using ViolentNight.Systems.Data;
using ViolentNight.Systems.Data.DataFileTypes;

namespace ViolentNight.Content.NPCs;

public abstract class ViolentNightNPC : ModNPC
{
    private static readonly Dictionary<int, TargetingData> targetingDataByType = [];
    private static readonly Dictionary<int, AnimationData> animationDataByType = [];

    private static readonly Dictionary<int, Dictionary<string, AnimationStateInfo>> animationStatesByType = [];
    private static readonly Dictionary<int, Dictionary<int, float>> targetingWeightsByType = [];

    protected Target? Target { get; private set; }

    protected int AnimationVariant { get; set; }

    protected AIContainer AIState { get; private set; }

    private string animationIdentifier;
    private int cycleFrameIndex;

    public override void SetStaticDefaults()
    {
        foreach (TargetingData data in DataManager.GetAllDataOfType<TargetingData>())
        {
            if (data.NPCType == Type)
            {
                targetingDataByType[Type] = data;
                break;
            }
        }

        foreach (AnimationData data in DataManager.GetAllDataOfType<AnimationData>())
        {
            if (data.NPCType == Type)
            {
                animationDataByType[Type] = data;
                break;
            }
        }

        targetingWeightsByType[Type] = [];

        foreach (TargetWeightingInfo info in targetingDataByType[Type].Weights)
        {
            targetingWeightsByType[Type][info.Type] = info.Weight;
        }

        animationStatesByType[Type] = [];

        foreach (AnimationStateInfo info in animationDataByType[Type].AnimationStates)
        {
            animationStatesByType[Type][info.Identifier] = info;
        }

        Main.npcFrameCount[Type] = animationDataByType[Type].Frames;
    }

    public override void OnSpawn(IEntitySource source)
    {
        AIState = InitialiseAI();
    }

    public sealed override void AI()
    {
        // In certain cases, such as mid-attack, it is undesirable for the NPC to change targets.
        if (!CanChangeTargets())
            return;

        List<Target> targets = [];

        TargetingData npcTargetingData = targetingDataByType[Type];

        Dictionary<int, float> npcTargetingWeights = targetingWeightsByType[Type];

        float sightRange = npcTargetingData.MaxSightRangeTiles * 16;

        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = npc.Distance(NPC.Center);
            float weight = 1.0f;

            if (distance > sightRange)
                continue;

            // Only hostile NPCs in an enemy faction will be targeted, and only NPCs in a faction may target other NPCs.
            if (!FactionSystem.IsEnemyOf(Type, npc.type))
                continue;

            if (npcTargetingWeights.TryGetValue(npc.type, out float value))
                weight = value;

            // The effective weight is the division of the distance to the target by its weight.
            // This means that targets with a higher weight appear closer, scaling linearly.
            float effectiveWeight = distance / weight;

            targets.Add(new(npc.type, effectiveWeight, npc.Hitbox));
        }

        foreach (Player player in Main.ActivePlayers)
        {
            float distance = player.Distance(NPC.Center);

            if (distance > sightRange)
                continue;

            const int PlayerType = -1;

            // The effective distance is the division of the distance to the target by its weight.
            // This means that targets with a higher weight appear closer.
            float effectiveWeight = distance / npcTargetingWeights[PlayerType];

            targets.Add(new(PlayerType, effectiveWeight, player.Hitbox));
        }

        if (targets.Count == 0)
        {
            Target = null;
            return;
        }

        // Order targets by effective weight (smallest first).
        targets.Sort((t1, t2) => t1.EffectiveWeight.CompareTo(t2.EffectiveWeight));

        // Assign the main target to the highest-priority entry.
        Target = targets[0];

        AIState?.UpdateCurrentState();
    }

    /// <summary>
    /// Used to determine if the NPC is able to change targets this tick. Useful in cases where it may not be desired to do so.
    /// </summary>
    public virtual bool CanChangeTargets()
    {
        return true;
    }

    public abstract AIContainer InitialiseAI();

    public sealed override void FindFrame(int frameHeight)
    {
        Dictionary<string, AnimationStateInfo> npcAnimations = animationStatesByType[Type];

        if (animationStatesByType.Count == 0)
            return;

        if (AIState is null)
            animationIdentifier = npcAnimations.Keys.First();
        // If the current AI state's identifier is a valid animation state identifier, switch to that one.
        else if (npcAnimations.ContainsKey(AIState.CurrentState.Identifier))
            animationIdentifier = AIState.CurrentState.Identifier;

        if (!npcAnimations.TryGetValue(animationIdentifier, out AnimationStateInfo currentState))
            return;

        switch (currentState.Mode)
        {
            case AnimationData.CycleMode:
                RunAnimationCycle(currentState);
                break;
            case AnimationData.VariantMode:
                RunAnimationVariant(currentState);
                break;
        }

        // Reset cycle frame if not in cycle state.
        if (currentState.Mode != AnimationData.CycleMode)
        {
            cycleFrameIndex = 0;
        }
    }

    private void RunAnimationCycle(AnimationStateInfo currentState)
    {
        AnimationFrame frame = currentState.Frames[cycleFrameIndex];

        // For cycle animations, ExtraInfo is the delay, so this tracks when the delay has elapsed.
        if (++NPC.frameCounter % frame.ExtraInfo == 0)
        {
            cycleFrameIndex++;
            NPC.frameCounter = 0;
        }

        if (cycleFrameIndex > currentState.Frames.Length - 1)
        {
            cycleFrameIndex = 0;
        }

        int frameHeight = NPC.frame.Height;

        NPC.frame = new(0, frame.Frame * frameHeight, NPC.frame.Width, frameHeight);
    }

    private void RunAnimationVariant(AnimationStateInfo currentState)
    {
        if (AnimationVariant < 0)
        {
            AnimationVariant = currentState.Frames.Length - 1;
        }
        if (AnimationVariant > currentState.Frames.Length - 1)
        {
            AnimationVariant = 0;
        }

        int frameId = currentState.Frames[AnimationVariant].Frame;
        int frameHeight = NPC.frame.Height;

        // Only works with vertically stacked spritesheets.
        NPC.frame = new(0, frameId * frameHeight, NPC.frame.Width, frameHeight);
    }
}

public struct Target(int type, float effectiveWeight, Rectangle hitbox)
{
    public int Type = type;
    public float EffectiveWeight = effectiveWeight;
    public Rectangle Hitbox = hitbox;
}
