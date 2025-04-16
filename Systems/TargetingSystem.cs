using System;
using Terraria.ModLoader;
using ViolentNight.Systems.Data;
using ViolentNight.Systems.Data.DataFileTypes;

namespace ViolentNight.Systems;

public sealed class TargetingSystem : ModSystem
{
    public override void PostSetupContent()
    {
        ReadOnlySpan<TargetingData> definitions = DataManager.GetAllDataOfType<TargetingData>();
    }
}
