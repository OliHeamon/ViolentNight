using Newtonsoft.Json.Linq;

namespace ViolentNight.Systems.Data;

public interface IDataManager
{
    public string Extension { get; }

    public IDataFile LoadData(JObject data);
}
