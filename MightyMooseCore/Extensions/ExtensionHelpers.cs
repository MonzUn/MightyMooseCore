using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Eco.Moose.Extensions
{
    public static class ExtensionHelpers
    {
        public static string GetTagStrippedObjectName(object obj, string fallback)
        {
            string name = GetObjectName(obj);
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            return name.StripTags();
        }

        public static string GetObjectName(object obj)
        {
            if (obj == null)
                return string.Empty;

            PropertyInfo? markedUpName = obj.GetType().GetProperty("MarkedUpName");
            object? markedUpValue = markedUpName?.GetValue(obj);
            if (markedUpValue != null)
                return markedUpValue.ToString();

            PropertyInfo? displayName = obj.GetType().GetProperty("DisplayName");
            object? displayNameValue = displayName?.GetValue(obj);
            if (displayNameValue is string displayNameString && !string.IsNullOrWhiteSpace(displayNameString))
                return displayNameString;

            PropertyInfo? name = obj.GetType().GetProperty("Name");
            object? nameValue = name?.GetValue(obj);
            if (nameValue is string nameString && !string.IsNullOrWhiteSpace(nameString))
                return nameString;

            return obj.ToString();
        }
    }
}
