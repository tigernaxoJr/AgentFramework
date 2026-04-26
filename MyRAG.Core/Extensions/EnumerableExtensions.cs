using System;
using System.Collections.Generic;

namespace MyRAG.Core.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// 根據聚合值 (Aggregate) 和條件 (Predicate) 將序列分塊。
    /// 常用於基於總權重 (例如：Token 數量) 來批次處理項目。
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
