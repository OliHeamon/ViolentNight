using Newtonsoft.Json.Linq;
using System;

namespace ViolentNight.Systems.Data;

public interface IDataManager<T> where T : struct
{
    string Extension { get; }

    void Populate(ReadOnlySpan<JObject> inputs, Span<T> outputs);
}
