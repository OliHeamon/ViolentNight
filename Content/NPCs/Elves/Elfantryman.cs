using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using ViolentNight.Systems.AI;
using ViolentNight.Systems.Pathfinding;

namespace ViolentNight.Content.NPCs.Elves;

public sealed class Elfantryman : ViolentNightNPC
{
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

        NPC.value = 60f;

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

        if (path != null)
        {
            foreach (Edge edge in path.edgesToTraverse)
            {
                region?.DrawEdge(edge);
            }
        }

        return false;
    }

    public override AIContainer InitialiseAI()
    {
        AIContainer container = new AIContainer("Idle")
            .AddState(new("Idle", IdleAI))
            .AddState(new("Navigating", NavigatingAI))
            .AddState(new("Attack", AttackAI))
            .AddState(new("Jumping", JumpingAI));

        container.OnStateChanged += () =>
        {
            AnimationVariant = 0;
            Timer = 0;
        };

        return container;
    }

    private void IdleAI()
    {
        NPC.velocity.X *= 0.95f;

        Timer++;

        if (Timer >= 150)
        {
            Timer = 0;
            NPC.direction *= -1;
        }

        NPC.spriteDirection = -NPC.direction;

        if (Target != null)
        {
            AIState.TransitionTo("Navigating");
        }
    }

    private MappedRegion region;
    private Pathfinder path;

    private bool hasPath;

    private Point targetPoint;
    private EdgeType navigatingEdgeType;

    private const float maxXVelocity = 3;

    private void NavigatingAI()
    {
        if (region == null)
        {
            region = new(40, NPC.Hitbox, 8, 10);
            path = new Pathfinder(region, SelectDestination);
            region.Remap(NPC.Center);
        }

        if (!hasPath) 
        {
            bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out List<Point> tiles);

            if (!tilesBelow)
                return;

            foreach (Point node in tiles)
            {
                bool started = path.StartPath(node, out bool alreadyAtGoal);

                if (alreadyAtGoal)
                {
                    AIState.TransitionTo("Attack");
                    return;
                }

                if (started)
                {
                    hasPath = true;

                    path.TryGetNextNavPoint(out targetPoint, out navigatingEdgeType);

                    break;
                }
            }
        }
        else
        {
            switch (navigatingEdgeType)
            {
                case EdgeType.Fall:
                case EdgeType.Walk:
                    Vector2 targetTileLocation = new Vector2(targetPoint.X * 16, targetPoint.Y * 16) + new
                         Vector2(8, 8);

                    int walkDirection = Math.Sign(targetTileLocation.X - NPC.Center.X);

                    NPC.velocity.X += 0.2f * walkDirection;

                    if (Math.Abs(NPC.velocity.X) > maxXVelocity)
                    {
                        NPC.velocity.X = maxXVelocity * walkDirection;
                    }

                    if (navigatingEdgeType == EdgeType.Fall && NPC.velocity.Y != 0)
                    {
                        NPC.velocity.X *= 0.5f;
                    } 

                    bool xClose = Math.Abs(targetTileLocation.X - NPC.Hitbox.Bottom().X) < 2;
                    bool goalReachedCondition = navigatingEdgeType == EdgeType.Walk ?
                        xClose :
                        xClose && Math.Abs(targetTileLocation.Y - NPC.Hitbox.Bottom().Y) < 16;


                    if (goalReachedCondition)
                    {
                        // Reached the end of the path.
                        if (!path.TryGetNextNavPoint(out targetPoint, out navigatingEdgeType))
                        {
                            PathLost();

                            AIState.TransitionTo("Attack");

                            return;
                        }
                    }

                    bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out List<Point> tiles);

                    if (!tilesBelow)
                    {
                        PathLost();
                        return;
                    }
                    else
                    {
                        bool contains = false;

                        foreach (Point node in tiles)
                        {
                            if (region.PointToNodeId.ContainsKey(node))
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

                    break;
                case EdgeType.Jump:
                    AIState.TransitionTo("Jumping");
                    break;
            }
        }
    }

    private bool jumped;
    private int jumpDirection;

    private void JumpingAI()
    {
        if (!jumped)
        {
            Vector2 start = NPC.Hitbox.Bottom();

            // Top-middle of target tile.
            Vector2 end = new Vector2(targetPoint.X * 16, targetPoint.Y * 16) + new Vector2(8, 0);

            Vector2 velocity = DyBasedSpeed(start, end, out bool valid);

            // If a jump is less than 3 tiles horizontally, use the Y-based method instead.
            if (end.X - start.X < 16 * 3 && valid)
            {
                NPC.velocity = velocity;
            }
            else
            {
                NPC.velocity = DxBasedSpeed(start, end);
            }

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

            bool tilesBelow = ViolentNightUtils.GetTilesBelow(NPC.Hitbox, out List<Point> tiles);

            // There are tiles that the NPC can stand on.
            if (tilesBelow)
            {
                // Setting this to false means the jump is over.
                jumped = false;

                bool contains = false;

                foreach (Point node in tiles)
                {
                    if (region.PointToNodeId.ContainsKey(node))
                    {
                        contains = true;
                    }
                }

                // If the tiles stood on are not in the navigation map, calculate a new path.
                if (!contains)
                {
                    AIState.TransitionTo("Navigating");

                    PathLost();

                    return;
                }

                // If the tiles below the NPC contain the target point, then we successfully reached the jump node.
                if (tiles.Contains(targetPoint))
                {
                    // Reached the end of the path.
                    if (!path.TryGetNextNavPoint(out targetPoint, out navigatingEdgeType))
                    {
                        PathLost();

                        AIState.TransitionTo("Attack");

                        return;
                    }
                }
                else
                {
                    PathLost();
                }

                AIState.TransitionTo("Navigating");
            }

            return;
        }
    }

    public override bool PreAI()
    {
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

        return true;
    }

    private Vector2 DxBasedSpeed(Vector2 start, Vector2 end)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;

        // Jump at max X speed.
        float uX = maxXVelocity * Math.Sign(dx);

        // Time of flight in ticks.
        int t = (int)(dx / uX);

        float g = NPC.gravity;

        float uY = (dy - (0.5f * g * t * t)) / t;
        uY -= 0.1f;

        return new Vector2(uX, uY);
    }

    private Vector2 DyBasedSpeed(Vector2 start, Vector2 end, out bool valid)
    {
        valid = true;

        float dx = end.X - start.X;
        float dy = end.Y - start.Y;

        // Initial Y velocity.
        float uY = -(float)Math.Sqrt(2 * NPC.gravity * -dy) - NPC.gravity;

        float t = PositiveQuadraticRoot(0.5f * NPC.gravity, -uY, dy);

        if (float.IsNaN(t) || t < 0)
        {
            valid = false;
            return Vector2.Zero;
        }

        float uX = Math.Min(dx / t, maxXVelocity);

        return new Vector2(uX, uY);
    }

    private float PositiveQuadraticRoot(float a, float b, float c)
    {
        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0)
        {
            return float.NaN;
        }

        float x1 = (-b + (float)Math.Sqrt(discriminant)) / (2 * a);
        float x2 = (-b - (float)Math.Sqrt(discriminant)) / (2 * a);

        return Math.Max(x1, x2);
    }

    private void PathLost()
    {
        NPC.velocity.X *= 0.75f;
        hasPath = false;
        region.Remap(NPC.Center);
    }

    private Point SelectDestination(List<Point> allNodes)
    {
        Point fallback = allNodes[0];

        Point closest = fallback;
        float closestToPlayer = float.PositiveInfinity;

        Point closestColliding = fallback;
        float distanceColliding = float.PositiveInfinity;

        // Pick a random node if there's no target.
        if (Target == null)
        {
            return Main.rand.Next(allNodes);
        }

        foreach (Point node in allNodes)
        {
            Vector2 world = new Vector2(node.X * 16, node.Y * 16) + new Vector2(8);

            Vector2 targetCenter = Target.Value.Hitbox.Center();

            float distance = Vector2.Distance(world, targetCenter);
            float distanceToMe = Vector2.Distance(world, NPC.Center);

            if (distance < closestToPlayer)
            {
                closest = node;
                closestToPlayer = distance;
            }

            // If a line of sight point is found, prioritise that one, otherwise return the node closest to the player.
            // LOS points can't be navigated to if an NPC is already nearby, so as to help them spread out a bit.
            if (distanceToMe < distanceColliding && Collision.CanHitLine(targetCenter, 0, 0, world - new Vector2(0, NPC.Hitbox.Height / 2), 0, 0))
            {
                closestColliding = node;
                distanceColliding = distanceToMe;
            }
        }

        return closestColliding == fallback ? closest : closestColliding;
    }

    private void AttackAI()
    {
        NPC.velocity.X *= 0.8f;

        if (Target == null)
        {
            AIState.TransitionTo("Idle");
            return;
        }

        Target target = Target.Value;

        if (Timer++ > 60)
        {
            if (!Collision.CanHitLine(target.Hitbox.Center(), 0, 0, NPC.Bottom - new Vector2(0, NPC.Hitbox.Height / 2), 0, 0))
            {
                AIState.TransitionTo("Idle");
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

        // Inverse lerp: 0 is full down, 1 is full up.
        float t = (angle - a) / (b - a);

        AnimationVariant = (int)(t * 5);

        return;

    }
}
