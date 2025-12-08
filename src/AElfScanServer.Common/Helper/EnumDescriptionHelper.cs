using System;
using System.ComponentModel;
using AElfScanServer.Common.Enums;

namespace AElfScanServer.Common.Helper;

public class EnumDescriptionHelper
{
    public static string GetEnumDescription(TokenCreatedExternalInfoEnum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
}