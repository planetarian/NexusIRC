// BingQueryable.cs
//

using System.Linq.Expressions;
using System.Reflection;

namespace BLinq {

    public static class BingQueryable {

        public static IBingQueryable<T> SafeResults<T>(this IBingQueryable<T> source) {
            MethodInfo safeResultsMethod = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) });
            Expression safeResultsExpression = Expression.Call(safeResultsMethod, source.Expression);

            return (IBingQueryable<T>)source.Provider.CreateQuery<T>(safeResultsExpression);
        }

        public static IBingQueryable<T> LocalResults<T>(this IBingQueryable<T> source, string location) {
            MethodInfo localResultsMethod = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) });
            Expression localResultsExpression =
                Expression.Call(localResultsMethod,
                                source.Expression, Expression.Constant(location));

            return (IBingQueryable<T>)source.Provider.CreateQuery<T>(localResultsExpression);
        }

        public static IBingQueryable<T> ScopeResults<T>(this IBingQueryable<T> source, string domain) {
            MethodInfo localResultsMethod = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) });
            Expression localResultsExpression =
                Expression.Call(localResultsMethod,
                                source.Expression, Expression.Constant(domain));

            return (IBingQueryable<T>)source.Provider.CreateQuery<T>(localResultsExpression);
        }
    }
}
