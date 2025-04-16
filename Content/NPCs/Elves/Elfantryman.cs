using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace ViolentNight.Content.NPCs.Elves;

public sealed class Elfantryman : ModNPC
{
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 10;
    }

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

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        NPC.frameCounter %= 10 * 4;

        Rectangle frame = new(0, (int)((NPC.frameCounter / 10) + 6) * 54, 48, 54);

        NPC.frame = frame;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

        spriteBatch.Draw(texture, NPC.position - screenPos + new Vector2(-4, 6), NPC.frame, drawColor, NPC.rotation, NPC.Size / 2, NPC.scale, NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

        return false;
    }
}
