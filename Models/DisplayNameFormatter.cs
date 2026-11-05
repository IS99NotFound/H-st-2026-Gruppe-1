using System.Globalization;

namespace H_st_2026_Gruppe_1.Models;

public static class DisplayNameFormatter
{
    public static string Format(string name)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Trim().ToLowerInvariant());
    }
}
