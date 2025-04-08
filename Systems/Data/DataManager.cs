using Hjson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace ViolentNight.Systems.Data;

public sealed class DataManager : ModSystem
{
    private static string[] dataFiles;

    private static readonly Dictionary<Type, Type> dataFileTypeToManagerType = [];

    public static void Register<T>()
    {
        Type dataFileType = typeof(T).GetInterfaces()[0].GetGenericArguments()[0];

        dataFileTypeToManagerType[dataFileType] = typeof(T);
    }

    public override void OnModLoad()
    {
        dataFiles = Mod.GetFileNames().Where(f => f.StartsWith("Data")).ToArray();

        foreach (Type type in dataFileTypeToManagerType.Keys)
        {
            RegisterDataType(type);
        }
    }

    public static void RegisterDataType(Type type)
        => RuntimeHelpers.RunClassConstructor(typeof(DataStorage<>).MakeGenericType(type).TypeHandle);

    public static ReadOnlySpan<T> GetAllDataOfType<T>() where T : struct => DataStorage<T>.Data;

    private static class DataStorage<T> where T : struct
    {
        public static readonly T[] Data;

        static DataStorage()
        {
            if (dataFiles == null)
            {
                return;
            }

            Type managerType = dataFileTypeToManagerType[typeof(T)];

            IDataManager<T> manager = Activator.CreateInstance(managerType) as IDataManager<T>;

            string[] targets = dataFiles.Where(f => f.EndsWith(manager.Extension)).ToArray();

            JObject[] files = new JObject[targets.Length];

            for (int i = 0; i < files.Length; i++)
            {
                string file = targets[i];

                // Hjson loading code is adapted from Terraria Overhaul.
                using Stream stream = ModContent.GetInstance<ViolentNight>().GetFileStream(file);
                using StreamReader streamReader = new(stream);
                string hjsonText = streamReader.ReadToEnd();

                string jsonText = HjsonValue.Parse(hjsonText).ToString(Stringify.Plain);
                JObject json = JObject.Parse(jsonText);

                files[i] = json;
            }

            Data = new T[files.Length];

            manager.Populate(files, Data);
        }
    }
}
