// BingContext.cs
//

using System;

namespace BLinq {

    public class BingContext {

        private BingQueryProvider _queryProvider;

        public BingContext(string appKey) {
            if (String.IsNullOrEmpty(appKey)) {
                throw new ArgumentNullException("appKey");
            }
            _queryProvider = new BingQueryProvider(appKey);
        }

        public IBingQueryable<ImageSearchResult> Images {
            get {
                return new BingQuery<ImageSearchResult>(_queryProvider);
            }
        }

        public IBingQueryable<PageSearchResult> Pages {
            get {
                return new BingQuery<PageSearchResult>(_queryProvider);
            }
        }
    }
}
