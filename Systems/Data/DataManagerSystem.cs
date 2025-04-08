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

    private static readonly Dictionary<Type, IDataFile[]> data = [];

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

            IDataFile[] files = new IDataFile[targets.Length];

            for (int i = 0; i < files.Length; i++)
            {
                string file = targets[i];

                // Hjson loading code is adapted from Terraria Overhaul.
                using Stream stream = Mod.GetFileStream(file);
                using StreamReader streamReader = new(stream);
                string hjsonText = streamReader.ReadToEnd();

                string jsonText = HjsonValue.Parse(hjsonText).ToString(Stringify.Plain);
                JObject json = JObject.Parse(jsonText);

                files[i] = manager.LoadData(json);
            }

            Type dataFileType = manager.GetType().BaseType.GetGenericArguments()[0];

            data[dataFileType] = files;
        }
    }

    public static T[] GetAllDataOfType<T>() where T : IDataFile
    {
        if (!data.TryGetValue(typeof(T), out IDataFile[] files))
        {
            throw new FileLoadException($"Unsupported data file type: {typeof(T).FullName}");
        }

        return files.Cast<T>().ToArray();
    }
}
