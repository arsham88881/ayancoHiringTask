using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Domain.Attributes.Shared;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsonSkipNullAttribute : Attribute { }

public class SkipNullJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);
        if (typeInfo.Kind == JsonTypeInfoKind.Object)
            foreach (JsonPropertyInfo property in typeInfo.Properties)
                if (property.AttributeProvider?.IsDefined(typeof(JsonSkipNullAttribute), false) == true)
                    property.ShouldSerialize = (obj, value) => value != null; //skip when null

        return typeInfo;
    }
}