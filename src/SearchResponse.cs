using System.Collections.Generic;
using Newtonsoft.Json;

namespace CheckDenFaktFakeNewsFunction
{
    public class SearchResponse
    {
        public string Query { get; set; }
        public List<Link> Links { get; set; }
    }

    public class Link
    {
        [JsonProperty(PropertyName = "domain")]
        public string Domain { get; set; }

        [JsonProperty(PropertyName = "trustScore")]
        public string TrustScore { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "snippet")]
        public string Snippet { get; set; }
    }
}
