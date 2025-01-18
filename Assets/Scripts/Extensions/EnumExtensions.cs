using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumExtensions {
    /// <returns> A dictionary where the keys are enum string names and values are their corresponding enum values </returns>
    public static Dictionary<string, TEnum> ToDictionary<TEnum>(this TEnum _) where TEnum : Enum
    {
        var names = Enum.GetNames(typeof(TEnum));
        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
        return names.Zip(values, (name, value) => new { name, value })
                    .ToDictionary(x => x.name, x => x.value);
    }
}