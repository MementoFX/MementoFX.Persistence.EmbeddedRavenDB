using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.EmbeddedRavenDB
{
    static class LinqExtensions
    {
        public static IList ToAnonymousList(this IEnumerable source)
        {
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new ArgumentException("Can't create a list from an empty sequence", nameof(source));

            var value = enumerator.Current;
            var returnList = (IList)typeof(List<>)
                .MakeGenericType(value.GetType())
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null);

            returnList.Add(value);

            while (enumerator.MoveNext())
            {
                returnList.Add(enumerator.Current);
            }

            return returnList;
        }

        public static IList ToAnonymousList(this IQueryable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var returnList = (IList)typeof(List<>)
                .MakeGenericType(source.ElementType)
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null);

            foreach (var elem in source)
            {
                returnList.Add(elem);
            }

            return returnList;
        }
    }
}
