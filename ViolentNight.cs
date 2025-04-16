using Terraria.ModLoader;
using ViolentNight.Systems.Data;
using ViolentNight.Systems.Data.DataFileTypes;

namespace ViolentNight;

public class ViolentNight : Mod
{
    public override void Load()
    {
        DataManager.Register<FactionDataManager>();
        DataManager.Register<TargetingDataManager>();
    }
}
