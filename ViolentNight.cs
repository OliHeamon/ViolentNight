using ReLogic.Content.Sources;
using Terraria.ModLoader;

namespace ViolentNight;

public sealed class ViolentNight : Mod
{
    public override IContentSource CreateDefaultContentSource()
    {
        SmartContentSource source = new(base.CreateDefaultContentSource());

        // Redirects requests for ModName/Content/... to ModName/Assets/...
        source.AddDirectoryRedirect("Content", "Assets");

        return source;
    }
}
