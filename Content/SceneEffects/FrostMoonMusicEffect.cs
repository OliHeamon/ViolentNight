using Terraria;
using Terraria.ModLoader;

namespace ViolentNight.Content.SceneEffects;

public sealed class FrostMoonMusicEffect : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/FrostMoon");

    public override bool IsSceneEffectActive(Player player)
    {
        return Main.snowMoon;
    }
}
