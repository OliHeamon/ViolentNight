using Newtonsoft.Json.Linq;

namespace ViolentNight.Systems.Data;

public abstract class DataManager<T> : IDataManager where T : IDataFile
{
    public abstract string Extension { get; }

    public abstract T LoadData(JObject data);

    IDataFile IDataManager.LoadData(JObject data)
    {
        return LoadData(data);
    }
}
