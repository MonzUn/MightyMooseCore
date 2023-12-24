using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Eco.EW.Utils
{
    public static class Text
    {
        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");

        public class Color
        {
            public static string Green(string msg) => $"<color=green>{msg}</color>";
            public static string Yellow(string msg) => $"<color=yellow>{msg}</color>";
            public static string Red(string msg) => $"<color=red>{msg}</color>";
        }

        public static string StripTags(string toStrip)
        {
            if (toStrip == null)
                return string.Empty;

            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
