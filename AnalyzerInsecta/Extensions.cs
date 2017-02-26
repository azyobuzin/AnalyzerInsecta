using System;
using System.Collections.Immutable;

namespace AnalyzerInsecta
{
    internal static class Extensions
    {
        public static TResult[] ToArray<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, TResult> selector)
        {
            if (source.IsDefaultOrEmpty) return new TResult[0];

            var result = new TResult[source.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = selector(source[i]);

            return result;
        }
    }
}
