using System;
using System.Collections.Generic;

namespace AElfScanServer.Common.Helper;

public class EnumConverter
{
    public static T ConvertToEnum<T>(string value) where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (Enum.TryParse<T>(value, true, out T result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid value for enum {typeof(T).Name}: {value}");
    }
    
    
    public static List<T> GetEnumValuesList<T>() where T : Enum
    {
        var enumValues = Enum.GetValues(typeof(T));
        List<T> enumList = new List<T>(enumValues.Length);
        
        foreach (var value in enumValues)
        {
            enumList.Add((T)value);
        }
        
        return enumList;
    }
}