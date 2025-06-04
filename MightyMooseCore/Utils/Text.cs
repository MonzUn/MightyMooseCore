using Eco.Shared.Utils;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Eco.Moose.Utils.TextUtils
{
    public static class TextUtils
    {
        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");

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

        public static int CalculateStringSimilarityScore(string source, string target, bool prioritizeContainingStrings = true)
        {
            const int CONTAINS_MULTIPLIER = 100;
            int score = Algorithms.DamerauLevenshteinDistance.CalculateDamerauLevenshteinDistance(source, target);
            if (prioritizeContainingStrings && target.ContainsCaseInsensitive(source))
                score -= (source.Length * CONTAINS_MULTIPLIER); // Prioritize strings that partially contain the source
            return score;
        }
    }
}