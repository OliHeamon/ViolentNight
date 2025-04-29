using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using ViolentNight.Systems.AI;

namespace ViolentNight.Content.NPCs.Elves;

public sealed class Elfantryman : ViolentNightNPC
{
    public override void SetDefaults()
    {
        NPC.width = 20;
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

        spriteBatch.Draw(texture, NPC.position - screenPos + new Vector2(-4, 6), NPC.frame, drawColor, NPC.rotation, NPC.Size / 2, NPC.scale, NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

        return false;
    }

    public override AIContainer InitialiseAI()
    {
        return new AIContainer("Idle")
            .AddState(new("Idle", IdleAI));
    }

    private void IdleAI()
    {
        NPC.ai[0]++;

        if (NPC.ai[0] >= 60)
        {
            NPC.ai[0] = 0;
            NPC.direction *= -1;
        }
    }
}
