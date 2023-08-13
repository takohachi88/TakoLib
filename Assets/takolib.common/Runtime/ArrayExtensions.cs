using System;
using System.Collections.Generic;

namespace TakoLib.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty<T>(this ICollection<T> self) => self == null || self.Count == 0;

        public static void Swap<T>(this IList<T> self, int i, int j)
        {
            (self[i], self[j]) = (self[j], self[i]);
        }

        public static bool IsInRange<T>(this IList<T> self, int index) => index.IsInRange(0, self.Count - 1);
    }
}
