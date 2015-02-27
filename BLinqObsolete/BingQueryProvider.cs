// BingQueryProvider.cs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;

namespace BLinq {

    internal sealed class BingQueryProvider : IQueryProvider {

        private readonly string _appKey;

        public BingQueryProvider(string appKey) {
            _appKey = appKey;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
            return new BingQuery<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression) {
            Type elementType = GetElementType(expression.Type);
            try {
                return (IQueryable)Activator.CreateInstance(typeof(BingQuery<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie) {
                throw tie.InnerException;
            }
        }

        public TResult Execute<TResult>(Expression expression) {
            return (TResult)Execute(expression);
        }

        public object Execute(Expression expression) {
            Type elementType = GetElementType(expression.Type);
            string query;

            var uriBuilder = new BingUriBuilder(elementType, _appKey);
            Uri uri = uriBuilder.BuildUri(expression, out query);

            if (ReferenceEquals(elementType, typeof(PageSearchResult))) {
                return ExecutePageSearch(uri, query);
            }
            return ReferenceEquals(elementType, typeof(ImageSearchResult))
                ? ExecuteImageSearch(uri, query)
                : null;
        }

        private IEnumerable<ImageSearchResult> ExecuteImageSearch(Uri uri, string query) {
            var resultList = new List<ImageSearchResult>();

            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            var webResponse = (HttpWebResponse)webRequest.GetResponse();
            string xml = null;

            if (webResponse.StatusCode == HttpStatusCode.OK) {
                using (var sr = new StreamReader(webResponse.GetResponseStream())) {
                    xml = sr.ReadToEnd();
                }
            }

            int count;
            int dummyTotalCount;
            return BingParser.ParseImageSearchResponse(xml, query, out count, out dummyTotalCount);
        }

        private IEnumerable<PageSearchResult> ExecutePageSearch(Uri uri, string query) {
            var resultList = new List<PageSearchResult>();

            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            var webResponse = (HttpWebResponse)webRequest.GetResponse();
            string xml = null;

            if (webResponse.StatusCode == HttpStatusCode.OK) {
                using (var sr = new StreamReader(webResponse.GetResponseStream())) {
                    xml = sr.ReadToEnd();
                }
            }

            int count;
            int dummyTotalCount;
            return BingParser.ParsePageSearchResponse(xml, query, out count, out dummyTotalCount);
        }

        private static Type FindIEnumerable(Type sequenceType) {
            if (ReferenceEquals(sequenceType, null) || ReferenceEquals(sequenceType, typeof(string))) {
                return null;
            }

            if (sequenceType.IsArray) {
                return typeof(IEnumerable<>).MakeGenericType(sequenceType.GetElementType());
            }

            if (sequenceType.IsGenericType) {
                foreach (Type arg in sequenceType.GetGenericArguments()) {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(sequenceType)) {
                        return ienum;
                    }
                }
            }

            Type[] interfaceTypes = sequenceType.GetInterfaces();
            if (interfaceTypes.Length != 0) {
                foreach (Type interfaceType in interfaceTypes) {
                    Type enumerableType = FindIEnumerable(interfaceType);
                    if (!ReferenceEquals(enumerableType, null)) {
                        return enumerableType;
                    }
                }
            }

            if (!ReferenceEquals(sequenceType.BaseType, null) &&
                !ReferenceEquals(sequenceType.BaseType, typeof(object))) {
                return FindIEnumerable(sequenceType.BaseType);
            }
            return null;
        }

        private static Type GetElementType(Type sequenceType) {
            Type enumerableType = FindIEnumerable(sequenceType);
            return ReferenceEquals(enumerableType, null)
                ? sequenceType
                : enumerableType.GetGenericArguments()[0];
        }
    }
}
