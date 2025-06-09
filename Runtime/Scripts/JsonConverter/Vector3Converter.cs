using UnityEngine;
using System;
using Valve.Newtonsoft.Json;

internal class Vector3Converter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not Vector3 val) return;
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(val.x);
        writer.WritePropertyName("y");
        writer.WriteValue(val.y);
        writer.WritePropertyName("z");
        writer.WriteValue(val.z);
        writer.WriteEndObject();
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        float x = 0f, y = 0f, z = 0f;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject) break;

            if (reader.TokenType == JsonToken.PropertyName)
            {
                string propName = (string)reader.Value;
                reader.Read();

                switch (propName)
                {
                    case "x": x = Convert.ToSingle(reader.Value); break;
                    case "y": y = Convert.ToSingle(reader.Value); break;
                    case "z": z = Convert.ToSingle(reader.Value); break;
                }
            }
        }

        return new Vector3(x, y, z);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3);
    }
}
