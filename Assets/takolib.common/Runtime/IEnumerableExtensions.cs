using System.Collections.Generic;
using System;
using System.Linq;

namespace TakoLib.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// .Net6のMaxByがUnityで使えないので自前実装。
        /// </summary>
        public static TSource MaxBy<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            var value = source.Max(selector);
            return source.First(c => selector(c).Equals(value));
        }

        /// <summary>
        /// .Net6のMinByがUnityで使えないので自前実装。
        /// </summary>
        public static TSource MinBy<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            var value = source.Min(selector);
            return source.First(c => selector(c).Equals(value));
        }
    }
}
