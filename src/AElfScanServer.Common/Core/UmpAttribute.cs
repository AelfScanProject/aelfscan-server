using System;

namespace AElfScanServer.Common.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UmpAttribute : Attribute
{
    public UmpAttribute()
    {
    }
}