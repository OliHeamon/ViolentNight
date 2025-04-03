using Terraria;
using Terraria.ModLoader;

namespace ViolentNight.Content.Systems;

public sealed class FrostMoonManager : ModSystem
{
    public override void Load()
    {
        On_Main.startSnowMoon += InitialiseFrostMoonDetour;
        On_Main.stopMoonEvent += ResetFrostMoonDetour;
    }

    private void EventStart()
    {
    }

    private void EventEnd()
    {
    }

    private void InitialiseFrostMoonDetour(On_Main.orig_startSnowMoon orig)
    {
        orig();

        EventStart();
    }

    private void ResetFrostMoonDetour(On_Main.orig_stopMoonEvent orig)
    {
        if (Main.snowMoon)
        {
            EventEnd();
        }

        orig();
    }
}
