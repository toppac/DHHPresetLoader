using System;
using System.Collections.Generic;
using System.Text;

namespace DHHPresetLoader
{
    public static class Extensions
    {
        public static bool EqualsCase(this string source, string dest)
        {
            return string.Equals(source, dest, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsCase(this string source, string dest)
        {
            return ContainsCase(source, dest, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsCase(string source, string dest, StringComparison comparison)
        {
            return source.IndexOf(dest, comparison) >= 0;
        }
    }
}
