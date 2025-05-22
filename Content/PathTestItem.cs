using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ViolentNight.Content.NPCs.Elves;

namespace ViolentNight.Content;

public class PathTestItem : ModItem
{
    public override string Texture => "ViolentNight/icon";

    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.useStyle = ItemUseStyleID.MowTheLawn;
        Item.useAnimation = 20;
        Item.useTime = 20;
        Item.autoReuse = false;
    }

    public override bool? UseItem(Player player)
    {
        Projectile.NewProjectile(Item.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<PathTestProj>(), 0, 0);

        return true;
    }
}

public class PathTestProj :  ModProjectile
{
    public override string Texture => "ViolentNight/icon";

    public override void SetDefaults()
    {
        Projectile.tileCollide = false;
        Projectile.timeLeft = 1;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;

        Vector2 position = Main.MouseWorld;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC.NewNPC(Projectile.GetSource_FromAI(),
                (int)(position.X),
                (int)(position.Y),
                ModContent.NPCType<Elfantryman>()
            );
        }
    }
}
