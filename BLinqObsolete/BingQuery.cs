// BingQuery.cs
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BLinq {

    internal sealed class BingQuery<T> : IBingQueryable<T> {

        private BingQueryProvider _queryProvider;
        private Expression _expression;

        public BingQuery(BingQueryProvider queryProvider) {
            _queryProvider = queryProvider;
            _expression = Expression.Constant(this);
        }

        public BingQuery(BingQueryProvider queryProvider, Expression expression) {
            _queryProvider = queryProvider;
            _expression = expression;
        }

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
        #endregion

        #region IEnumerable<T> Members
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            IEnumerable<T> sequence = _queryProvider.Execute<IEnumerable<T>>(_expression);
            return sequence.GetEnumerator();
        }
        #endregion

        #region IQueryable Members
        Type IQueryable.ElementType {
            get {
                return typeof(T);
            }
        }

        Expression IQueryable.Expression {
            get {
                return _expression;
            }
        }

        IQueryProvider IQueryable.Provider {
            get {
                return _queryProvider;
            }
        }
        #endregion
    }
}
