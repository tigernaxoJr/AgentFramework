using System;
using System.Collections.Generic;

namespace MyRAG.Core.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Chunks a sequence based on an aggregate value and a predicate.
    /// Useful for batching items based on a total weight (e.g., token count).
    /// </summary>
    public static IEnumerable<List<TSource>> ChunkByAggregate<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> aggregator,
        Func<TAccumulate, int, bool> predicate)
    {
        using var enumerator = source.GetEnumerator();
        var aggregate = seed;
        var index = 0;
        var chunk = new List<TSource>();

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            aggregate = aggregator(aggregate, current);

            if (predicate(aggregate, index++))
            {
                chunk.Add(current);
            }
            else
            {
                if (chunk.Count > 0)
                {
                    yield return chunk;
                }

                chunk = [current];
                aggregate = aggregator(seed, current);
                index = 1;
            }
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }
}
