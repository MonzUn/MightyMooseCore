using System.ComponentModel;
using System.Reflection;

namespace Eco.EW.Utils
{
    public static class Text
    {
        public class Color
        {
            public static string Green(string msg) => $"<color=green>{msg}</color>";
            public static string Yellow(string msg) => $"<color=yellow>{msg}</color>";
            public static string Red(string msg) => $"<color=red>{msg}</color>";
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
