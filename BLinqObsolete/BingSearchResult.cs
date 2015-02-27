// BingSearchResult.cs
//

using System;
using System.ComponentModel.DataAnnotations;

namespace BLinq {

    public abstract class BingSearchResult {
        
        // TODO: Need to get RIA Services to support Uri as a valid key type...
        [Key]
        [Display(AutoGenerateField = false)]
        public string ID {
            get;
            set;
        }

        [Editable(false)]
        public Uri Uri {
            get;
            set;
        }

        [Editable(false)]
        [Display(AutoGenerateField = false)]
        public string Query {
            get;
            set;
        }
    }
}
