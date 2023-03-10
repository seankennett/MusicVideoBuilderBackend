using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewVideoFunction.Interfaces;

namespace NewVideoFunction
{
    public class NewVideoFunction
    {
        private readonly Connections _connections;
        private readonly IDatabaseWriter _databaseWriter;
        private readonly IMailer _mailer;
        private readonly IUserService _userService;
        private readonly IBlobService _blobService;

        public NewVideoFunction(IOptions<Connections> options, IDatabaseWriter databaseWriter, IMailer mailer, IUserService userService, IBlobService blobService)
        {
            _connections = options.Value;
            _databaseWriter = databaseWriter;
            _mailer = mailer;
            _userService = userService;
            _blobService = blobService;
        }
        /*
         https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid-trigger?tabs=in-process%2Cextensionv3&pivots=programming-language-csharp
        Put in fiddler:

         POST http://localhost:7207/runtime/webhooks/EventGrid?functionName=VideoNotifyFunction HTTP/1.1
User-Agent: Fiddler
aeg-event-type: Notification
Content-Type: application/json
Host: localhost:7207
Content-Length: 1008

[{
	"id": "741a105c-501e-007b-2436-25dfaa06188a",
	"topic": "/subscriptions/285ec89b-c6b0-46a6-9758-a0bce37bd2da/resourceGroups/music-video-builder/providers/Microsoft.Storage/storageAccounts/musicvideobuilderprivate",
	"subject": "/blobServices/default/containers/user-69d12eed-18c7-4763-8df2-ad828af710df/blobs/user/69d12eed-18c7-4763-8df2-ad828af710df/1/2023-01-10T20-49-57/CircleOfLifeExtended.mp4",
	"data": {
		"api": "PutBlockList",
		"requestId": "741a105c-501e-007b-2436-25dfaa000000",
		"eTag": "0x8DAF34DFFCADF62",
		"contentType": "application/octet-stream",
		"contentLength": 169910227,
		"blobType": "BlockBlob",
		"url": "https://musicvideobuilderprivate.blob.core.windows.net/user-69d12eed-18c7-4763-8df2-ad828af710df/user/69d12eed-18c7-4763-8df2-ad828af710df/1/2023-01-10T20-49-57/CircleOfLifeExtended.mp4",
		"sequencer": "0000000000000000000000000001493C00000000001b7609",
		"storageDiagnostics": {
			"batchId": "b98e3c68-e006-000c-0036-250a3e000000"
		}
	},
	"eventType": "Microsoft.Storage.BlobCreated",
	"eventTime": "2023-01-10T21:02:35.5883377Z",
	"dataVersion": ""
}]
         Prefix set like this
        $"{userObjectId}/{video.VideoId}/{date}/{video.VideoName}.{video.Format}"
         */
        [FunctionName("NewVideoFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation($"Processing EventGridEvent {JsonSerializer.Serialize(eventGridEvent)}");
            var blobPath = eventGridEvent.Subject;

            var splitBlob = blobPath.Split('/');
            var containerName = splitBlob[4];
            var userObjectId = splitBlob[7];
            var videoId = splitBlob[8];
            var videoName = splitBlob.Last();
            var blobName = string.Join("/", splitBlob.Skip(6));
            var blobPrefix = string.Join("/", splitBlob.Skip(6).Take(4));

            log.LogInformation($"Container {containerName} userId {userObjectId} videoId {videoId} blobName {blobName} blobPrefix {blobPrefix} subject {blobPath}");

            var userDetails = await _userService.GetDetails(userObjectId);

            await _blobService.CleanTempFiles(containerName, blobPrefix);

            var blobSasUri = await _blobService.GetBlobSas(containerName, blobName);

            await _databaseWriter.UpdateIsBuilding(int.Parse(videoId));

            _mailer.Send(userDetails.username, userDetails.email, blobSasUri.ToString(), videoName);
        }
    }
}
