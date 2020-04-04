using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using System.Collections.Generic;

namespace CheckDenFaktFakeNewsFunction
{
    public static class ModerationFunctions
    {
        [FunctionName("Approve")]
        public static async Task<IActionResult> Approve(
            [HttpTrigger(AuthorizationLevel.User, "post", Route = null)] Document req, 
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client, 
            ILogger log)
        {
            log.LogInformation("C# approve trigger function processed a request.");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };
            IDocumentQuery<Document> query = client.CreateDocumentQuery<Document>(collectionUri, options)
               .Where(p => p.Id.Equals(req.Id))
               .AsDocumentQuery();

            var result = await query.ExecuteNextAsync<Document>();
            var doc = result.First();     

            Document document = new Document()
            {
                Id = req.Id,
                DateTime = req.DateTime,
                ApprovedByModerator = doc.ApprovedByModerator,
                Votes = doc.Votes,
                Content = doc.Content,
                Sources = doc.Sources
            };

            await client.UpsertDocumentAsync(collectionUri, document);

            return new OkResult();
        }

        [FunctionName("Vote")]
        public static async Task<IActionResult> Vote(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Document req,
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# approve trigger function processed a request.");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };
            IDocumentQuery<Document> query = client.CreateDocumentQuery<Document>(collectionUri, options)
               .Where(p => p.Id.Equals(req.Id))
               .AsDocumentQuery();

            var result = await query.ExecuteNextAsync<Document>();

            if (result.Count == 0)
            {
                return new NotFoundResult();
            }

            var doc = result.First();

            Document document = new Document()
            {
                Id = req.Id,
                DateTime = req.DateTime,
                ApprovedByModerator = doc.ApprovedByModerator,
                Votes = doc.Votes+1,
                Content = doc.Content,
                Sources = doc.Sources
            };

            await client.UpsertDocumentAsync(collectionUri, document);

            return new OkResult();
        }

        [FunctionName("Remove")]
        public static async Task<IActionResult> Remove([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Document req,
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# remove trigger function processed a request.");
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            FeedOptions options = new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            };

            IDocumentQuery<Document> query = client.CreateDocumentQuery<Document>(collectionUri, options)
               .Where(p => p.Id.Equals(req.Id))
               .AsDocumentQuery();

            var result = await query.ExecuteNextAsync();
            var doc = result.First();

            await client.DeleteDocumentAsync(doc.SelfLink);
            return new OkResult();
        }

        [FunctionName("Insert")]
        public static async Task<IActionResult> Insert([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Document req,
            [CosmosDB(databaseName: "fakenewsdb", collectionName: "fakenews", ConnectionStringSetting = "CosmosDbConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# remove trigger function processed a request.");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("fakenewsdb", "fakenews");

            string dateTime = DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ssZ");

            List<string> sources;

            if (req.Sources == null)
            {
                sources = new List<string>();
            }
            else
            {
                sources = new List<string>();
            }

            Document document = new Document()
            {
                Id = Guid.NewGuid().ToString(),
                DateTime = dateTime,
                ApprovedByModerator = false,
                Votes = 0,
                Content = req.Content,
                Sources = sources
            };

            await client.UpsertDocumentAsync(collectionUri, document);

            return new OkResult();
        }

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

            var query = client.CreateDocumentQuery<Document>(collectionUri, feedOptions : options, sqlExpression:
                $"SELECT * FROM root where (root[\"DateTime\"] >= \"{dateTime}\")order by root._ts")
                .AsDocumentQuery(); 

            var result = await query.ExecuteNextAsync();

            if (result.Count == 0)
            {
                return new EmptyResult();
            }

            return new OkObjectResult(result.First());
        }
    }
}
