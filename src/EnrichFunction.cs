using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace CheckDenFaktFakeNewsFunction
{
    public static class EnrichFunction
    {
        [FunctionName("EnrichSources")]
        public static async Task EnrichSources(
            [ServiceBusTrigger("newfakenews", Connection = "ServiceBusConnection")] string queueItem,
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
            ILogger log)
        {

            var item = JsonConvert.DeserializeObject<Document>(queueItem);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };

            IDocumentQuery<Document> query = client.CreateDocumentQuery<Document>(collectionUri, options)
               .Where(p => p.Id.Equals(item.Id) && p.DateTime == item.DateTime)
               .AsDocumentQuery();

            Document document = (await query.ExecuteNextAsync<Document>().ConfigureAwait(false)).FirstOrDefault();
            
            var response = await GetSearchResult(document.Content);

            foreach (var link in response)
            {
                document.Sources.Add(link);
            }

            await client.UpsertDocumentAsync(collectionUri, document).ConfigureAwait(false);
        }

        private static async Task<List<string>> GetSearchResult(string content)
        {
            var client = new RestClient(Environment.GetEnvironmentVariable("ApimUrl"));
            var request = new RestRequest("/we-websearch-func/WebSearch", Method.POST);

            request.AddHeader("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("ApimKey"));
            request.AddHeader("Content-Type", "application/json");

            request.AddJsonBody(
                $"{{" +
                $"\"text\" : \"{content}\"" +
                $"}}");

            var cancellationTokenSource = new CancellationTokenSource();

            var restResponse = await client.ExecuteAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);

            SearchResponse response = JsonConvert.DeserializeObject<SearchResponse>(restResponse.Content);

            List<string> list = new List<string>();

            foreach (var item in response.Links)
            {
                list.Add(item.Url);
            }

            return list;
        }
    }
}
