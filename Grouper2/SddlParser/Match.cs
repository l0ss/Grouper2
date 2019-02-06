using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Grouper2.SddlParser
{
    internal static class Match
    {
        public static LinkedList<string> ManyByPrefix(string input, IDictionary<string, string> tokensToLabels, out string reminder)
        {
            LinkedList<string> labels = new LinkedList<string>();

            reminder = SubstituteEmptyWithNull(input);
            while (reminder != null)
            {
                string label = OneByPrefix(reminder, tokensToLabels, out reminder);

                if (label != null)
                    labels.AddLast(label);
                else
                    break;
            }

            return labels;
        }

        public static LinkedList<string> ManyByUint(uint mask, IDictionary<uint, string> tokensToLabels, out uint reminder)
        {
            LinkedList<string> labels = new LinkedList<string>();

            reminder = mask;
            while (reminder > 0)
            {
                string label = OneByUint(reminder, tokensToLabels, out reminder);

                if (label != null)
                    labels.AddLast(label);
                else
                    break;
            }

            return labels;
        }

        public static string OneByPrefix(string input, IDictionary<string, string> tokensToLabels, out string reminder)
        {
            foreach (KeyValuePair<string, string> kv in tokensToLabels)
            {
                if (input.StartsWith(kv.Key))
                {
                    reminder = SubstituteEmptyWithNull(input.Substring(kv.Key.Length));
                    return kv.Value;
                }
            }

            reminder = input;
            return null;
        }

        public static string OneByUint(uint mask, IDictionary<uint, string> tokensToLabels, out uint reminder)
        {
            foreach (KeyValuePair<uint, string> kv in tokensToLabels)
            {
                if ((mask & kv.Key) == kv.Key)
                {
                    reminder = mask - kv.Key;
                    return kv.Value;
                }
            }

            reminder = mask;
            return null;
        }

        public static string OneByRegex(string input, IDictionary<string, string> tokensToLabels)
        {
            foreach (KeyValuePair<string, string> kv in tokensToLabels)
            {
                if (Regex.IsMatch(input, kv.Key))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        private static string SubstituteEmptyWithNull(string input)
        {
            return input == string.Empty ? null : input;
        }
    }
}