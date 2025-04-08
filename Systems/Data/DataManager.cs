using Newtonsoft.Json.Linq;

namespace ViolentNight.Systems.Data;

public abstract class DataManager<T> : IDataManager where T : struct
{
    public T[] Data { get; set; } = [];

    public abstract string Extension { get; }

    protected abstract T LoadData(JObject data);

    public void Populate(JObject[] data)
    {
        Data = new T[data.Length];

        for (int i  = 0; i < data.Length; i++)
        {
            Data[i] = LoadData(data[i]);
        }
    }
}

public interface IDataManager
{
    public string Extension { get; }

    public void Populate(JObject[] data);
}
