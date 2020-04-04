using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Newtonsoft.Json;

namespace CheckDenFaktFakeNewsFunction
{
    public static class InsertFunction
    {
        private static QueueClient queueClient;

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
                sources = req.Sources;
            }

            if (req.Content == null)
            {
                return new BadRequestResult();
            }

            Document document = new Document()
            {
                Id = Guid.NewGuid().ToString(),
                DateTime = dateTime,
                ApprovedByModerator = false,
                Votes = 0,
                Content = req.Content,
                Sources = sources,
                AmountOfVotes = 0
            };

            await client.UpsertDocumentAsync(collectionUri, document);
            queueClient = new QueueClient(Environment.GetEnvironmentVariable("ServiceBusConnection"), "newfakenews");

            await queueClient.SendAsync(new Message()
            {
                Body = Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new Document
                        {
                            Id = document.Id,
                            DateTime = document.DateTime
                        }))
            }).ConfigureAwait(false);

            return new OkResult();
        }
    }
}
