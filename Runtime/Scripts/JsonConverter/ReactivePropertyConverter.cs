using System;
using R3;
using Valve.Newtonsoft.Json;

internal class ReactivePropertyConverter<T> : JsonConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not ReactiveProperty<T> val) return;
        serializer.Serialize(writer, val.Value);
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var val = serializer.Deserialize<T>(reader);
        return new SerializableReactiveProperty<T>(val);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ReactiveProperty<T>);
    }
}
