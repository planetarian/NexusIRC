// BingUriBuilder.cs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using BLinq.Utility;
using ExpressionVisitor = BLinq.Utility.ExpressionVisitor;

namespace BLinq {

    internal sealed class BingUriBuilder : ExpressionVisitor {

        private StringBuilder _urlBuilder;
        private Dictionary<string, string> _queryString;
        private List<string> _queryExtensions;

        private string _searchField;
        private string _parameterPrefix;

        public BingUriBuilder(Type searchResultType, string appKey)
            : this(searchResultType, appKey, null) {
        }

        public BingUriBuilder(Type searchResultType, string appKey, IDictionary<string, object> parameters) {
            _queryString = new Dictionary<string, string>();
            _queryString["AppId"] = appKey;
            _queryString["Market"] = "en-us";
            _queryString["Version"] = "2.0";
            _queryString["Adult"] = "Moderate";
            _queryString["xmltype"] = "attributebased";

            if (parameters != null) {
                foreach (KeyValuePair<string, object> parameter in parameters) {
                    _queryString[parameter.Key] = parameter.Value.ToString();
                }
            }

            if (ReferenceEquals(searchResultType, typeof(PageSearchResult))) {
                _queryString["Sources"] = "Web";
                _parameterPrefix = "Web.";
            }
            else if (ReferenceEquals(searchResultType, typeof(ImageSearchResult))) {
                _queryString["Sources"] = "Image";
                _parameterPrefix = "Image.";
            }

            _urlBuilder = new StringBuilder();
            _urlBuilder.Append("http://api.search.live.net/xml.aspx?");
        }

        public Uri BuildUri(Expression expression, out string query) {
            if (expression != null) {
                expression = PartialEvaluator.Eval(expression, delegate(Expression expr) {
                    if ((expr.NodeType == ExpressionType.Call) &&
                        ReferenceEquals(((MethodCallExpression)expr).Method.DeclaringType, typeof(BingQueryable))) {
                        return false;
                    }

                    return (expr.NodeType != ExpressionType.Parameter);
                });
            }

            Visit(expression);

            if (_queryString.TryGetValue("Query", out query) == false) {
                throw new InvalidOperationException("The query expression must contain a where expression.");
            }
            if (_queryExtensions != null) {
                foreach (string extension in _queryExtensions) {
                    query = query + " " + extension;
                }
                _queryString["Query"] = query;
            }

            foreach (KeyValuePair<string, string> parameter in _queryString) {
                _urlBuilder.Append(parameter.Key);
                _urlBuilder.Append("=");
                _urlBuilder.Append(Uri.EscapeDataString(parameter.Value));
                _urlBuilder.Append("&");
            }

            string url = _urlBuilder.ToString();
            return new Uri(url, UriKind.Absolute);
        }

        protected override Expression VisitBinary(BinaryExpression b) {
            if ((b.NodeType == ExpressionType.Equal) ||
                (b.NodeType == ExpressionType.AndAlso)) {
                return base.VisitBinary(b);
            }

            throw new NotSupportedException("Only equality comparisons and logical ANDs are supported in binary expressions.");
        }

        protected override Expression VisitConditional(ConditionalExpression c) {
            throw new NotSupportedException("Conditional expressions are not supported in queries.");
        }

        protected override Expression VisitConstant(ConstantExpression c) {
            if (c.Value == null) {
                throw new ArgumentNullException("Cannot use null in queries.");
            }

            if (String.CompareOrdinal(_searchField, "Query") == 0) {
                _queryString["Query"] = (string)c.Value;
                _searchField = null;
            }

            return c;
        }

        protected override Expression VisitMemberAccess(MemberExpression m) {
            if ((m.Expression != null) && (m.Expression.NodeType == ExpressionType.Parameter)) {
                string name = m.Member.Name;

                if (_queryString.ContainsKey(name)) {
                    throw new NotSupportedException(String.Format("The member '{0}' cannot appear more than once in the query expression.", name));
                }

                if (String.CompareOrdinal(name, "Query") == 0) {
                    _searchField = name;
                }
                else {
                    throw new InvalidOperationException("Only the Query member can be used in a query expression.");
                }
                return m;
            }

            throw new NotSupportedException(String.Format("The member '{0}' is not supported.", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m) {
            if (ReferenceEquals(m.Method.DeclaringType, typeof(Queryable)) ||
                ReferenceEquals(m.Method.DeclaringType, typeof(Enumerable))) {
                if (String.CompareOrdinal(m.Method.Name, "Where") == 0) {
                    Visit(m.Arguments[0]);

                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    Visit(lambda.Body);

                    return m;
                }
                if (String.CompareOrdinal(m.Method.Name, "Skip") == 0) {
                    Visit(m.Arguments[0]);

                    ConstantExpression countExpression = (ConstantExpression)StripQuotes(m.Arguments[1]);
                    _queryString[_parameterPrefix + "Offset"] = ((int)countExpression.Value).ToString(CultureInfo.InvariantCulture);

                    return m;
                }
                if (String.CompareOrdinal(m.Method.Name, "Take") == 0) {
                    Visit(m.Arguments[0]);

                    ConstantExpression countExpression = (ConstantExpression)StripQuotes(m.Arguments[1]);
                    _queryString[_parameterPrefix + "Count"] = ((int)countExpression.Value).ToString(CultureInfo.InvariantCulture);

                    return m;
                }
            }
            else if (ReferenceEquals(m.Method.DeclaringType, typeof(BingQueryable))) {
                if (String.CompareOrdinal(m.Method.Name, "SafeResults") == 0) {
                    Visit(m.Arguments[0]);

                    _queryString["Adult"] = "Strict";
                    return m;
                }
                else if (String.CompareOrdinal(m.Method.Name, "LocalResults") == 0) {
                    Visit(m.Arguments[0]);

                    ConstantExpression locationExpression = (ConstantExpression)StripQuotes(m.Arguments[1]);
                    if (_queryExtensions == null) {
                        _queryExtensions = new List<string>();
                    }
                    _queryExtensions.Add("loc=" + (string)locationExpression.Value);

                    return m;
                }
                else if (String.CompareOrdinal(m.Method.Name, "ScopeResults") == 0) {
                    Visit(m.Arguments[0]);

                    ConstantExpression siteExpression = (ConstantExpression)StripQuotes(m.Arguments[1]);
                    if (_queryExtensions == null) {
                        _queryExtensions = new List<string>();
                    }
                    _queryExtensions.Add("site=" + (string)siteExpression.Value);

                    return m;
                }
            }

            throw new NotSupportedException(String.Format("The method '{0}' is not supported.", m.Method.Name));
        }

        protected override NewExpression VisitNew(NewExpression nex) {
            throw new NotSupportedException("New expressions are not supported in queries.");
        }

        protected override Expression VisitUnary(UnaryExpression u) {
            throw new NotSupportedException("Unary expressions are not supported in queries.");
        }
    }
}
