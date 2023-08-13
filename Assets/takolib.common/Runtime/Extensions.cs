using System;

namespace TakoLib.Common.Extensions
{
    public static class Extensions
    {
        public static bool IsInRange<T>(this T value, T from, T to) where T : IComparable
            => (0 <= value.CompareTo(from)) && (value.CompareTo(to) <= 0);
    }
}
