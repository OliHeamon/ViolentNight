using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using ViolentNight.Systems.AI;
using Wayfarer.API;
using Wayfarer.Data;
using Wayfarer.Edges;
using Wayfarer.Pathfinding;

namespace ViolentNight.Content.NPCs.Elves;

public sealed class Elfantryman : ViolentNightNPC
{
    private WayfarerHandle handle;
    private PathResult path;

    private bool jumped;
    private int jumpDirection;

    private int stuckDuration;

    private const float maxXVelocity = 3;

    public int Timer
    {
        get => (int)NPC.ai[0];
        set => NPC.ai[0] = value;
    }

    public override void SetDefaults()
    {
        NPC.width = 16;
        NPC.height = 40;

        NPC.defense = 6;
        NPC.lifeMax = 200;

        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath2;

        NPC.value = 0;

        NPC.knockBackResist = 0.5f;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Invasions.FrostMoon,
            new FlavorTextBestiaryInfoElement("Mods.ViolentNight.Bestiary.Elfantryman"),
        ]);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

        spriteBatch.Draw(texture, NPC.position - screenPos + new Vector2(-8, 6), NPC.frame, drawColor, NPC.rotation, NPC.Size / 2, NPC.scale, NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

        if (handle.Initialized && path is not null)
        {
            //WayfarerAPI.DebugRenderNavMesh(handle, Main.spriteBatch);
            WayfarerAPI.DebugRenderPath(handle, Main.spriteBatch, path);
        }

        return false;
    }

    public override AIContainer InitialiseAI()
    {
        AIContainer container = new AIContainer("Idle")
            .AddState(new("Idle", IdleAI))
            .AddState(new("Navigating", NavigatingAI))
            .AddState(new("Attack", AttackAI))
            .AddState(new("Jumping", JumpingAI))
            .AddState(new("WaitingForPath", WaitingAI));

        container.OnStateChanged += () =>
        {
            AnimationVariant = 0;
            Timer = 0;

            if (container.CurrentState.Identifier == "WaitingForPath")
            {
                StartPathing();
            }
        };

        return container;
    }

    public override void OnSpawn(IEntitySource source)
    {
        base.OnSpawn(source);

        Timer = 320;

        NavMeshParameters navMeshParameters = new(
            NPC.Center.ToTileCoordinates(),
            100,
            WayfarerPresets.DefaultIsTileValid
        );
        NavigatorParameters navigatorParameters = new(
            NPC.Hitbox,
            WayfarerPresets.DefaultJumpFunction,
            new(8, 10),
            () => NPC.gravity,
            SelectDestination
        );

        WayfarerAPI.TryCreatePathfindingInstance(navMeshParameters, navigatorParameters, out handle);
    }

    private void IdleAI()
    {
        NPC.velocity.X *= 0.95f;

        Timer++;

        if (Timer % 300 == 0)
        {
            NPC.spriteDirection = Main.rand.Next([-1, 1]);
        }

        if (Target != null && Timer >= 360)
        {
            Timer = 0;

            AIState.TransitionTo("WaitingForPath");
        }
    }

    private void WaitingAI()
    {
        NPC.velocity.X = 0;
    }

    private void StartPathing()
    {
        bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out Point[] tiles);

        if (!tilesBelow)
            return;

        WayfarerAPI.RecalculateNavMesh(handle, NPC.Center.ToTileCoordinates());
        WayfarerAPI.RecalculatePath(handle, tiles, NewPathFound);
    }

    private void NewPathFound(PathResult result)
    {
        if (AIState.CurrentState.Identifier != "WaitingForPath")
            return;

        path = result;
        AIState.TransitionTo("Navigating");
    }

    private void PathLost()
    {
        NPC.velocity.X = 0;

        path = null;
        AIState.TransitionTo("WaitingForPath");
    }

    private void NavigatingAI()
    {
        if (NPC.position == NPC.oldPosition && path != null && path.HasPath)
        {
            stuckDuration++;

            if (stuckDuration > 60 * 5)
            {
                PathLost();

                stuckDuration = 0;
            }
        }
        else
        {
            stuckDuration = 0;
        }

        if (path is not null && path.IsAlreadyAtGoal)
        {
            AIState.TransitionTo("Attack");
            return;
        }

        if (path is null || !path.HasPath)
        {
            PathLost();
            return;
        }

        PathEdge edge = path.Current;

        if (edge.Is<Fall>() || edge.Is<Walk>())
        {
            Vector2 targetTileLocation = new Vector2(edge.To.X * 16, edge.To.Y * 16) + new Vector2(8, 8);

            int walkDirection = Math.Sign(targetTileLocation.X - NPC.Center.X);

            NPC.velocity.X += 0.2f * walkDirection;

            if (Math.Abs(NPC.velocity.X) > maxXVelocity)
            {
                NPC.velocity.X = maxXVelocity * walkDirection;
            }

            if (Math.Abs(NPC.velocity.X) < 0.02f)
            {
                NPC.velocity.X = 0;
            }

            if (edge.Is<Fall>() && NPC.velocity.Y != 0)
            {
                NPC.velocity.X *= 0.75f;
            }

            bool xClose = Math.Abs(targetTileLocation.X - NPC.Hitbox.Bottom().X) < 2;

            bool goalReachedCondition = edge.Is<Walk>() ?
                xClose :
                xClose && Math.Abs(targetTileLocation.Y - NPC.Hitbox.Bottom().Y) < 16;

            if (goalReachedCondition)
            {
                path.Advance(out bool atGoal);

                // Reached the end of the path.
                if (atGoal)
                {
                    AIState.TransitionTo("Attack");
                    return;
                }
            }

            bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out Point[] tiles);

            if (tilesBelow)
            {
                bool contains = false;

                foreach (Point node in tiles)
                {
                    if (WayfarerAPI.PointIsInNavMesh(handle, node))
                    {
                        contains = true;
                    }
                }

                if (!contains)
                {
                    PathLost();
                    return;
                }
            }

            NPC.spriteDirection = Math.Sign(NPC.velocity.X);
        }
        else if (edge.Is<Jump>())
        {
            AIState.TransitionTo("Jumping");
        }
    }

    private void JumpingAI()
    {
        if (path is null || !path.HasPath)
        {
            PathLost();
            return;
        }

        PathEdge edge = path.Current;

        if (!jumped)
        {
            Vector2 start = NPC.Hitbox.Bottom();

            // Top-middle of target tile.
            Vector2 end = new Vector2(edge.To.X * 16, edge.To.Y * 16) + new Vector2(8, 0);

            NPC.velocity = WayfarerPresets.DefaultJumpFunction(start, end, () => NPC.gravity);

            jumped = true;
            jumpDirection = Math.Sign(end.X - start.X);
        }
        else
        {
            if (NPC.velocity.X == 0)
            {
                NPC.velocity.X += (maxXVelocity / 16) * jumpDirection;
            }

            NPC.spriteDirection = jumpDirection;

            bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out Point[] tiles);

            // There are tiles that the NPC can stand on.
            if (tilesBelow)
            {
                // Setting this to false means the jump is over.
                jumped = false;

                bool contains = false;

                foreach (Point node in tiles)
                {
                    if (WayfarerAPI.PointIsInNavMesh(handle, node))
                    {
                        contains = true;
                    }
                }

                // If the tiles stood on are not in the navigation map, calculate a new path.
                if (!contains)
                {
                    PathLost();
                    return;
                }

                // If the tiles below the NPC contain the target point, then we successfully reached the jump node.
                if (tiles.Contains(edge.To))
                {
                    path.Advance(out bool atGoal);

                    // Reached the end of the path.
                    if (atGoal)
                    {
                        AIState.TransitionTo("Attack");
                        return;
                    }
                }
                else
                {
                    PathLost();
                    return;
                }

                AIState.TransitionTo("Navigating");
            }

            return;
        }
    }

    public override void PostAI()
    {
        if (AIState?.CurrentState?.Identifier != "Jumping")
        {
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
        }

        //Main.NewText(AIState?.CurrentState?.Identifier ?? "None");
    }

    private void AttackAI()
    {
        NPC.velocity.X *= 0.8f;

        if (Target is null)
        {
            AIState.TransitionTo("Idle");
            return;
        }

        Target target = Target.Value;

        if (Timer++ > 120)
        {
            if (!ViolentNightUtils.HasLineOfSight(target.Hitbox.Center(), NPC.Bottom - new Vector2(0, NPC.Hitbox.Height / 2)))
            {
                AIState.TransitionTo("Idle");
                PathLost();
                return;
            }

            Timer = 0;
        }

        float dx = target.Hitbox.Center.X - NPC.Center.X;

        NPC.spriteDirection = Math.Sign(dx);

        // For testing angle, act like the target is to the right of the NPC.
        Vector2 testTargetPosition = new(NPC.Center.X + Math.Abs(dx), target.Hitbox.Center.Y);

        Vector2 targetDirection = testTargetPosition - NPC.Center;

        float angle = (float)Math.Atan2(targetDirection.Y, targetDirection.X);

        float a = MathHelper.PiOver2;
        float b = -MathHelper.PiOver2;

        // 0 is full down, 1 is full up.
        float t = ViolentNightUtils.InverseLerp(angle, a, b);

        AnimationVariant = (int)(t * 5);

        return;
    }

    private Point SelectDestination(IReadOnlySet<Point> allNodes)
    {
        Point fallback = Point.Zero;

        Point closestColliding = fallback;
        float distanceColliding = float.PositiveInfinity;

        Point closest = fallback;
        float closestToTarget = float.PositiveInfinity;

        int index = Main.rand.Next(allNodes.Count);

        // Pick a random node if there's no target.
        if (Target is null)
            return allNodes.ElementAt(index);

        foreach (Point node in allNodes)
        {
            Vector2 world = new Vector2(node.X * 16, node.Y * 16) + new Vector2(8, 0);

            Vector2 targetCenter = Target.Value.Hitbox.Center();

            float distanceToTarget = Vector2.Distance(world, Target.Value.Hitbox.Center());
            float distanceToMe = Vector2.Distance(world, NPC.Center);

            if (distanceToTarget < closestToTarget)
            {
                closest = node;
                closestToTarget = distanceToTarget;
            }

            // If a line of sight point is found, prioritise that one, otherwise return the node closest to the player.
            // TODO: LOS points should be unable to be navigated to if an NPC is already there, so as to help them spread out a bit.
            if (distanceToMe < distanceColliding && ViolentNightUtils.HasLineOfSight(targetCenter, world - new Vector2(0, NPC.Hitbox.Height / 2)))
            {
                closestColliding = node;
                distanceColliding = distanceToMe;
            }
        }

        if (closestColliding != fallback)
            return closestColliding;
        else if (closest != fallback)
            return closest;
        else
            return allNodes.ElementAt(index);
    }

    public override void OnKill()
    {
        handle.Dispose();
    }
}
