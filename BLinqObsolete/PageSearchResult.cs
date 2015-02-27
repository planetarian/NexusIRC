// PageSearchResult.cs
//

using System;

namespace BLinq {

    public class PageSearchResult : BingSearchResult {

        public DateTime DateTime {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string DisplayUrl {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }
    }
}
