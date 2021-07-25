using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int size)
        {
            return items
                .Select((item, index) => (Value: item, GroupIndex: index / size ))
                .GroupBy(item => item.GroupIndex)
                .Select(group => group.Select(item => item.Value));
        }
    }
}
