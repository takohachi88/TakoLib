using System;

namespace TakoLib.Common.Extensions
{
    public static class CommonExtensions
    {
        public static bool IsInRange<T>(this T value, T from, T to) where T : IComparable
            => (0 <= value.CompareTo(from)) && (value.CompareTo(to) <= 0);

        public static bool ToBool(this int value) => Convert.ToBoolean(value);
        public static int ToInt(this bool value) => Convert.ToInt32(value);
        public static short ToShort(this bool value) => Convert.ToInt16(value);
    }
}
