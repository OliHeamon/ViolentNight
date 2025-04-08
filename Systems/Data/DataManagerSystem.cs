using Hjson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;

namespace ViolentNight.Systems.Data;

public sealed class DataManagerSystem : ModSystem
{
    private static readonly List<IDataManager> dataManagers = [];

    private static readonly Dictionary<Type, IDataManager> typeToManager = [];

    public static void Register(IDataManager manager)
    {
        dataManagers.Add(manager);
    }

    public override void OnModLoad()
    {
        IEnumerable<string> allDataFiles = Mod.GetFileNames().Where(f => f.StartsWith("Data"));

        foreach (IDataManager manager in dataManagers)
        {
            string[] targets = allDataFiles.Where(f => f.EndsWith(manager.Extension)).ToArray();

            JObject[] files = new JObject[targets.Length];

            for (int i = 0; i < files.Length; i++)
            {
                string file = targets[i];

                // Hjson loading code is adapted from Terraria Overhaul.
                using Stream stream = Mod.GetFileStream(file);
                using StreamReader streamReader = new(stream);
                string hjsonText = streamReader.ReadToEnd();

                string jsonText = HjsonValue.Parse(hjsonText).ToString(Stringify.Plain);
                JObject json = JObject.Parse(jsonText);

                files[i] = json;
            }

            manager.Populate(files);

            Type dataFileType = manager.GetType().BaseType.GetGenericArguments()[0];

            typeToManager[dataFileType] = manager;
        }
    }

    public static ReadOnlySpan<T> GetAllDataOfType<T>() where T : struct
    {
        if (!typeToManager.TryGetValue(typeof(T), out IDataManager manager))
        {
            throw new FileLoadException($"Unsupported data file type: {typeof(T).FullName}");
        }

        return ((DataManager<T>)manager).Data;
    }
}
