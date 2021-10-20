using System;
using System.Collections.Generic;

namespace CommonInfrastructure
{
    public static class EnumerableExtension
    {
        /// <summary> Execute action for each element of enumeration </summary>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var v in enumerable)
                action(v);
        }
    }
}