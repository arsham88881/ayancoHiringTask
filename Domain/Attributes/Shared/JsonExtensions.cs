using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Domain.Attributes.Shared;


public static class JsonExtensions
{
    public static string? ObjectToJsonString<T>(this T model)
    {
        if (model == null) return null;
        try
        {
            return JsonConvert.SerializeObject(model,
                Newtonsoft.Json.Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
        }
        catch
        {
            return null;
        }
    }

    public static object? ObjectToJsonObject<T>(this T model)
    {
        return model == null ? null : JsonConvert.DeserializeObject(model.ObjectToJsonString());
    }

    public static T? JsonToModel<T>(this string json)
    {
        var typeTOutput = typeof(T);
        return string.IsNullOrWhiteSpace(json)
            ? default(T)
            : typeTOutput == typeof(string)
                ? (T)Convert.ChangeType(json, typeof(T))
                : JsonConvert.DeserializeObject<T>(json);
    }

    public static List<T>? JsonToListOfModel<T>(this string json)
    {
        var typeTOutput = typeof(T);
        return string.IsNullOrWhiteSpace(json)
            ? new List<T>()
            : typeTOutput == typeof(string)
                ? (List<T>)Convert.ChangeType(json, typeof(List<T>))
                : JsonConvert.DeserializeObject<List<T>>(json);
    }

    public static object? JsonStringToJsonObject(this string xJson)
    {
        return string.IsNullOrWhiteSpace(xJson) ? null : JsonConvert.DeserializeObject(xJson, typeof(object));
    }
}