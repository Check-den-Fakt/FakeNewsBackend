using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq;

namespace CheckDenFaktFakeNewsFunction
{
    public static class GetFunctions
    {
        [FunctionName("GetOne")]
        public static async Task<IActionResult> GetOne([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
                    [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
                    ILogger log)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            string dateTime = DateTime.Today.AddDays(-14).ToString("o");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };

            var query = client.CreateDocumentQuery<Document>(collectionUri, feedOptions: options, sqlExpression:
                $"SELECT Top 1 * FROM root where (root[\"DateTime\"] >= \"{dateTime}\") order by root._ts")
                .AsDocumentQuery();

            var result = await query.ExecuteNextAsync();

            if (result.Count == 0)
            {
                return new EmptyResult();
            }

            return new OkObjectResult(result.FirstOrDefault());
        }

        [FunctionName("GetNext")]
        public static async Task<IActionResult> GetNext([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
            ILogger log)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            string dateTime = DateTime.Today.AddDays(-14).ToString("o");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };

            int.TryParse(req.Query["offset"], out int n);

            var query = client.CreateDocumentQuery<Document>(collectionUri, feedOptions: options, sqlExpression:
                $"SELECT Top {n + 1} * FROM root where (root[\"DateTime\"] >= \"{dateTime}\") order by root._ts")
                .AsDocumentQuery();

            var result = await query.ExecuteNextAsync();

            if (result.Count == 0)
            {
                return new EmptyResult();
            }
            else if (result.Count <= n)
            {
                return new NoContentResult();
            }

            return new OkObjectResult(result.Skip(n).First());
        }
    }
}
