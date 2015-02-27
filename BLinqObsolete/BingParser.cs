// BingParser.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BLinq {

    internal static class BingParser {

        public static IEnumerable<ImageSearchResult> ParseImageSearchResponse(string xml, string requestedQuery, out int count, out int totalCount) {
            var resultList = new List<ImageSearchResult>();
            count = 0;
            totalCount = 0;

            if (!String.IsNullOrEmpty(xml)) {
                XDocument document = XDocument.Parse(xml);

                // unused
                // XNamespace defaultNamespace = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/element";
                XNamespace mediaNamespace = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/multimedia";

                XElement imageElement = document.Descendants(mediaNamespace + "Image").FirstOrDefault();
                if (imageElement != null) {
                    totalCount = Int32.Parse(imageElement.Attribute("Total").Value);

                    foreach (XElement resultElement in imageElement.Descendants(mediaNamespace + "ImageResult")) {
                        XElement thumbnailElement = resultElement.Descendants(mediaNamespace + "Thumbnail").First();

                        var result = new ImageSearchResult{
                            ID = resultElement.Attribute("Url").Value,
                            Query = requestedQuery,
                            Title = resultElement.Attribute("Title").Value,
                            MediaUri = new Uri(resultElement.Attribute("MediaUrl").Value, UriKind.Absolute),
                            Width = Int32.Parse(resultElement.Attribute("Width").Value),
                            Height = Int32.Parse(resultElement.Attribute("Height").Value),
                            Uri = new Uri(resultElement.Attribute("Url").Value, UriKind.Absolute),
                            DisplayUrl = resultElement.Attribute("DisplayUrl").Value,
                            ThumbnailUri = new Uri(thumbnailElement.Attribute("Url").Value, UriKind.Absolute),
                            ThumbnailWidth = Int32.Parse(thumbnailElement.Attribute("Width").Value),
                            ThumbnailHeight = Int32.Parse(thumbnailElement.Attribute("Height").Value)
                        };

                        resultList.Add(result);
                    }

                    count = resultList.Count;
                }
            }

            return resultList;
        }

        public static IEnumerable<PageSearchResult> ParsePageSearchResponse(string xml, string requestedQuery, out int count, out int totalCount) {
            List<PageSearchResult> resultList = new List<PageSearchResult>();
            count = 0;
            totalCount = 0;

            if (!String.IsNullOrEmpty(xml)) {
                XDocument document = XDocument.Parse(xml);

                XNamespace defaultNamespace = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/element";
                XNamespace webNamespace = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/web";

                XElement webElement = document.Descendants(webNamespace + "Web").FirstOrDefault();
                if (webElement != null) {
                    totalCount = Int32.Parse(webElement.Attribute("Total").Value);

                    foreach (XElement resultElement in webElement.Descendants(webNamespace + "WebResult")) {
                        PageSearchResult result = new PageSearchResult() {
                            ID = resultElement.Attribute("Url").Value,
                            Query = requestedQuery,
                            Title = resultElement.Attribute("Title").Value,
                            Uri = new Uri(resultElement.Attribute("Url").Value, UriKind.Absolute),
                            DisplayUrl = resultElement.Attribute("DisplayUrl").Value,
                            DateTime = DateTime.Parse(resultElement.Attribute("DateTime").Value)
                        };

                        XAttribute descriptionAttribute = resultElement.Attribute("Description");
                        if (descriptionAttribute != null) {
                            result.Description = descriptionAttribute.Value;
                        }

                        resultList.Add(result);
                    }

                    count = resultList.Count;
                }
            }

            return resultList;
        }
    }
}
