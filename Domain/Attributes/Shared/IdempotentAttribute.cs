using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Attributes.Shared;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute
{
    public int CacheTimeInMinutes { get; }

    public IdempotentAttribute(int cacheTimeInMinutes = 60)
    {
        CacheTimeInMinutes = cacheTimeInMinutes;
    }
}