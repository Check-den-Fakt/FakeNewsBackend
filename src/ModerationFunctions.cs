using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq;

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

        [FunctionName("VoteDown")]
        public static async Task<IActionResult> VoteDown(
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
                Votes = doc.Votes - 1,
                Content = doc.Content,
                Sources = doc.Sources,
                AmountOfVotes = doc.AmountOfVotes + 1,
            };

            await client.UpsertDocumentAsync(collectionUri, document);

            return new OkResult();
        }

        [FunctionName("VoteUp")]
        public static async Task<IActionResult> VoteUp(
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
                Votes = doc.Votes + 1,
                AmountOfVotes = doc.AmountOfVotes + 1,
                Content = doc.Content,
                Sources = doc.Sources
            };

            await client.UpsertDocumentAsync(collectionUri, document);

            return new OkResult();
        }

        [FunctionName("Remove")]
        public static async Task<IActionResult> Remove([HttpTrigger(AuthorizationLevel.User, "post", Route = null)] Document req,
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
    }
}