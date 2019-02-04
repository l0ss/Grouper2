using System;
using System.Text;

namespace Grouper2.SddlParser
{
    internal static class StringBuilderExtensions
    {
        private static readonly string IndentString = "  ";
        private static readonly string IndentNewLine = $"{Environment.NewLine}{IndentString}";

        public static StringBuilder AppendIndentEnv(this StringBuilder sb, string value)
        {
            string indentedValue = $"{IndentString}{value.Replace(Environment.NewLine, IndentNewLine)}";
            return sb.AppendLineEnv(indentedValue.TrimEnd());
        }

        public static StringBuilder AppendLineEnv(this StringBuilder sb, string value)
        {
            return sb.AppendFormat("{0}{1}", value, Environment.NewLine);
        }
    }
}