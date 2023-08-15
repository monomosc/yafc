using System.Collections.Generic;
using System.Linq;

namespace YAFC;

public static class LinqExtensions {

    public static IEnumerable<(T, int)> Enumerate<T>(this IEnumerable<T> enumerable) {
        var i = 0;
        foreach (var item in enumerable) {
            yield return (item, i++);
        }
    }
}