using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ViolentNight.Systems;

public sealed class FrostMoonAnnouncementSystem : ModSystem
{
    public override void Load()
    {
        IL_Main.startSnowMoon += EditStartText;
    }

    // Customises the wave 1 text.
    private void EditStartText(ILContext il)
    {
        ILCursor c = new(il);

        ILLabel label = c.DefineLabel();

        // Skip all instructions involved in calling Terraria.Lang::GetInvasionWaveText.
        if (!c.TryGotoNext(MoveType.After, i => i.MatchStsfld(typeof(NPC).GetField("waveNumber", BindingFlags.Public | BindingFlags.Static))))
        {
            throw new Exception("Could not find waveNumber.");
        }
        c.Emit(OpCodes.Br, label);

        // Add a label just after the Terraria.Lang::GetInvasionWaveText call, but before stloc.0.
        if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(Lang).GetMethod("GetInvasionWaveText", BindingFlags.Public | BindingFlags.Static))))
        {
            throw new Exception("Could not find NetworkText.");
        }
        c.MarkLabel(label);

        // Emit new NetworkText.
        c.EmitDelegate(() =>
        {
            return NetworkText.FromKey("Mods.ViolentNight.StartMessage");
        });
    }
}
