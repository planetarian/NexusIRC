// ImageSearchResult.cs
//

using System;

namespace BLinq {

    public class ImageSearchResult : BingSearchResult {

        public string DisplayUrl {
            get;
            set;
        }

        public int Height {
            get;
            set;
        }

        public Uri MediaUri {
            get;
            set;
        }

        public int ThumbnailHeight {
            get;
            set;
        }

        public Uri ThumbnailUri {
            get;
            set;
        }

        public int ThumbnailWidth {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }

        public int Width {
            get;
            set;
        }
    }
}
