using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CheckDenFaktFakeNewsFunction
{
    public class Document
    {
        public string DateTime { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Content { get; set; }

        public bool ApprovedByModerator { get; set; }

        public int Votes { get; set; }

        public int AmountOfVotes { get; set; }

        public List<string> Sources { get; set; }
    }
}
