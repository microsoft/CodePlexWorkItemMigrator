using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns an empty enumerable if collection is null.
        /// </summary>
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> collection) =>
            object.ReferenceEquals(collection, null) ? Enumerable.Empty<T>() : collection;
    }
}
